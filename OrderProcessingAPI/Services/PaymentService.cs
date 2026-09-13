using OrderProcessingAPI.Domain.Entities;
using OrderProcessingAPI.Domain.Enums;
using OrderProcessingAPI.Repositories;

namespace OrderProcessingAPI.Services;

public sealed class PaymentService(IPaymentRepository payments) : IPaymentService
{
    public Task<PaymentRecord?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
        => payments.GetByOrderIdAsync(orderId, cancellationToken);

    public async Task<PaymentRecord> ChargeAsync(Order order, CancellationToken cancellationToken)
    {
        var existing = await payments.GetByOrderIdAsync(order.Id, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var payment = new PaymentRecord
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Amount = order.Amount,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        if (order.FailPayment)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = "Эмуляция отказа платёжного шлюза (FailPayment=true).";
            await payments.AddAsync(payment, cancellationToken);
            await payments.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException(payment.FailureReason);
        }

        payment.Status = PaymentStatus.Charged;
        await payments.AddAsync(payment, cancellationToken);
        await payments.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task RefundAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByOrderIdAsync(orderId, cancellationToken);
        if (payment is null || payment.Status != PaymentStatus.Charged)
        {
            return;
        }

        payment.Status = PaymentStatus.Refunded;
        payment.UpdatedAtUtc = DateTime.UtcNow;
        await payments.SaveChangesAsync(cancellationToken);
    }
}
