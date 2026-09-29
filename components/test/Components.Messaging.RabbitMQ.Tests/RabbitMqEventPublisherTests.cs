using System.Text.Json;
using Bogus;
using Components.Messaging.RabbitMQ.Tests.Fixtures;
using RabbitMQ.Client;

namespace Components.Messaging.RabbitMQ.Tests;

public class RabbitMqEventPublisherTests
{
    // Note on ordering: Setup_Options_ExchangeName_Result configures a value read once by
    // RabbitMqEventPublisher's constructor (IOptions<T> is frozen at construction, unlike a
    // Mock<T> queried lazily per call) - it must run before NewInstance(). Every other
    // Setup_... call configures mock behavior and runs after, per the repo convention.

    [Fact]
    public async Task PublishAsync_creates_a_channel_only_once_across_multiple_publishes()
    {
        // Arrange
        var fixture = new RabbitMqEventPublisherFixture();
        fixture.Setup_Options_ExchangeName_Result("test-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();
        fixture.Setup_Channel_IsOpen_Result(true);

        // Act
        await sut.PublishAsync(new SampleEvent("first"));
        await sut.PublishAsync(new SampleEvent("second"));

        // Assert
        fixture.Connection.Verify(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_declares_a_durable_topic_exchange_with_the_configured_name()
    {
        // Arrange
        var fixture = new RabbitMqEventPublisherFixture();
        fixture.Setup_Options_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();

        // Act
        await sut.PublishAsync(new SampleEvent("payload"));

        // Assert
        fixture.Channel.Verify(c => c.ExchangeDeclareAsync(
            "orders-exchange", ExchangeType.Topic, true, false,
            It.IsAny<IDictionary<string, object?>?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_uses_the_event_type_name_as_the_routing_key_when_no_message_key_is_given()
    {
        // Arrange
        var fixture = new RabbitMqEventPublisherFixture();
        fixture.Setup_Options_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();

        // Act
        await sut.PublishAsync(new SampleEvent("payload"));

        // Assert
        fixture.Channel.Verify(c => c.BasicPublishAsync(
            "orders-exchange", nameof(SampleEvent), false,
            It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_uses_the_explicit_message_key_as_the_routing_key_when_one_is_given()
    {
        // Arrange
        var fixture = new RabbitMqEventPublisherFixture();
        fixture.Setup_Options_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();

        // Act
        await sut.PublishAsync(new SampleEvent("payload"), messageKey: "campaign.published.v1");

        // Assert
        fixture.Channel.Verify(c => c.BasicPublishAsync(
            "orders-exchange", "campaign.published.v1", false,
            It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_publishes_to_the_explicit_destination_instead_of_the_default_exchange()
    {
        // Arrange
        var fixture = new RabbitMqEventPublisherFixture();
        fixture.Setup_Options_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();

        // Act
        await sut.PublishAsync(new SampleEvent("payload"), destination: "tenant-42-exchange");

        // Assert
        fixture.Channel.Verify(c => c.BasicPublishAsync(
            "tenant-42-exchange", It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_declares_each_distinct_destination_exchange_only_once()
    {
        // Arrange
        var fixture = new RabbitMqEventPublisherFixture();
        fixture.Setup_Options_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();

        // Act - default exchange twice, one override exchange twice
        await sut.PublishAsync(new SampleEvent("a"));
        await sut.PublishAsync(new SampleEvent("b"));
        await sut.PublishAsync(new SampleEvent("c"), destination: "tenant-42-exchange");
        await sut.PublishAsync(new SampleEvent("d"), destination: "tenant-42-exchange");

        // Assert
        fixture.Channel.Verify(c => c.ExchangeDeclareAsync(
            "orders-exchange", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<IDictionary<string, object?>?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        fixture.Channel.Verify(c => c.ExchangeDeclareAsync(
            "tenant-42-exchange", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<IDictionary<string, object?>?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_reuses_the_same_channel_when_publishing_to_different_destinations()
    {
        // Arrange
        var fixture = new RabbitMqEventPublisherFixture();
        fixture.Setup_Options_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();
        fixture.Setup_Channel_IsOpen_Result(true);

        // Act
        await sut.PublishAsync(new SampleEvent("a"));
        await sut.PublishAsync(new SampleEvent("b"), destination: "tenant-42-exchange");

        // Assert
        fixture.Connection.Verify(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_serializes_the_event_as_persistent_json()
    {
        // Arrange
        var fixture = new RabbitMqEventPublisherFixture();
        fixture.Setup_Options_ExchangeName_Result("orders-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();
        var @event = new SampleEvent(new Faker().Commerce.ProductName());

        BasicProperties? capturedProperties = null;
        ReadOnlyMemory<byte> capturedBody = default;
        fixture.Channel
            .Setup(c => c.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>(
                (_, _, _, properties, body, _) =>
                {
                    capturedProperties = properties;
                    capturedBody = body;
                })
            .Returns(ValueTask.CompletedTask);

        // Act
        await sut.PublishAsync(@event);

        // Assert
        capturedProperties!.Persistent.Should().BeTrue();
        capturedProperties.ContentType.Should().Be("application/json");
        JsonSerializer.Deserialize<SampleEvent>(capturedBody.Span).Should().Be(@event);
    }

    [Fact]
    public async Task DisposeAsync_disposes_the_channel_once_it_was_created()
    {
        // Arrange
        var fixture = new RabbitMqEventPublisherFixture();
        fixture.Setup_Options_ExchangeName_Result("test-exchange");
        var sut = fixture.NewInstance();
        fixture.Setup_Connection_CreateChannelAsync_Result();
        await sut.PublishAsync(new SampleEvent("payload"));

        // Act
        await sut.DisposeAsync();

        // Assert
        fixture.Channel.Verify(c => c.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_does_not_throw_when_no_channel_was_ever_created()
    {
        // Arrange
        var fixture = new RabbitMqEventPublisherFixture();
        fixture.Setup_Options_ExchangeName_Result("test-exchange");
        var sut = fixture.NewInstance();

        // Act
        var act = async () => await sut.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }
}
