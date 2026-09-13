using Microsoft.EntityFrameworkCore;
using OrderProcessingAPI.Domain.Entities;
using OrderProcessingAPI.Infrastructure.Persistence;

namespace OrderProcessingAPI.Repositories;

public sealed class OutboxRepository(AppDbContext db) : IOutboxRepository
{
    public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        db.OutboxMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => db.OutboxMessages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OutboxMessage>> GetRecentAsync(int take, CancellationToken cancellationToken)
        => await db.OutboxMessages
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(int take, CancellationToken cancellationToken)
        => await db.OutboxMessages
            .Where(x => x.PublishedAtUtc == null)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task MarkPublishedAsync(Guid id, DateTime publishedAtUtc, CancellationToken cancellationToken)
    {
        var message = await db.OutboxMessages.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (message is null)
        {
            return;
        }

        message.PublishedAtUtc = publishedAtUtc;
        await db.SaveChangesAsync(cancellationToken);
    }
}
