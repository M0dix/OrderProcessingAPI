using System.Threading.Channels;

namespace OrderProcessingAPI.Infrastructure.Messaging;

public sealed class InMemoryMessageBroker : IMessageBroker
{
    private readonly Channel<BrokerMessage> _channel = Channel.CreateUnbounded<BrokerMessage>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask PublishAsync(BrokerMessage message, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(message, cancellationToken);

    public IAsyncEnumerable<BrokerMessage> SubscribeAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
