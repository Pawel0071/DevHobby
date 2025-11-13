using System.Security.Cryptography;
using System.Text;

namespace RPG.Abstractions.Interfaces;

public interface IEventIdProvider
{
    Guid Generate(IGameEvent gameEvent, DateTime occurredAtUtc);
    Guid Generate(IGameEvent gameEvent, DateTime occurredAtUtc, int sequence, Guid correlationId);
}

public sealed class DeterministicEventIdProvider : IEventIdProvider
{
    private static readonly Guid NamespaceGuid = Guid.Parse("11111111-2222-3333-4444-555555555555");

    public Guid Generate(IGameEvent gameEvent, DateTime occurredAtUtc)
    {
        if (gameEvent == null) throw new ArgumentNullException(nameof(gameEvent));
        var type = gameEvent.GetType();
        var keyParts = new List<string> { type.Name, occurredAtUtc.ToUniversalTime().Ticks.ToString() };
        var candidateNames = new[] { "CharacterId", "NpcId", "WorldId", "PlayerId", "SkillId", "SessionId" };
        foreach (var name in candidateNames)
        {
            var prop = type.GetProperty(name);
            if (prop != null)
            {
                var value = prop.GetValue(gameEvent)?.ToString();
                if (!string.IsNullOrWhiteSpace(value)) keyParts.Add(value);
            }
        }
        var composite = string.Join('|', keyParts);
        var nsBytes = NamespaceGuid.ToByteArray();
        var dataBytes = Encoding.UTF8.GetBytes(composite);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(nsBytes.Concat(dataBytes).ToArray());
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }

    public Guid Generate(IGameEvent gameEvent, DateTime occurredAtUtc, int sequence, Guid correlationId)
    {
        if (gameEvent == null) throw new ArgumentNullException(nameof(gameEvent));
        var type = gameEvent.GetType();
        var keyParts = new List<string> { type.Name, sequence.ToString(), correlationId.ToString("N") };
        var candidateNames = new[] { "CharacterId", "NpcId", "WorldId", "PlayerId", "SkillId", "SessionId" };
        foreach (var name in candidateNames)
        {
            var prop = type.GetProperty(name);
            if (prop != null)
            {
                var value = prop.GetValue(gameEvent)?.ToString();
                if (!string.IsNullOrWhiteSpace(value)) keyParts.Add(value);
            }
        }
        var composite = string.Join('|', keyParts);
        var nsBytes = NamespaceGuid.ToByteArray();
        var dataBytes = Encoding.UTF8.GetBytes(composite);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(nsBytes.Concat(dataBytes).ToArray());
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }
}
