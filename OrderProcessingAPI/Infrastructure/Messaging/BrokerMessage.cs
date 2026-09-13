namespace OrderProcessingAPI.Infrastructure.Messaging;

public sealed record BrokerMessage(Guid MessageId, string Type, string Payload);
