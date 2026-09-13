using OrderProcessingAPI.Domain.Entities;

namespace OrderProcessingAPI.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken);
    Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<OutboxMessage>> GetRecentAsync(int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(int take, CancellationToken cancellationToken);
    Task MarkPublishedAsync(Guid id, DateTime publishedAtUtc, CancellationToken cancellationToken);
}
