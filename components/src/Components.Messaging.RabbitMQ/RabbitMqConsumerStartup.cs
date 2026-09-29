using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Components.Messaging.RabbitMQ;

public class RabbitMqConsumerStartup<TEvent>(
    IConnection connection,
    IOptionsMonitor<EventConsumerOptions> consumerOptions,
    IOptions<RabbitMqOptions> rabbitOptions,
    ILogger<RabbitMqConsumerStartup<TEvent>> logger) : IEventConsumerStartup<TEvent>, IAsyncDisposable where TEvent : class
{
    private IChannel? _channel;

    public async Task StartupAsync(Func<TEvent, CancellationToken, Task> handleEvent, CancellationToken cancellationToken)
    {
        var options = consumerOptions.Get(typeof(TEvent).Name);
        var exchangeName = rabbitOptions.Value.ExchangeName;
        var routingKey = options.MessageKey ?? typeof(TEvent).Name;

        var deadLetterExchange = $"{options.Destination}.dlx";
        var deadLetterQueue = $"{options.Destination}.dlq";

        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        _channel = channel;

        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Dead-letter side: a message nacked without requeue below lands here instead of
        // vanishing or looping forever, so a permanent failure stays inspectable.
        await channel.ExchangeDeclareAsync(
            exchange: deadLetterExchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: deadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(deadLetterQueue, deadLetterExchange, routingKey: string.Empty, cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: options.Destination,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { { "x-dead-letter-exchange", deadLetterExchange } },
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(options.Destination, exchangeName, routingKey, cancellationToken: cancellationToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: (ushort)options.PrefetchCount, global: false, cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            try
            {
                var @event = JsonSerializer.Deserialize<TEvent>(delivery.Body.Span)
                    ?? throw new InvalidOperationException($"Message body could not be deserialized into {typeof(TEvent).Name}.");

                await handleEvent(@event, cancellationToken);

                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to handle a {EventType} message from queue {QueueName}. Routing to {DeadLetterQueue}.", typeof(TEvent).Name, options.Destination, deadLetterQueue);
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
            }
        };

        await channel.BasicConsumeAsync(options.Destination, autoAck: false, consumer: consumer, cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }
    }
}
