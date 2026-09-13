using OrderProcessingAPI.Domain.Entities;

namespace OrderProcessingAPI.Services;

public interface IPaymentService
{
    Task<PaymentRecord?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task<PaymentRecord> ChargeAsync(Order order, CancellationToken cancellationToken);
    Task RefundAsync(Guid orderId, CancellationToken cancellationToken);
}
