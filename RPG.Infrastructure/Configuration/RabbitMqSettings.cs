namespace RPG.Infrastructure.Configuration;

public class RabbitMqSettings
{
    public string Host { get; set; } = default!;
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    
    // Exchange and Queue configuration
    public string ExchangeName { get; set; } = "rpg_exchange";
    public string ExchangeType { get; set; } = "topic";
    public string? QueueName { get; set; }
    public string? RoutingKey { get; set; }
}