using OrderProcessingAPI.Domain.Entities;
using OrderProcessingAPI.Domain.Enums;
using OrderProcessingAPI.Infrastructure.Messaging;
using OrderProcessingAPI.Repositories;

namespace OrderProcessingAPI.Services;

public sealed class OrderSagaOrchestrator(
    IOrderRepository orders,
    IInventoryService inventory,
    IPaymentService payments,
    IShippingService shipping,
    IIdempotencyStore idempotency,
    ILogger<OrderSagaOrchestrator> logger)
{
    public const string HandlerName = "OrderCreatedSaga";

    public async Task HandleOrderCreatedAsync(Guid messageId, Guid orderId, CancellationToken cancellationToken)
    {
        if (!await idempotency.TryMarkProcessedAsync(messageId, HandlerName, cancellationToken))
        {
            logger.LogInformation(
                "Дубль OrderCreated {MessageId} для заказа {OrderId} — повторный резерв не создаём.",
                messageId,
                orderId);
            return;
        }

        var order = await orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning("Заказ {OrderId} не найден, сага пропущена.", orderId);
            return;
        }

        if (order.Status is OrderStatus.Shipped or OrderStatus.Compensated or OrderStatus.Failed)
        {
            logger.LogInformation("Заказ {OrderId} уже в статусе {Status}, сага не стартует.", order.Id, order.Status);
            return;
        }

        try
        {
            await inventory.ReserveAsync(order, cancellationToken);
            await SetStatusAsync(order, OrderStatus.Reserved, cancellationToken);
            logger.LogInformation("Заказ {OrderId}: товар зарезервирован.", order.Id);

            await payments.ChargeAsync(order, cancellationToken);
            await SetStatusAsync(order, OrderStatus.Paid, cancellationToken);
            logger.LogInformation("Заказ {OrderId}: оплата прошла.", order.Id);

            await shipping.CreateAsync(order, cancellationToken);
            await SetStatusAsync(order, OrderStatus.Shipped, cancellationToken);
            logger.LogInformation("Заказ {OrderId}: отгрузка создана.", order.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Заказ {OrderId}: шаг саги упал, запускаем компенсации.", order.Id);
            await CompensateAsync(order, ex.Message, cancellationToken);
        }
    }

    private async Task CompensateAsync(Order order, string reason, CancellationToken cancellationToken)
    {
        await SetStatusAsync(order, OrderStatus.Failed, cancellationToken, reason);

        try
        {
            await shipping.CancelAsync(order.Id, cancellationToken);
            await payments.RefundAsync(order.Id, cancellationToken);
            await inventory.ReleaseAsync(order.Id, cancellationToken);
            await SetStatusAsync(order, OrderStatus.Compensated, cancellationToken, reason);
            logger.LogInformation("Заказ {OrderId}: компенсации выполнены, резерв откатан.", order.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Заказ {OrderId}: компенсация не завершилась полностью.", order.Id);
            await SetStatusAsync(order, OrderStatus.Failed, cancellationToken, $"{reason}; compensation: {ex.Message}");
        }
    }

    private async Task SetStatusAsync(
        Order order,
        OrderStatus status,
        CancellationToken cancellationToken,
        string? failureReason = null)
    {
        order.Status = status;
        order.FailureReason = failureReason;
        order.UpdatedAtUtc = DateTime.UtcNow;
        await orders.SaveChangesAsync(cancellationToken);
    }
}
