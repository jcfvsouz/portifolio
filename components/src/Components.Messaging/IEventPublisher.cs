namespace Components.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, string? destination = null, string? messageKey = null, CancellationToken cancellationToken = default) where TEvent : class;
}
