using OrderProcessingAPI.Domain.Entities;

namespace OrderProcessingAPI.Repositories;

public interface IPaymentRepository
{
    Task<PaymentRecord?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task AddAsync(PaymentRecord payment, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
