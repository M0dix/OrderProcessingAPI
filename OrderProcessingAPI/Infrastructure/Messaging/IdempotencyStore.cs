using Microsoft.EntityFrameworkCore;
using OrderProcessingAPI.Domain.Entities;
using OrderProcessingAPI.Infrastructure.Persistence;

namespace OrderProcessingAPI.Infrastructure.Messaging;

public sealed class IdempotencyStore(AppDbContext db) : IIdempotencyStore
{
    public async Task<bool> TryMarkProcessedAsync(Guid messageId, string handler, CancellationToken cancellationToken)
    {
        var exists = await db.ProcessedMessages
            .AnyAsync(x => x.MessageId == messageId && x.Handler == handler, cancellationToken);

        if (exists)
        {
            return false;
        }

        db.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = messageId,
            Handler = handler,
            ProcessedAtUtc = DateTime.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }
}
