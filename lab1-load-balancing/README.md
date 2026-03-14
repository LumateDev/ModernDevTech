# Лабораторная работа №1 — Балансировка нагрузки, проксирование и кэширование

## Описание

HTTP-сервис на ASP.NET Core 9, демонстрирующий:

- **Балансировку нагрузки** — Nginx распределяет запросы между экземплярами сервиса по алгоритму Round Robin
- **Reverse Proxy** — клиент обращается к одному адресу (localhost:8080), реальные сервисы скрыты
- **Кэширование** — in-memory кэш на уровне каждого экземпляра сервиса

## Архитектура

```

                    ┌──────────────┐
                    │   Клиент     │
                    │  :8080       │
                    └──────┬───────┘
                           │
                    ┌──────▼───────┐
                    │    Nginx     │
                    │ Round Robin  │
                    └──┬───────┬───┘
                       │       │
              ┌────────▼──┐ ┌──▼────────┐
              │ service-1 │ │ service-2 │
              │   :8080   │ │   :8080   │
              └───────────┘ └───────────┘

```

## Эндпоинты

### GET /info

Возвращает информацию о текущем экземпляре:

```json
{
  "service": "service-1",
  "time": "2026-03-14T12:00:00"
}
```

### GET /data?id={id}

Возвращает данные с кэшированием:

Первый запрос:

```json
{
  "id": 1,
  "value": "Random data 93b760ad",
  "source": "generated"
}
```

Повторный запрос (при попадании на тот же сервис):

```json
{
  "id": 1,
  "value": "Random data 93b760ad",
  "source": "cache"
}
```

## Стек

- ASP.NET Core 9
- Nginx (Alpine)
- Docker / Docker Compose

## Запуск

```bash
docker-compose up --build -d
```

## Проверка

### Прямой доступ к экземплярам

```bash
curl http://localhost:8081/info
curl http://localhost:8082/info
```

### Балансировка нагрузки (Round Robin)

```bash
curl http://localhost:8080/info   # → service-1
curl http://localhost:8080/info   # → service-2
curl http://localhost:8080/info   # → service-1
curl http://localhost:8080/info   # → service-2
```

### Кэширование

```bash
curl "http://localhost:8080/data?id=1"   # → source: "generated"
curl "http://localhost:8080/data?id=1"   # → source: "cache" или "generated" (другой сервис)
curl "http://localhost:8080/data?id=1"   # → source: "cache"
```

## Остановка

```bash
docker-compose down
```
