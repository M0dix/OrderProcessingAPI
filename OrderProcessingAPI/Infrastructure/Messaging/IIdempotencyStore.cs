namespace OrderProcessingAPI.Infrastructure.Messaging;

public interface IIdempotencyStore
{
    Task<bool> TryMarkProcessedAsync(Guid messageId, string handler, CancellationToken cancellationToken);
}
