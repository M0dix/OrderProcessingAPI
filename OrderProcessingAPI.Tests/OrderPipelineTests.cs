using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrderProcessingAPI.Contracts.Dtos;
using OrderProcessingAPI.Domain.Enums;
using Xunit;

namespace OrderProcessingAPI.Tests;

[Collection("OrderApi")]
public sealed class OrderPipelineTests : IClassFixture<OrderApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public OrderPipelineTests(OrderApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_HappyPath_ReservesPaysAndShips()
    {
        var created = await CreateOrderAsync("ok@example.com");
        Assert.Equal(OrderStatus.Pending, created.Status);

        var order = await WaitForStatusAsync(created.Id, OrderStatus.Shipped);
        Assert.Equal(ReservationStatus.Reserved, order.Reservation?.Status);
        Assert.Equal(PaymentStatus.Charged, order.Payment?.Status);
        Assert.Equal(ShipmentStatus.Created, order.Shipment?.Status);
        Assert.False(string.IsNullOrWhiteSpace(order.Shipment?.TrackingNumber));
    }

    [Fact]
    public async Task CreateOrder_WhenPaymentFails_CompensatesAndReleasesStock()
    {
        var stockBefore = await GetStockAsync("SKU-BOOK");

        var created = await CreateOrderAsync("fail-pay@example.com", failPayment: true);
        var order = await WaitForStatusAsync(created.Id, OrderStatus.Compensated);

        Assert.Equal(ReservationStatus.Released, order.Reservation?.Status);
        Assert.Equal(PaymentStatus.Failed, order.Payment?.Status);
        Assert.Null(order.Shipment);
        Assert.Contains("FailPayment", order.FailureReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        var stockAfter = await GetStockAsync("SKU-BOOK");
        Assert.Equal(stockBefore.Available, stockAfter.Available);
    }

    [Fact]
    public async Task Outbox_WhenPublisherPaused_KeepsUnpublishedEvent_ThenRecovers()
    {
        var pause = await _client.PostAsync("/api/demo/outbox/pause", content: null);
        pause.EnsureSuccessStatusCode();

        try
        {
            var created = await CreateOrderAsync("crash@example.com");
            var pending = await GetOrderAsync(created.Id);
            Assert.Equal(OrderStatus.Pending, pending.Status);

            var unpublished = (await GetOutboxAsync())
                .First(x => !x.Published);
            Assert.Equal("OrderCreated", unpublished.Type);
            Assert.Null(unpublished.PublishedAtUtc);

            var resume = await _client.PostAsync("/api/demo/outbox/resume", content: null);
            resume.EnsureSuccessStatusCode();

            var recovered = await WaitForStatusAsync(created.Id, OrderStatus.Shipped);
            Assert.Equal(OrderStatus.Shipped, recovered.Status);
            Assert.True((await GetOutboxAsync()).All(x => x.Published));
        }
        finally
        {
            await _client.PostAsync("/api/demo/outbox/resume", content: null);
        }
    }

    [Fact]
    public async Task Replay_OrderCreated_DoesNotCreateSecondReservation()
    {
        var created = await CreateOrderAsync("replay@example.com");
        var shipped = await WaitForStatusAsync(created.Id, OrderStatus.Shipped);
        Assert.NotNull(shipped.Reservation);

        var message = (await GetOutboxAsync()).First(x => x.Type == "OrderCreated");
        var replay = await _client.PostAsync($"/api/demo/replay/{message.Id}", content: null);
        replay.EnsureSuccessStatusCode();

        var afterReplay = await GetOrderAsync(created.Id);
        Assert.Equal(OrderStatus.Shipped, afterReplay.Status);
        Assert.Equal(shipped.Reservation!.Id, afterReplay.Reservation!.Id);
        Assert.Equal(ReservationStatus.Reserved, afterReplay.Reservation.Status);
        Assert.Equal(PaymentStatus.Charged, afterReplay.Payment?.Status);
    }

    private async Task<OrderResponse> CreateOrderAsync(
        string email,
        bool failPayment = false,
        bool failShipment = false)
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerEmail = email,
            ProductSku = "SKU-BOOK",
            Quantity = 1,
            FailPayment = failPayment,
            FailShipment = failShipment
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        Assert.NotNull(body);
        return body;
    }

    private async Task<OrderResponse> GetOrderAsync(Guid id)
    {
        var order = await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{id}", JsonOptions);
        Assert.NotNull(order);
        return order;
    }

    private async Task<StockResponse> GetStockAsync(string sku)
    {
        var stock = await _client.GetFromJsonAsync<List<StockResponse>>("/api/inventory", JsonOptions);
        var item = stock?.FirstOrDefault(x => x.Sku == sku);
        Assert.NotNull(item);
        return item;
    }

    private async Task<List<OutboxMessageResponse>> GetOutboxAsync()
    {
        var items = await _client.GetFromJsonAsync<List<OutboxMessageResponse>>("/api/outbox", JsonOptions);
        Assert.NotNull(items);
        return items;
    }

    private async Task<OrderResponse> WaitForStatusAsync(Guid id, params OrderStatus[] expected)
    {
        for (var i = 0; i < 40; i++)
        {
            var order = await GetOrderAsync(id);
            if (expected.Contains(order.Status))
            {
                return order;
            }

            await Task.Delay(150);
        }

        var last = await GetOrderAsync(id);
        throw new Xunit.Sdk.XunitException(
            $"Заказ {id} остался в статусе {last.Status}, ждали {string.Join("/", expected)}.");
    }
}
