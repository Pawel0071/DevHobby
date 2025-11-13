using Microsoft.Extensions.Hosting;
using RPG.Infrastructure.Interfaces;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace RPG.Infrastructure.Outbox;

public interface IOutboxCircuitBreakerState
{
    string State { get; }
    DateTime ChangedAtUtc { get; }
    int RecentErrorCount { get; }
    void SetState(string state, DateTime changedAtUtc);
    void SetRecentErrorCount(int count);
}

internal sealed class OutboxCircuitBreakerState : IOutboxCircuitBreakerState
{
    public string State { get; private set; } = "Closed";
    public DateTime ChangedAtUtc { get; private set; } = DateTime.UtcNow;
    public int RecentErrorCount { get; private set; } = 0;
    public void SetState(string state, DateTime changedAtUtc)
    {
        State = state;
        ChangedAtUtc = changedAtUtc;
    }
    public void SetRecentErrorCount(int count) => RecentErrorCount = count;
}

/// <summary>
/// Dispatcher Outbox korzystający z Redis jako magazynu, z wbudowanym prostym circuit breakerem dla RabbitMQ.
/// Logika:
///  - Wiadomości zapisywane są jako elementy listy Redis: klucz listy = "outbox:pending" (LPUSH przez producentów).
///  - Dispatcher (ten worker) wykonuje BRPOP (lub RPOPLPUSH) batchowo aby pobrać wiadomości FIFO.
///  - Po udanym publish -> wiadomość usuwana (już zdjęta z listy), brak potrzeby dodatkowej aktualizacji.
///  - Przy błędzie wiadomość trafia do listy retry z inkrementem licznika (max retry).
///  - Circuit breaker monitoruje liczbę kolejnych błędów w oknie czasowym.
///     Closed -> normalna praca.
///     Open   -> pauza publikacji, tylko obserwacja czy minął czas resetu.
///     HalfOpen -> pojedyncze próby; sukces -> Closed, błąd -> Open.
/// </summary>
public class OutboxDispatcher : BackgroundService
{
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly IRabbitMqPublisher _publisher;
    private readonly IDatabase _redis;
    private readonly IOutboxCircuitBreakerState _breakerState;
    private readonly OutboxCircuitBreakerState? _mutableBreakerState; // gdy dostarczono naszą klasę – aktualizujemy ją

    // Ustawienia
    private const string PendingListKey = "outbox:pending"; // LPUSH / BRPOP
    private const string RetryListKey = "outbox:retry";
    private const int BatchSize = 10;
    private const int MaxRetriesPerMessage = 3;
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan RetryBackoffBase = TimeSpan.FromSeconds(2);

    // Circuit Breaker settings
    private const int ErrorThreshold = 5; // liczba błędów
    private static readonly TimeSpan ErrorWindow = TimeSpan.FromSeconds(10); // w tym oknie
    private static readonly TimeSpan OpenStateDuration = TimeSpan.FromSeconds(15); // ile czekamy w Open

    private enum BreakerState { Closed, Open, HalfOpen }
    private BreakerState _state = BreakerState.Closed;
    private DateTime _stateChangedAt = DateTime.UtcNow;
    private readonly ConcurrentQueue<DateTime> _errorTimestamps = new();

    // Metrics
    private static readonly Meter Meter = new("RPG.Outbox", "1.0.0");
    private static readonly Counter<long> PublishedCounter = Meter.CreateCounter<long>("outbox_published_total");
    private static readonly Counter<long> RetryCounter = Meter.CreateCounter<long>("outbox_retry_total");
    private static readonly Counter<long> DroppedCounter = Meter.CreateCounter<long>("outbox_dropped_total");
    private static readonly ObservableGauge<int> BreakerStateGauge = Meter.CreateObservableGauge("outbox_breaker_state", () => new Measurement<int>((int)_staticLastState));
    private static BreakerState _staticLastState = BreakerState.Closed;

    public OutboxDispatcher(
        IConnectionMultiplexer redisMultiplexer,
        IRabbitMqPublisher publisher,
        ILogger<OutboxDispatcher> logger,
        IOutboxCircuitBreakerState sharedState)
    {
        _redis = redisMultiplexer.GetDatabase();
        _publisher = publisher;
        _logger = logger;
        _breakerState = sharedState;
        _mutableBreakerState = sharedState as OutboxCircuitBreakerState; // brak wyjątku jeśli test dostarczy własny stan
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Info("OutboxDispatcher started (Redis mode, breaker=Closed)");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                UpdateBreakerState();

                if (_state == BreakerState.Open)
                {
                    await Task.Delay(PollDelay, stoppingToken);
                    continue; // brak publikacji
                }

                // Najpierw spróbuj przenieść wiadomości z retry jeśli ich backoff minął
                await DrainRetryAsync(stoppingToken);

                // HalfOpen -> tylko jedna sonda naraz
                int target = _state == BreakerState.HalfOpen ? 1 : BatchSize;
                var messages = await PopBatchAsync(target, stoppingToken);

                if (messages.Count == 0)
                {
                    await Task.Delay(PollDelay, stoppingToken);
                    continue;
                }

                foreach (var msg in messages)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    var success = await TryPublishAsync(msg, stoppingToken);

                    if (!success)
                        await HandlePublishFailureAsync(msg, stoppingToken);
                    else if (_state == BreakerState.HalfOpen)
                        TransitionTo(BreakerState.Closed, "Half-open probe succeeded; moving to Closed");

                    if (_state == BreakerState.Open) break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error in OutboxDispatcher main loop", ex);
            }
        }

        _logger.Warn("OutboxDispatcher stopped.");
    }

    private void UpdateBreakerState()
    {
        // wyczyść stare błędy spoza okna
        while (_errorTimestamps.TryPeek(out var ts) && ts < DateTime.UtcNow - ErrorWindow)
            _errorTimestamps.TryDequeue(out _);

        _breakerState.SetRecentErrorCount(_errorTimestamps.Count);
        if (_mutableBreakerState != null)
        {
            _mutableBreakerState.SetRecentErrorCount(_errorTimestamps.Count);
        }

        switch (_state)
        {
            case BreakerState.Closed when _errorTimestamps.Count >= ErrorThreshold:
                TransitionTo(BreakerState.Open, $"Error threshold reached ({_errorTimestamps.Count}/{ErrorThreshold}) – opening breaker");
                break;
            case BreakerState.Open when DateTime.UtcNow - _stateChangedAt >= OpenStateDuration:
                TransitionTo(BreakerState.HalfOpen, "Open duration elapsed – transitioning to HalfOpen");
                break;
        }
    }

    private void TransitionTo(BreakerState newState, string reason)
    {
        _state = newState;
        _staticLastState = newState;
        _stateChangedAt = DateTime.UtcNow;
        _breakerState.SetState(newState.ToString(), _stateChangedAt);
        if (_mutableBreakerState != null)
        {
            _mutableBreakerState.SetState(newState.ToString(), _stateChangedAt);
        }
        _logger.Warn($"CircuitBreaker transition: {_state} – {reason}");

        if (newState == BreakerState.Closed)
            // reset historii błędów
            while (_errorTimestamps.TryDequeue(out _)) { }
    }

    private async Task DrainRetryAsync(CancellationToken ct)
    {
        // Sprawdzamy czubek retry listy – jeśli spełnia warunek backoff to przenosimy do pending (RPOPLPUSH)
        // Aby uniknąć blokowania – limit elementów na iterację
        for (int i = 0; i < BatchSize; i++)
        {
            var value = await _redis.ListRightPopAsync(RetryListKey);
            if (!value.HasValue) break;

            OutboxMessage? msg = null;
            try { msg = JsonSerializer.Deserialize<OutboxMessage>(value!); } catch (JsonException ex) { _logger.Warn($"Retry entry invalid JSON: {ex.Message}"); }
            if (msg == null) continue;

            var backoff = RetryBackoffBase * Math.Pow(2, Math.Max(0, msg.RetryCount - 1)); // 2^n progressive
            if (msg.LastRetryAt.HasValue && DateTime.UtcNow - msg.LastRetryAt < backoff)
            {
                // Za wcześnie – odłóż z powrotem na lewo aby zachować względną kolejność
                await _redis.ListLeftPushAsync(RetryListKey, value);
                break; // następne też będą młodsze
            }

            // Backoff minął – reset LastRetryAt (zostaw RetryCount) i wrzuć na pending na początek
            msg.LastRetryAt = null;
            var json = JsonSerializer.Serialize(msg);
            await _redis.ListLeftPushAsync(PendingListKey, json);
        }
    }

    private async Task<List<OutboxMessage>> PopBatchAsync(int count, CancellationToken ct)
    {
        var list = new List<OutboxMessage>(count);
        for (int i = 0; i < count; i++)
        {
            var value = await _redis.ListRightPopAsync(PendingListKey);
            if (!value.HasValue) break;

            try
            {
                var msg = JsonSerializer.Deserialize<OutboxMessage>(value!);
                if (msg != null) list.Add(msg);
            }
            catch (JsonException ex)
            {
                _logger.Warn($"Failed to deserialize outbox entry: {ex.Message}");
            }
        }
        return list;
    }

    private async Task<bool> TryPublishAsync(OutboxMessage msg, CancellationToken ct)
    {
        try
        {
            _logger.Debug($"Dispatching message {msg.Id} to topic '{msg.Topic}' (state={_state})");
            await _publisher.PublishAsync(msg.Topic, msg.Payload);
            PublishedCounter.Add(1);
            _logger.Info($"Message {msg.Id} dispatched successfully.");
            return true;
        }
        catch (Exception ex)
        {
            _errorTimestamps.Enqueue(DateTime.UtcNow);
            _logger.Warn($"Publish failed for message {msg.Id}: {ex.Message}");
            return false;
        }
    }

    private async Task HandlePublishFailureAsync(OutboxMessage msg, CancellationToken ct)
    {
        msg.RetryCount++;
        msg.LastRetryAt = DateTime.UtcNow;

        if (msg.RetryCount > MaxRetriesPerMessage)
        {
            DroppedCounter.Add(1);
            _logger.Error($"Message {msg.Id} dropped after {MaxRetriesPerMessage} retries.");
            return; // porzucamy (można rozszerzyć o DLQ)
        }

        RetryCounter.Add(1);
        var json = JsonSerializer.Serialize(msg);
        await _redis.ListLeftPushAsync(RetryListKey, json);
        _logger.Warn($"Message {msg.Id} scheduled for retry (attempt {msg.RetryCount}).");

        // Jeśli w HalfOpen – błąd przełącza znów na Open
        if (_state == BreakerState.HalfOpen)
            TransitionTo(BreakerState.Open, "Half-open probe failed – back to Open");
    }
}
