using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Components.Messaging;

public class EventConsumer<TEvent>(
    IEventConsumerStartup<TEvent> startup,
    IServiceScopeFactory scopeFactory) : BackgroundService where TEvent : class
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await startup.StartupAsync(InvokeHandlerAsync, stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when the host is shutting down.
        }
    }

    private async Task InvokeHandlerAsync(TEvent @event, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<TEvent>>();
        await handler.HandleAsync(@event, cancellationToken);
    }
}
