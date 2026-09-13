using Microsoft.Extensions.DependencyInjection;
using OrderProcessingAPI.Infrastructure.Persistence;
using OrderProcessingAPI.Repositories;

namespace OrderProcessingAPI.Infrastructure.Messaging;

public sealed class OutboxPublisher : BackgroundService
{
    private readonly IMessageBroker broker;
    private readonly OutboxPublisherState state;
    private readonly ILogger<OutboxPublisher> logger;
    private readonly IServiceScopeFactory scopeFactory;

    public OutboxPublisher(
        IMessageBroker broker,
        OutboxPublisherState state,
        ILogger<OutboxPublisher> logger,
        IServiceScopeFactory scopeFactory)
    {
        this.broker = broker;
        this.state = state;
        this.logger = logger;
        this.scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxPublisher запущен.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (state.Enabled)
                {
                    await PublishPendingAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка публикации outbox.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var pending = await outbox.GetUnpublishedAsync(20, cancellationToken);

        foreach (var message in pending)
        {
            await broker.PublishAsync(
                new BrokerMessage(message.Id, message.Type, message.Payload),
                cancellationToken);

            await outbox.MarkPublishedAsync(message.Id, DateTime.UtcNow, cancellationToken);
            logger.LogInformation("Outbox {MessageId} ({Type}) опубликован.", message.Id, message.Type);
        }
    }
}
