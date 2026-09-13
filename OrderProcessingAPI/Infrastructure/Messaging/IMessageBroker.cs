namespace OrderProcessingAPI.Infrastructure.Messaging;

public interface IMessageBroker
{
    ValueTask PublishAsync(BrokerMessage message, CancellationToken cancellationToken);
    IAsyncEnumerable<BrokerMessage> SubscribeAsync(CancellationToken cancellationToken);
}
