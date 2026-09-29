using System.Text.Json;
using Components.Messaging.RabbitMQ.Tests.Fixtures;
using RabbitMQ.Client;

namespace Components.Messaging.RabbitMQ.Tests;

public class RabbitMqConsumerStartupTests
{
    // Note on ordering: Setup_RabbitOptions_ExchangeName_Result reassigns the fixture's
    // IOptions<RabbitMqOptions> instance - NewInstance() must capture that exact instance in
    // the constructor, so this setup runs before NewInstance(). Every other Setup_... call
    // configures Mock<T> behavior queried lazily per call (including ConsumerOptions, an
    // IOptionsMonitor mock) and runs after, per the repo convention.

    [Fact]
    public async Task StartupAsync_declares_the_main_exchange_as_a_durable_topic()
    {
        // Arrange
        var fixture = new RabbitMqConsumerStartupFixture();
        fixture.Setup_RabbitOptions_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();
        fixture.Setup_ConsumerOptions_Get_Result(new EventConsumerOptions { Destination = "orders-queue" });
        fixture.Setup_Channel_BasicConsumeAsync_CapturesConsumer();

        // Act
        await sut.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        fixture.Channel.Verify(c => c.ExchangeDeclareAsync(
            "orders-exchange", ExchangeType.Topic, true, false,
            It.IsAny<IDictionary<string, object?>?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartupAsync_declares_a_fanout_dead_letter_exchange_bound_to_a_dead_letter_queue()
    {
        // Arrange
        var fixture = new RabbitMqConsumerStartupFixture();
        fixture.Setup_RabbitOptions_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();
        fixture.Setup_ConsumerOptions_Get_Result(new EventConsumerOptions { Destination = "orders-queue" });
        fixture.Setup_Channel_BasicConsumeAsync_CapturesConsumer();

        // Act
        await sut.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        fixture.Channel.Verify(c => c.ExchangeDeclareAsync(
            "orders-queue.dlx", ExchangeType.Fanout, true, false,
            It.IsAny<IDictionary<string, object?>?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);

        fixture.Channel.Verify(c => c.QueueDeclareAsync(
            "orders-queue.dlq", true, false, false,
            It.IsAny<IDictionary<string, object?>?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);

        fixture.Channel.Verify(c => c.QueueBindAsync(
            "orders-queue.dlq", "orders-queue.dlx", string.Empty,
            It.IsAny<IDictionary<string, object?>?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartupAsync_declares_the_main_queue_with_a_dead_letter_exchange_argument()
    {
        // Arrange
        var fixture = new RabbitMqConsumerStartupFixture();
        fixture.Setup_RabbitOptions_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();
        fixture.Setup_ConsumerOptions_Get_Result(new EventConsumerOptions { Destination = "orders-queue" });
        fixture.Setup_Channel_BasicConsumeAsync_CapturesConsumer();

        // Act
        await sut.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        fixture.Channel.Verify(c => c.QueueDeclareAsync(
            "orders-queue", true, false, false,
            It.Is<IDictionary<string, object?>?>(args =>
                args != null && args.ContainsKey("x-dead-letter-exchange") && Equals(args["x-dead-letter-exchange"], "orders-queue.dlx")),
            It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartupAsync_binds_the_main_queue_using_the_configured_message_key()
    {
        // Arrange
        var fixture = new RabbitMqConsumerStartupFixture();
        fixture.Setup_RabbitOptions_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();
        fixture.Setup_ConsumerOptions_Get_Result(new EventConsumerOptions { Destination = "orders-queue", MessageKey = "campaign.published.v1" });
        fixture.Setup_Channel_BasicConsumeAsync_CapturesConsumer();

        // Act
        await sut.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        fixture.Channel.Verify(c => c.QueueBindAsync(
            "orders-queue", "orders-exchange", "campaign.published.v1",
            It.IsAny<IDictionary<string, object?>?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartupAsync_binds_the_main_queue_using_the_event_type_name_when_no_message_key_is_configured()
    {
        // Arrange
        var fixture = new RabbitMqConsumerStartupFixture();
        fixture.Setup_RabbitOptions_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();
        fixture.Setup_ConsumerOptions_Get_Result(new EventConsumerOptions { Destination = "orders-queue" });
        fixture.Setup_Channel_BasicConsumeAsync_CapturesConsumer();

        // Act
        await sut.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        fixture.Channel.Verify(c => c.QueueBindAsync(
            "orders-queue", "orders-exchange", nameof(SampleEvent),
            It.IsAny<IDictionary<string, object?>?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartupAsync_sets_qos_using_the_configured_prefetch_count()
    {
        // Arrange
        var fixture = new RabbitMqConsumerStartupFixture();
        fixture.Setup_RabbitOptions_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();
        fixture.Setup_ConsumerOptions_Get_Result(new EventConsumerOptions { Destination = "orders-queue", PrefetchCount = 25 });
        fixture.Setup_Channel_BasicConsumeAsync_CapturesConsumer();

        // Act
        await sut.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        fixture.Channel.Verify(c => c.BasicQosAsync(0, 25, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartupAsync_starts_consuming_from_the_configured_queue_with_manual_ack()
    {
        // Arrange
        var fixture = new RabbitMqConsumerStartupFixture();
        fixture.Setup_RabbitOptions_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();
        fixture.Setup_ConsumerOptions_Get_Result(new EventConsumerOptions { Destination = "orders-queue" });
        fixture.Setup_Channel_BasicConsumeAsync_CapturesConsumer();

        // Act
        await sut.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        fixture.Channel.Verify(c => c.BasicConsumeAsync(
            "orders-queue", false, It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<IDictionary<string, object?>?>(), It.IsAny<IAsyncBasicConsumer>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Received_message_is_deserialized_dispatched_and_acknowledged()
    {
        // Arrange
        var fixture = new RabbitMqConsumerStartupFixture();
        fixture.Setup_RabbitOptions_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();
        fixture.Setup_ConsumerOptions_Get_Result(new EventConsumerOptions { Destination = "orders-queue" });
        fixture.Setup_Channel_BasicConsumeAsync_CapturesConsumer();

        SampleEvent? handled = null;
        await sut.StartupAsync((@event, _) =>
        {
            handled = @event;
            return Task.CompletedTask;
        }, CancellationToken.None);

        var body = JsonSerializer.SerializeToUtf8Bytes(new SampleEvent("payload"));

        // Act
        await fixture.CapturedConsumer!.HandleBasicDeliverAsync(
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
        fixture.Channel.Verify(c => c.BasicAckAsync(1UL, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Received_message_is_nacked_without_requeue_when_the_handler_throws()
    {
        // Arrange
        var fixture = new RabbitMqConsumerStartupFixture();
        fixture.Setup_RabbitOptions_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();
        fixture.Setup_ConsumerOptions_Get_Result(new EventConsumerOptions { Destination = "orders-queue" });
        fixture.Setup_Channel_BasicConsumeAsync_CapturesConsumer();

        await sut.StartupAsync((_, _) => throw new InvalidOperationException("boom"), CancellationToken.None);

        var body = JsonSerializer.SerializeToUtf8Bytes(new SampleEvent("payload"));

        // Act
        await fixture.CapturedConsumer!.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 7,
            redelivered: false,
            exchange: "orders-exchange",
            routingKey: nameof(SampleEvent),
            properties: new BasicProperties(),
            body: body,
            cancellationToken: CancellationToken.None);

        // Assert
        fixture.Channel.Verify(c => c.BasicNackAsync(7UL, false, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Received_message_is_nacked_without_requeue_when_the_body_cannot_be_deserialized()
    {
        // Arrange
        var fixture = new RabbitMqConsumerStartupFixture();
        fixture.Setup_RabbitOptions_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();
        fixture.Setup_ConsumerOptions_Get_Result(new EventConsumerOptions { Destination = "orders-queue" });
        fixture.Setup_Channel_BasicConsumeAsync_CapturesConsumer();

        await sut.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        var body = "null"u8.ToArray(); // valid JSON, but deserializes to a null SampleEvent

        // Act
        await fixture.CapturedConsumer!.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 3,
            redelivered: false,
            exchange: "orders-exchange",
            routingKey: nameof(SampleEvent),
            properties: new BasicProperties(),
            body: body,
            cancellationToken: CancellationToken.None);

        // Assert
        fixture.Channel.Verify(c => c.BasicNackAsync(3UL, false, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_disposes_the_channel_once_startup_has_run()
    {
        // Arrange
        var fixture = new RabbitMqConsumerStartupFixture();
        fixture.Setup_RabbitOptions_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();
        fixture.Setup_ConsumerOptions_Get_Result(new EventConsumerOptions { Destination = "orders-queue" });
        fixture.Setup_Channel_BasicConsumeAsync_CapturesConsumer();
        await sut.StartupAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Act
        await sut.DisposeAsync();

        // Assert
        fixture.Channel.Verify(c => c.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_does_not_throw_when_startup_was_never_called()
    {
        // Arrange
        var fixture = new RabbitMqConsumerStartupFixture();
        var sut = fixture.NewInstance();

        // Act
        var act = async () => await sut.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }
}
