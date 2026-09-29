using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Components.Messaging.RabbitMQ.Tests.Fixtures;

public class RabbitMqEventPublisherFixture
{
    public Mock<IConnection> Connection { get; private set; } = new();
    public Mock<IChannel> Channel { get; private set; } = new();
    public IOptions<RabbitMqOptions> Options { get; private set; } = Microsoft.Extensions.Options.Options.Create(new RabbitMqOptions());

    public void ConfigureMocks()
    {
        Connection = new();
        Channel = new();
        // Options is not a Mock<T> - it's a value read once by the SUT's constructor, not
        // queried lazily per call, so it's deliberately left untouched here. A test that
        // wants a specific ExchangeName must call Setup_Options_ExchangeName_Result(...)
        // before NewInstance(), not after.
    }

    public RabbitMqEventPublisher NewInstance()
    {
        ConfigureMocks();
        return new(Connection.Object, Options);
    }

    public void Setup_Connection_CreateChannelAsync_Result() =>
        Connection
            .Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Channel.Object);

    public void Setup_Channel_IsOpen_Result(bool isOpen) =>
        Channel.Setup(c => c.IsOpen).Returns(isOpen);

    public void Setup_Options_ExchangeName_Result(string exchangeName) =>
        Options = Microsoft.Extensions.Options.Options.Create(new RabbitMqOptions { ExchangeName = exchangeName });
}
