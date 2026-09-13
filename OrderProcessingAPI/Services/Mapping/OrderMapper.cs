using OrderProcessingAPI.Contracts.Dtos;
using OrderProcessingAPI.Domain.Entities;

namespace OrderProcessingAPI.Services.Mapping;

public static class OrderMapper
{
    public static OrderResponse ToResponse(
        Order order,
        InventoryReservation? reservation = null,
        PaymentRecord? payment = null,
        Shipment? shipment = null)
        => new()
        {
            Id = order.Id,
            CustomerEmail = order.CustomerEmail,
            ProductSku = order.ProductSku,
            Quantity = order.Quantity,
            Amount = order.Amount,
            Status = order.Status,
            FailureReason = order.FailureReason,
            CreatedAtUtc = order.CreatedAtUtc,
            UpdatedAtUtc = order.UpdatedAtUtc,
            Reservation = reservation is null
                ? null
                : new ReservationDto
                {
                    Id = reservation.Id,
                    ProductSku = reservation.ProductSku,
                    Quantity = reservation.Quantity,
                    Status = reservation.Status
                },
            Payment = payment is null
                ? null
                : new PaymentDto
                {
                    Id = payment.Id,
                    Amount = payment.Amount,
                    Status = payment.Status,
                    FailureReason = payment.FailureReason
                },
            Shipment = shipment is null
                ? null
                : new ShipmentDto
                {
                    Id = shipment.Id,
                    TrackingNumber = shipment.TrackingNumber,
                    Status = shipment.Status
                }
        };

    public static StockResponse ToStock(ProductStock stock)
        => new()
        {
            Sku = stock.Sku,
            Name = stock.Name,
            Available = stock.Available,
            Price = stock.Price
        };

    public static OutboxMessageResponse ToOutbox(OutboxMessage message)
        => new()
        {
            Id = message.Id,
            Type = message.Type,
            CreatedAtUtc = message.CreatedAtUtc,
            PublishedAtUtc = message.PublishedAtUtc
        };
}
