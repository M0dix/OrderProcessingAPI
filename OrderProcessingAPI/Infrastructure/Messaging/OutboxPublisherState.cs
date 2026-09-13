namespace OrderProcessingAPI.Infrastructure.Messaging;

public sealed class OutboxPublisherState
{
    public bool Enabled { get; set; } = true;
}
