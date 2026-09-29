using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Components.Messaging.RabbitMQ.Tests;

public class RabbitMqConsumerStartupTests
{
    private record SampleEvent(string Name);

    private class CapturedConsumer
    {
        public IAsyncBasicConsumer? Value { get; set; }
    }

    private static (IConnection Connection, IChannel Channel) ConnectionReturningAnOpenChannel()
    {
        var channel = Substitute.For<IChannel>();
        channel.IsOpen.Returns(true);

        var connection = Substitute.For<IConnection>();
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>())
            .Returns(channel);

        return (connection, channel);
    }

    private static CapturedConsumer SetupConsumeCapture(IChannel channel)
    {
        var captured = new CapturedConsumer();
        channel.BasicConsumeAsync(
                Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(),
                Arg.Any<IDictionary<string, object?>?>(),
                Arg.Do<IAsyncBasicConsumer>(c => captured.Value = c),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(string.Empty));

        return captured;
    }

    private static RabbitMqConsumerStartup<SampleEvent> CreateStartup(
        IConnection connection,
        EventConsumerOptions consumerOptions,
        string exchangeName = "orders-exchange")
    {
        var monitor = Substitute.For<IOptionsMonitor<EventConsumerOptions>>();
        monitor.Get(nameof(SampleEvent)).Returns(consumerOptions);

        return new RabbitMqConsumerStartup<SampleEvent>(
            connection,
            monitor,
            Options.Create(new RabbitMqOptions { ExchangeName = exchangeName }),
            NullLogger<RabbitMqConsumerStartup<SampleEvent>>.Instance);
    }

    [Fact]
    public async Task StartupAsync_declares_the_main_exchange_as_a_durable_topic()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var startup = CreateStartup(connection, new EventConsumerOptions { Destination = "orders-queue" });
        SetupConsumeCapture(channel);

        // Act
        await startup.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        await channel.Received(1).ExchangeDeclareAsync(
            exchange: Arg.Is("orders-exchange"),
            type: Arg.Is(ExchangeType.Topic),
            durable: Arg.Is(true),
            autoDelete: Arg.Is(false),
            arguments: Arg.Any<IDictionary<string, object?>?>(),
            passive: Arg.Any<bool>(),
            noWait: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartupAsync_declares_a_fanout_dead_letter_exchange_bound_to_a_dead_letter_queue()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var startup = CreateStartup(connection, new EventConsumerOptions { Destination = "orders-queue" });
        SetupConsumeCapture(channel);

        // Act
        await startup.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        await channel.Received(1).ExchangeDeclareAsync(
            exchange: Arg.Is("orders-queue.dlx"),
            type: Arg.Is(ExchangeType.Fanout),
            durable: Arg.Is(true),
            autoDelete: Arg.Is(false),
            arguments: Arg.Any<IDictionary<string, object?>?>(),
            passive: Arg.Any<bool>(),
            noWait: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>());

        await channel.Received(1).QueueDeclareAsync(
            queue: Arg.Is("orders-queue.dlq"),
            durable: Arg.Is(true),
            exclusive: Arg.Is(false),
            autoDelete: Arg.Is(false),
            arguments: Arg.Any<IDictionary<string, object?>?>(),
            passive: Arg.Any<bool>(),
            noWait: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>());

        await channel.Received(1).QueueBindAsync(
            queue: Arg.Is("orders-queue.dlq"),
            exchange: Arg.Is("orders-queue.dlx"),
            routingKey: Arg.Is(string.Empty),
            arguments: Arg.Any<IDictionary<string, object?>?>(),
            noWait: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartupAsync_declares_the_main_queue_with_a_dead_letter_exchange_argument()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var startup = CreateStartup(connection, new EventConsumerOptions { Destination = "orders-queue" });
        SetupConsumeCapture(channel);

        // Act
        await startup.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        await channel.Received(1).QueueDeclareAsync(
            queue: Arg.Is("orders-queue"),
            durable: Arg.Is(true),
            exclusive: Arg.Is(false),
            autoDelete: Arg.Is(false),
            arguments: Arg.Is<IDictionary<string, object?>?>(args =>
                args != null && args.ContainsKey("x-dead-letter-exchange") && Equals(args["x-dead-letter-exchange"], "orders-queue.dlx")),
            passive: Arg.Any<bool>(),
            noWait: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartupAsync_binds_the_main_queue_using_the_configured_message_key()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var startup = CreateStartup(connection, new EventConsumerOptions { Destination = "orders-queue", MessageKey = "campaign.published.v1" });
        SetupConsumeCapture(channel);

        // Act
        await startup.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        await channel.Received(1).QueueBindAsync(
            queue: Arg.Is("orders-queue"),
            exchange: Arg.Is("orders-exchange"),
            routingKey: Arg.Is("campaign.published.v1"),
            arguments: Arg.Any<IDictionary<string, object?>?>(),
            noWait: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartupAsync_binds_the_main_queue_using_the_event_type_name_when_no_message_key_is_configured()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var startup = CreateStartup(connection, new EventConsumerOptions { Destination = "orders-queue" });
        SetupConsumeCapture(channel);

        // Act
        await startup.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        await channel.Received(1).QueueBindAsync(
            queue: Arg.Is("orders-queue"),
            exchange: Arg.Is("orders-exchange"),
            routingKey: Arg.Is(nameof(SampleEvent)),
            arguments: Arg.Any<IDictionary<string, object?>?>(),
            noWait: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartupAsync_sets_qos_using_the_configured_prefetch_count()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var startup = CreateStartup(connection, new EventConsumerOptions { Destination = "orders-queue", PrefetchCount = 25 });
        SetupConsumeCapture(channel);

        // Act
        await startup.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        await channel.Received(1).BasicQosAsync(
            prefetchSize: Arg.Is<uint>(0),
            prefetchCount: Arg.Is<ushort>(25),
            global: Arg.Is(false),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartupAsync_starts_consuming_from_the_configured_queue_with_manual_ack()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var startup = CreateStartup(connection, new EventConsumerOptions { Destination = "orders-queue" });
        SetupConsumeCapture(channel);

        // Act
        await startup.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        await channel.Received(1).BasicConsumeAsync(
            Arg.Is("orders-queue"),
            Arg.Is(false),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<IDictionary<string, object?>?>(),
            Arg.Any<IAsyncBasicConsumer>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Received_message_is_deserialized_dispatched_and_acknowledged()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var startup = CreateStartup(connection, new EventConsumerOptions { Destination = "orders-queue" });
        var capture = SetupConsumeCapture(channel);

        SampleEvent? handled = null;
        await startup.StartupAsync((@event, _) =>
        {
            handled = @event;
            return Task.CompletedTask;
        }, CancellationToken.None);

        var body = JsonSerializer.SerializeToUtf8Bytes(new SampleEvent("payload"));

        // Act
        await capture.Value!.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 1,
            redelivered: false,
            exchange: "orders-exchange",
            routingKey: nameof(SampleEvent),
            properties: new BasicProperties(),
            body: body,
            cancellationToken: CancellationToken.None);

        // Assert
        handled.Should().BeEquivalentTo(new SampleEvent("payload"));
        await channel.Received(1).BasicAckAsync(
            deliveryTag: Arg.Is(1UL),
            multiple: Arg.Is(false),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Received_message_is_nacked_without_requeue_when_the_handler_throws()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var startup = CreateStartup(connection, new EventConsumerOptions { Destination = "orders-queue" });
        var capture = SetupConsumeCapture(channel);

        await startup.StartupAsync((_, _) => throw new InvalidOperationException("boom"), CancellationToken.None);

        var body = JsonSerializer.SerializeToUtf8Bytes(new SampleEvent("payload"));

        // Act
        await capture.Value!.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 7,
            redelivered: false,
            exchange: "orders-exchange",
            routingKey: nameof(SampleEvent),
            properties: new BasicProperties(),
            body: body,
            cancellationToken: CancellationToken.None);

        // Assert
        await channel.Received(1).BasicNackAsync(
            deliveryTag: Arg.Is(7UL),
            multiple: Arg.Is(false),
            requeue: Arg.Is(false),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Received_message_is_nacked_without_requeue_when_the_body_cannot_be_deserialized()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var startup = CreateStartup(connection, new EventConsumerOptions { Destination = "orders-queue" });
        var capture = SetupConsumeCapture(channel);

        await startup.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        var body = "null"u8.ToArray(); // valid JSON, but deserializes to a null SampleEvent

        // Act
        await capture.Value!.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 3,
            redelivered: false,
            exchange: "orders-exchange",
            routingKey: nameof(SampleEvent),
            properties: new BasicProperties(),
            body: body,
            cancellationToken: CancellationToken.None);

        // Assert
        await channel.Received(1).BasicNackAsync(
            deliveryTag: Arg.Is(3UL),
            multiple: Arg.Is(false),
            requeue: Arg.Is(false),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisposeAsync_disposes_the_channel_once_startup_has_run()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var startup = CreateStartup(connection, new EventConsumerOptions { Destination = "orders-queue" });
        SetupConsumeCapture(channel);
        await startup.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Act
        await startup.DisposeAsync();

        // Assert
        await channel.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_does_not_throw_when_startup_was_never_called()
    {
        // Arrange
        var (connection, _) = ConnectionReturningAnOpenChannel();
        var startup = CreateStartup(connection, new EventConsumerOptions { Destination = "orders-queue" });

        // Act
        var act = async () => await startup.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }
}
