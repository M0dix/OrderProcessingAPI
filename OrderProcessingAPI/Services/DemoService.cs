using System.Text.Json;
using OrderProcessingAPI.Contracts.Dtos;
using OrderProcessingAPI.Domain.Events;
using OrderProcessingAPI.Infrastructure.Messaging;
using OrderProcessingAPI.Repositories;

namespace OrderProcessingAPI.Services;

public sealed class DemoService(
    IOutboxRepository outbox,
    OrderSagaOrchestrator saga,
    OutboxPublisherState publisherState)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public object PausePublisher()
    {
        publisherState.Enabled = false;
        return new
        {
            publisherEnabled = false,
            message = "Публикатор остановлен. Новые заказы останутся в outbox с PublishedAtUtc = null — как после падения процесса сразу после коммита."
        };
    }

    public object ResumePublisher()
    {
        publisherState.Enabled = true;
        return new
        {
            publisherEnabled = true,
            message = "Публикатор включён. Неотправленные события из outbox будут опубликованы."
        };
    }

    public object PublisherStatus()
        => new { publisherEnabled = publisherState.Enabled };

    public async Task<ReplayResult> ReplayOrderCreatedAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var message = await outbox.GetByIdAsync(messageId, cancellationToken)
            ?? throw new KeyNotFoundException("Сообщение outbox не найдено.");

        if (message.Type != EventTypes.OrderCreated)
        {
            throw new InvalidOperationException($"Тип {message.Type} не поддерживается для replay.");
        }

        var payload = JsonSerializer.Deserialize<OrderCreated>(message.Payload, JsonOptions)
            ?? throw new InvalidOperationException("Не удалось разобрать payload.");

        await saga.HandleOrderCreatedAsync(messageId, payload.OrderId, cancellationToken);

        return new ReplayResult
        {
            MessageId = messageId,
            Duplicate = true,
            Message = "Повторная доставка обработана. Второй резерв не создаётся — проверьте GET /api/orders/{id}."
        };
    }
}
