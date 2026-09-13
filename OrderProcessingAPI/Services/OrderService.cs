using System.Text.Json;
using OrderProcessingAPI.Contracts.Dtos;
using OrderProcessingAPI.Domain.Entities;
using OrderProcessingAPI.Domain.Enums;
using OrderProcessingAPI.Domain.Events;
using OrderProcessingAPI.Infrastructure.Persistence;
using OrderProcessingAPI.Repositories;
using OrderProcessingAPI.Services.Mapping;

namespace OrderProcessingAPI.Services;

public sealed class OrderService(
    AppDbContext db,
    IOrderRepository orders,
    IOutboxRepository outbox,
    IInventoryRepository inventory,
    IPaymentRepository payments,
    IShipmentRepository shipments)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var stock = await inventory.GetStockAsync(request.ProductSku, cancellationToken)
            ?? throw new InvalidOperationException($"Товар {request.ProductSku} не найден.");

        if (stock.Available < request.Quantity)
        {
            throw new InvalidOperationException(
                $"Недостаточно товара {request.ProductSku}: нужно {request.Quantity}, доступно {stock.Available}.");
        }

        var now = DateTime.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerEmail = request.CustomerEmail,
            ProductSku = request.ProductSku,
            Quantity = request.Quantity,
            Amount = stock.Price * request.Quantity,
            Status = OrderStatus.Pending,
            FailPayment = request.FailPayment,
            FailShipment = request.FailShipment,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var created = new OrderCreated(
            order.Id,
            order.ProductSku,
            order.Quantity,
            order.Amount,
            order.CustomerEmail);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = EventTypes.OrderCreated,
            Payload = JsonSerializer.Serialize(created, JsonOptions),
            CreatedAtUtc = now
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await orders.AddAsync(order, cancellationToken);
        await outbox.AddAsync(outboxMessage, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return OrderMapper.ToResponse(order);
    }

    public async Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var reservation = await inventory.GetReservationByOrderIdAsync(id, cancellationToken);
        var payment = await payments.GetByOrderIdAsync(id, cancellationToken);
        var shipment = await shipments.GetByOrderIdAsync(id, cancellationToken);
        return OrderMapper.ToResponse(order, reservation, payment, shipment);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetRecentAsync(int take, CancellationToken cancellationToken)
    {
        var items = await orders.GetRecentAsync(take, cancellationToken);
        return items.Select(x => OrderMapper.ToResponse(x)).ToList();
    }

    public async Task<IReadOnlyList<OutboxMessageResponse>> GetOutboxAsync(CancellationToken cancellationToken)
    {
        var messages = await outbox.GetRecentAsync(50, cancellationToken);
        return messages.Select(OrderMapper.ToOutbox).ToList();
    }
}
