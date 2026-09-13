using System.Text.Json;
using OrderProcessingAPI.Domain.Events;
using OrderProcessingAPI.Services;

namespace OrderProcessingAPI.Infrastructure.Messaging;

public sealed class OrderCreatedConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IMessageBroker broker;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<OrderCreatedConsumer> logger;

    public OrderCreatedConsumer(
        IMessageBroker broker,
        IServiceScopeFactory scopeFactory,
        ILogger<OrderCreatedConsumer> logger)
    {
        this.broker = broker;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OrderCreatedConsumer запущен.");

        await foreach (var message in broker.SubscribeAsync(stoppingToken))
        {
            try
            {
                await HandleAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка обработки {MessageId} ({Type}). Сообщение не ack — at-least-once.",
                    message.MessageId, message.Type);
            }
        }
    }

    private async Task HandleAsync(BrokerMessage message, CancellationToken cancellationToken)
    {
        if (message.Type != EventTypes.OrderCreated)
        {
            return;
        }

        var payload = JsonSerializer.Deserialize<OrderCreated>(message.Payload, JsonOptions)
            ?? throw new InvalidOperationException("Не удалось разобрать OrderCreated.");

        using var scope = scopeFactory.CreateScope();
        var saga = scope.ServiceProvider.GetRequiredService<OrderSagaOrchestrator>();
        await saga.HandleOrderCreatedAsync(message.MessageId, payload.OrderId, cancellationToken);
    }
}
