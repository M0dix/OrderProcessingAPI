namespace OrderProcessingAPI.Domain.Events;

public static class EventTypes
{
    public const string OrderCreated = "OrderCreated";
}

public sealed record OrderCreated(
    Guid OrderId,
    string ProductSku,
    int Quantity,
    decimal Amount,
    string CustomerEmail);
