using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Components.Messaging.RabbitMQ;

public class RabbitMqEventPublisher(IConnection connection, IOptions<RabbitMqOptions> options) : IEventPublisher, IAsyncDisposable
{
    private readonly string _defaultExchangeName = options.Value.ExchangeName;
    private readonly SemaphoreSlim _channelLock = new(1, 1);
    private readonly ConcurrentDictionary<string, byte> _declaredExchanges = new();
    private IChannel? _channel;

    public async Task PublishAsync<TEvent>(TEvent @event, string? destination = null, string? messageKey = null, CancellationToken cancellationToken = default) where TEvent : class
    {
        var exchangeName = destination ?? _defaultExchangeName;
        var channel = await GetChannelAsync(exchangeName, cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event);
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: messageKey ?? typeof(TEvent).Name,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    private async Task<IChannel> GetChannelAsync(string exchangeName, CancellationToken cancellationToken)
    {
        var channel = await GetOpenChannelAsync(cancellationToken);
        await EnsureExchangeDeclaredAsync(channel, exchangeName, cancellationToken);
        return channel;
    }

    private async Task<IChannel> GetOpenChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true }) return _channel;

        await _channelLock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true }) return _channel;

            _channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            return _channel;
        }
        finally
        {
            _channelLock.Release();
        }
    }

    private async Task EnsureExchangeDeclaredAsync(IChannel channel, string exchangeName, CancellationToken cancellationToken)
    {
        if (_declaredExchanges.ContainsKey(exchangeName)) return;

        await _channelLock.WaitAsync(cancellationToken);
        try
        {
            if (!_declaredExchanges.TryAdd(exchangeName, 0)) return;

            await channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _channelLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        _channelLock.Dispose();
    }
}
