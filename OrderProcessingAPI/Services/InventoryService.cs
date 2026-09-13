using OrderProcessingAPI.Domain.Entities;
using OrderProcessingAPI.Domain.Enums;
using OrderProcessingAPI.Repositories;

namespace OrderProcessingAPI.Services;

public sealed class InventoryService(IInventoryRepository inventory) : IInventoryService
{
    public Task<ProductStock?> GetStockAsync(string sku, CancellationToken cancellationToken)
        => inventory.GetStockAsync(sku, cancellationToken);

    public Task<IReadOnlyList<ProductStock>> GetAllStockAsync(CancellationToken cancellationToken)
        => inventory.GetAllStockAsync(cancellationToken);

    public Task<InventoryReservation?> GetReservationAsync(Guid orderId, CancellationToken cancellationToken)
        => inventory.GetReservationByOrderIdAsync(orderId, cancellationToken);

    public async Task<InventoryReservation> ReserveAsync(Order order, CancellationToken cancellationToken)
    {
        var existing = await inventory.GetReservationByOrderIdAsync(order.Id, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var stock = await inventory.GetStockAsync(order.ProductSku, cancellationToken)
            ?? throw new InvalidOperationException($"Товар {order.ProductSku} не найден.");

        if (stock.Available < order.Quantity)
        {
            throw new InvalidOperationException(
                $"Недостаточно товара {order.ProductSku}: нужно {order.Quantity}, доступно {stock.Available}.");
        }

        stock.Available -= order.Quantity;
        var reservation = new InventoryReservation
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductSku = order.ProductSku,
            Quantity = order.Quantity,
            Status = ReservationStatus.Reserved,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await inventory.AddReservationAsync(reservation, cancellationToken);
        await inventory.SaveChangesAsync(cancellationToken);
        return reservation;
    }

    public async Task ReleaseAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var reservation = await inventory.GetReservationByOrderIdAsync(orderId, cancellationToken);
        if (reservation is null || reservation.Status == ReservationStatus.Released)
        {
            return;
        }

        var stock = await inventory.GetStockAsync(reservation.ProductSku, cancellationToken);
        if (stock is not null)
        {
            stock.Available += reservation.Quantity;
        }

        reservation.Status = ReservationStatus.Released;
        reservation.UpdatedAtUtc = DateTime.UtcNow;
        await inventory.SaveChangesAsync(cancellationToken);
    }
}
