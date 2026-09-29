using Components.Messaging.RabbitMQ.Tests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Components.Messaging.RabbitMQ.Tests.Fixtures;

public class RabbitMqConsumerStartupFixture
{
    public Mock<IConnection> Connection { get; private set; } = new();
    public Mock<IChannel> Channel { get; private set; } = new();
    public Mock<IOptionsMonitor<EventConsumerOptions>> ConsumerOptions { get; private set; } = new();
    public IOptions<RabbitMqOptions> RabbitOptions { get; private set; } = Microsoft.Extensions.Options.Options.Create(new RabbitMqOptions());
    public ILogger<RabbitMqConsumerStartup<SampleEvent>> Logger { get; private set; } = NullLogger<RabbitMqConsumerStartup<SampleEvent>>.Instance;
    public IAsyncBasicConsumer? CapturedConsumer { get; private set; }

    public void ConfigureMocks()
    {
        Connection = new();
        Channel = new();
        ConsumerOptions = new();
        Logger = NullLogger<RabbitMqConsumerStartup<SampleEvent>>.Instance;
        CapturedConsumer = null;
        // RabbitOptions is not a Mock<T> - it's a value captured once by the SUT's primary
        // constructor, not queried lazily per call, so it's deliberately left untouched
        // here. A test that wants a specific ExchangeName must call
        // Setup_RabbitOptions_ExchangeName_Result(...) before NewInstance(), not after.
    }

    public RabbitMqConsumerStartup<SampleEvent> NewInstance()
    {
        ConfigureMocks();
        return new(Connection.Object, ConsumerOptions.Object, RabbitOptions, Logger);
    }

    public void Setup_Connection_CreateChannelAsync_Result() =>
        Connection
            .Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Channel.Object);

    public void Setup_ConsumerOptions_Get_Result(EventConsumerOptions options) =>
        ConsumerOptions.Setup(o => o.Get(nameof(SampleEvent))).Returns(options);

    public void Setup_RabbitOptions_ExchangeName_Result(string exchangeName) =>
        RabbitOptions = Microsoft.Extensions.Options.Options.Create(new RabbitMqOptions { ExchangeName = exchangeName });

    public void Setup_Channel_BasicConsumeAsync_CapturesConsumer() =>
        Channel
            .Setup(c => c.BasicConsumeAsync(
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object?>?>(), It.IsAny<IAsyncBasicConsumer>(), It.IsAny<CancellationToken>()))
            .Callback<string, bool, string, bool, bool, IDictionary<string, object?>?, IAsyncBasicConsumer, CancellationToken>(
                (_, _, _, _, _, _, consumer, _) => CapturedConsumer = consumer)
            .ReturnsAsync(string.Empty);
}
