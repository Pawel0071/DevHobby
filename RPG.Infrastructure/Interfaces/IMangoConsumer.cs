namespace RPG.Infrastructure.Interfaces;

public interface IMangoConsumer<in T>
{
    Task Consume(T message);
}