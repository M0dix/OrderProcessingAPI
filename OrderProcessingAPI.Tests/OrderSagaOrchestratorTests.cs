using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderProcessingAPI.Domain.Entities;
using OrderProcessingAPI.Domain.Enums;
using OrderProcessingAPI.Infrastructure.Messaging;
using OrderProcessingAPI.Repositories;
using OrderProcessingAPI.Services;
using Xunit;

namespace OrderProcessingAPI.Tests;

public sealed class OrderSagaOrchestratorTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IInventoryService> _inventory = new();
    private readonly Mock<IPaymentService> _payments = new();
    private readonly Mock<IShippingService> _shipping = new();
    private readonly Mock<IIdempotencyStore> _idempotency = new();

    private OrderSagaOrchestrator CreateSut()
        => new(
            _orders.Object,
            _inventory.Object,
            _payments.Object,
            _shipping.Object,
            _idempotency.Object,
            NullLogger<OrderSagaOrchestrator>.Instance);

    [Fact]
    public async Task DuplicateMessage_DoesNotReserveAgain()
    {
        var messageId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _idempotency
            .Setup(x => x.TryMarkProcessedAsync(messageId, OrderSagaOrchestrator.HandlerName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await CreateSut().HandleOrderCreatedAsync(messageId, orderId, CancellationToken.None);

        _orders.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _inventory.Verify(x => x.ReserveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _payments.Verify(x => x.ChargeAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _shipping.Verify(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HappyPath_ReservesPaysAndShips_WithoutBroker()
    {
        var order = PendingOrder();
        SetupNewMessage();
        _orders.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orders.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _inventory.Setup(x => x.ReserveAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryReservation { OrderId = order.Id, Status = ReservationStatus.Reserved });
        _payments.Setup(x => x.ChargeAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentRecord { OrderId = order.Id, Status = PaymentStatus.Charged });
        _shipping.Setup(x => x.CreateAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Shipment { OrderId = order.Id, Status = ShipmentStatus.Created });

        await CreateSut().HandleOrderCreatedAsync(Guid.NewGuid(), order.Id, CancellationToken.None);

        Assert.Equal(OrderStatus.Shipped, order.Status);
        _inventory.Verify(x => x.ReserveAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _payments.Verify(x => x.ChargeAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _shipping.Verify(x => x.CreateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _inventory.Verify(x => x.ReleaseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PaymentFailure_CompensatesAndReleasesReservation()
    {
        var order = PendingOrder(failPayment: true);
        SetupNewMessage();
        _orders.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orders.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _inventory.Setup(x => x.ReserveAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryReservation { OrderId = order.Id, Status = ReservationStatus.Reserved });
        _payments.Setup(x => x.ChargeAsync(order, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Эмуляция отказа платёжного шлюза (FailPayment=true)."));
        _shipping.Setup(x => x.CancelAsync(order.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _payments.Setup(x => x.RefundAsync(order.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _inventory.Setup(x => x.ReleaseAsync(order.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await CreateSut().HandleOrderCreatedAsync(Guid.NewGuid(), order.Id, CancellationToken.None);

        Assert.Equal(OrderStatus.Compensated, order.Status);
        Assert.Contains("FailPayment", order.FailureReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        _shipping.Verify(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _shipping.Verify(x => x.CancelAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
        _payments.Verify(x => x.RefundAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
        _inventory.Verify(x => x.ReleaseAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ShipmentFailure_CompensatesInReverseOrder()
    {
        var order = PendingOrder(failShipment: true);
        SetupNewMessage();
        _orders.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orders.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _inventory.Setup(x => x.ReserveAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryReservation { OrderId = order.Id, Status = ReservationStatus.Reserved });
        _payments.Setup(x => x.ChargeAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentRecord { OrderId = order.Id, Status = PaymentStatus.Charged });
        _shipping.Setup(x => x.CreateAsync(order, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Эмуляция сбоя службы доставки (FailShipment=true)."));

        var sequence = new MockSequence();
        _shipping.InSequence(sequence)
            .Setup(x => x.CancelAsync(order.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _payments.InSequence(sequence)
            .Setup(x => x.RefundAsync(order.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _inventory.InSequence(sequence)
            .Setup(x => x.ReleaseAsync(order.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateSut().HandleOrderCreatedAsync(Guid.NewGuid(), order.Id, CancellationToken.None);

        Assert.Equal(OrderStatus.Compensated, order.Status);
        _shipping.Verify(x => x.CancelAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
        _payments.Verify(x => x.RefundAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
        _inventory.Verify(x => x.ReleaseAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupNewMessage()
        => _idempotency
            .Setup(x => x.TryMarkProcessedAsync(It.IsAny<Guid>(), OrderSagaOrchestrator.HandlerName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

    private static Order PendingOrder(bool failPayment = false, bool failShipment = false)
        => new()
        {
            Id = Guid.NewGuid(),
            CustomerEmail = "unit@example.com",
            ProductSku = "SKU-BOOK",
            Quantity = 1,
            Amount = 49.90m,
            Status = OrderStatus.Pending,
            FailPayment = failPayment,
            FailShipment = failShipment,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
}
