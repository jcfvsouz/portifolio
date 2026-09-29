namespace Components.Messaging;

public interface IEventConsumerStartup<TEvent> where TEvent : class
{
    Task StartupAsync(Func<TEvent, CancellationToken, Task> handleEvent, CancellationToken cancellationToken);
}
