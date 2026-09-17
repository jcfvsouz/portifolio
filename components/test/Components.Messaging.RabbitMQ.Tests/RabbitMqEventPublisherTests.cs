using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Components.Messaging.RabbitMQ.Tests;

public class RabbitMqEventPublisherTests
{
    private record SampleEvent(string Name);

    private static (IConnection Connection, IChannel Channel) ConnectionReturningAnOpenChannel()
    {
        var channel = Substitute.For<IChannel>();
        channel.IsOpen.Returns(true);

        var connection = Substitute.For<IConnection>();
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>())
            .Returns(channel);

        return (connection, channel);
    }

    private static IOptions<RabbitMqOptions> OptionsWithExchange(string exchangeName) =>
        Options.Create(new RabbitMqOptions { ExchangeName = exchangeName });

    [Fact]
    public async Task PublishAsync_creates_a_channel_only_once_across_multiple_publishes()
    {
        // Arrange
        var (connection, _) = ConnectionReturningAnOpenChannel();
        var publisher = new RabbitMqEventPublisher(connection, OptionsWithExchange("test-exchange"));

        // Act
        await publisher.PublishAsync(new SampleEvent("first"));
        await publisher.PublishAsync(new SampleEvent("second"));

        // Assert
        await connection.Received(1).CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_declares_a_durable_topic_exchange_with_the_configured_name()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var publisher = new RabbitMqEventPublisher(connection, OptionsWithExchange("orders-exchange"));

        // Act
        await publisher.PublishAsync(new SampleEvent("payload"));

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
    public async Task PublishAsync_uses_the_event_type_name_as_the_routing_key_when_no_message_key_is_given()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var publisher = new RabbitMqEventPublisher(connection, OptionsWithExchange("orders-exchange"));

        // Act
        await publisher.PublishAsync(new SampleEvent("payload"));

        // Assert
        await channel.Received(1).BasicPublishAsync(
            exchange: Arg.Is("orders-exchange"),
            routingKey: Arg.Is(nameof(SampleEvent)),
            mandatory: Arg.Is(false),
            basicProperties: Arg.Any<BasicProperties>(),
            body: Arg.Any<ReadOnlyMemory<byte>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_uses_the_explicit_message_key_as_the_routing_key_when_one_is_given()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var publisher = new RabbitMqEventPublisher(connection, OptionsWithExchange("orders-exchange"));

        // Act
        await publisher.PublishAsync(new SampleEvent("payload"), messageKey: "campaign.published.v1");

        // Assert
        await channel.Received(1).BasicPublishAsync(
            exchange: Arg.Is("orders-exchange"),
            routingKey: Arg.Is("campaign.published.v1"),
            mandatory: Arg.Is(false),
            basicProperties: Arg.Any<BasicProperties>(),
            body: Arg.Any<ReadOnlyMemory<byte>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_publishes_to_the_explicit_destination_instead_of_the_default_exchange()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var publisher = new RabbitMqEventPublisher(connection, OptionsWithExchange("orders-exchange"));

        // Act
        await publisher.PublishAsync(new SampleEvent("payload"), destination: "tenant-42-exchange");

        // Assert
        await channel.Received(1).BasicPublishAsync(
            exchange: Arg.Is("tenant-42-exchange"),
            routingKey: Arg.Any<string>(),
            mandatory: Arg.Any<bool>(),
            basicProperties: Arg.Any<BasicProperties>(),
            body: Arg.Any<ReadOnlyMemory<byte>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_declares_each_distinct_destination_exchange_only_once()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var publisher = new RabbitMqEventPublisher(connection, OptionsWithExchange("orders-exchange"));

        // Act — default exchange twice, one override exchange twice
        await publisher.PublishAsync(new SampleEvent("a"));
        await publisher.PublishAsync(new SampleEvent("b"));
        await publisher.PublishAsync(new SampleEvent("c"), destination: "tenant-42-exchange");
        await publisher.PublishAsync(new SampleEvent("d"), destination: "tenant-42-exchange");

        // Assert
        await channel.Received(1).ExchangeDeclareAsync(
            exchange: Arg.Is("orders-exchange"),
            type: Arg.Any<string>(),
            durable: Arg.Any<bool>(),
            autoDelete: Arg.Any<bool>(),
            arguments: Arg.Any<IDictionary<string, object?>?>(),
            passive: Arg.Any<bool>(),
            noWait: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>());
        await channel.Received(1).ExchangeDeclareAsync(
            exchange: Arg.Is("tenant-42-exchange"),
            type: Arg.Any<string>(),
            durable: Arg.Any<bool>(),
            autoDelete: Arg.Any<bool>(),
            arguments: Arg.Any<IDictionary<string, object?>?>(),
            passive: Arg.Any<bool>(),
            noWait: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_reuses_the_same_channel_when_publishing_to_different_destinations()
    {
        // Arrange
        var (connection, _) = ConnectionReturningAnOpenChannel();
        var publisher = new RabbitMqEventPublisher(connection, OptionsWithExchange("orders-exchange"));

        // Act
        await publisher.PublishAsync(new SampleEvent("a"));
        await publisher.PublishAsync(new SampleEvent("b"), destination: "tenant-42-exchange");

        // Assert
        await connection.Received(1).CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_serializes_the_event_as_persistent_json()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var publisher = new RabbitMqEventPublisher(connection, OptionsWithExchange("orders-exchange"));
        var @event = new SampleEvent("payload");

        ReadOnlyMemory<byte> capturedBody = default;
        var capturedProperties = new BasicProperties();
        channel.When(c => c.BasicPublishAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<BasicProperties>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>()))
            .Do(callInfo =>
            {
                capturedProperties = callInfo.ArgAt<BasicProperties>(3);
                capturedBody = callInfo.ArgAt<ReadOnlyMemory<byte>>(4);
            });

        // Act
        await publisher.PublishAsync(@event);

        // Assert
        capturedProperties.Persistent.Should().BeTrue();
        capturedProperties.ContentType.Should().Be("application/json");
        JsonSerializer.Deserialize<SampleEvent>(capturedBody.Span).Should().Be(@event);
    }

    [Fact]
    public async Task DisposeAsync_disposes_the_underlying_channel_once_it_was_created()
    {
        // Arrange
        var (connection, channel) = ConnectionReturningAnOpenChannel();
        var publisher = new RabbitMqEventPublisher(connection, OptionsWithExchange("test-exchange"));
        await publisher.PublishAsync(new SampleEvent("payload"));

        // Act
        await publisher.DisposeAsync();

        // Assert
        await channel.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_does_not_throw_when_no_channel_was_ever_created()
    {
        // Arrange
        var (connection, _) = ConnectionReturningAnOpenChannel();
        var publisher = new RabbitMqEventPublisher(connection, OptionsWithExchange("test-exchange"));

        // Act
        var act = async () => await publisher.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }
}
