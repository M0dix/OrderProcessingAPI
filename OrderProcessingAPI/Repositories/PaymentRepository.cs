using Microsoft.EntityFrameworkCore;
using OrderProcessingAPI.Domain.Entities;
using OrderProcessingAPI.Infrastructure.Persistence;

namespace OrderProcessingAPI.Repositories;

public sealed class PaymentRepository(AppDbContext db) : IPaymentRepository
{
    public Task<PaymentRecord?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
        => db.Payments.FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);

    public Task AddAsync(PaymentRecord payment, CancellationToken cancellationToken)
    {
        db.Payments.Add(payment);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
