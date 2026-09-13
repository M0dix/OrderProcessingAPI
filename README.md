## Данные при старте:

| SKU | Товар | Остаток | Цена |
|-----|-------|---------|------|
| `SKU-BOOK` | Distributed Systems in Practice | 20 | 49.90 |
| `SKU-MUG` | Outbox Pattern Mug | 8 | 15.00 |

БД — SQLite `orders.db` рядом с процессом. Очередь — `Channel` в памяти.

## Устройство работы

1. `POST /api/orders` — валидация запроса (наличие товара, цена) и запись заказа и события в одну транзакцию.
2. `OutboxPublisher` — фоновый сервис, читает неотправленные события и отправляет в очередь.
3. `OrderCreatedConsumer` — получает событие и запускает шаги бизнес-цепочки. Дубли отфильтровываются по таблице `ProcessedMessages`.
4. При ошибке любого шага выполняются компенсации: `CancelShipment` → `Refund` → `ReleaseReservation`.

## API

| Метод | Путь | Назначение |
|-------|------|------------|
| `POST` | `/api/orders` | Создать заказ |
| `GET` | `/api/orders/{id}` | Заказ с деталями |
| `GET` | `/api/orders` | Последние заказы |
| `GET` | `/api/inventory` | Остатки |
| `GET` | `/api/outbox` | Журнал событий |
| `POST` | `/api/demo/outbox/pause` | Остановить отправку |
| `POST` | `/api/demo/outbox/resume` | Продолжить отправку |
| `POST` | `/api/demo/replay/{id}` | Повторная доставка события |
