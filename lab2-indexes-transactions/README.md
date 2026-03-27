# Лабораторная работа №2 — Индексы, транзакции, уровни изоляции, кэширование

## Стек

- ASP.NET Core 9
- PostgreSQL 16
- Redis 7
- Docker / Docker Compose
- xUnit (тесты)

---

## Запуск

Поднять все контейнеры (API, PostgreSQL, Redis, xUnit):

```bash
docker-compose up --build -d
```

````

API будет доступно по адресу: [http://localhost:8080](http://localhost:8080)
Swagger UI: [http://localhost:8080/swagger](http://localhost:8080/swagger)

---

## Часть 1. Индексы

Наполнить таблицу `users` 10 000 записями:

```bash
curl -X POST http://localhost:8080/api/indexdemo/seed
```

Поиск **без индекса** (Seq Scan):

```bash
curl http://localhost:8080/api/indexdemo/search-no-index?email=user_9999@example.com
```

Поиск **с индексом** (Index Scan):

```bash
curl http://localhost:8080/api/indexdemo/search-with-index?email=user_9999@example.com
```

Ожидаемый результат: поиск с индексом выполняется быстрее и использует `Index Scan` (или `Bitmap Scan`).

---

## Часть 2. Транзакции (ACID)

Инициализировать счета (id=1,2, баланс 1000):

```bash
curl -X POST http://localhost:8080/api/transaction/seed
```

Успешный перевод:

```bash
curl -X POST "http://localhost:8080/api/transaction/transfer?from=1&to=2&amount=200"
```

Перевод с ошибкой (демонстрация атомарности):

```bash
curl -X POST http://localhost:8080/api/transaction/reset
curl -X POST "http://localhost:8080/api/transaction/transfer-fail?from=1&to=2&amount=200"
```

Проверить балансы:

```bash
curl http://localhost:8080/api/transaction/balances
```

Ожидаемый результат: при ошибке балансы не изменяются (откат транзакции).

---

## Часть 3. Уровни изоляции

Эндпоинты демонстрируют поведение транзакций при разных уровнях изоляции:

- **READ COMMITTED** – предотвращает грязное чтение
- **READ UNCOMMITTED** (PostgreSQL приводит к READ COMMITTED)
- **REPEATABLE READ** – снимок данных на момент начала транзакции
- **SERIALIZABLE** – полная изоляция

```bash
curl http://localhost:8080/api/isolation/read-committed
curl http://localhost:8080/api/isolation/read-uncommitted
curl http://localhost:8080/api/isolation/repeatable-read
curl http://localhost:8080/api/isolation/serializable
```

Ответы содержат пояснения, какие значения были прочитаны в каждой из транзакций.

---

## Часть 4. Кэширование (Cache Aside + Redis)

Первый запрос — данные из БД, сохраняются в Redis (TTL 60 сек):

```bash
curl http://localhost:8080/api/cache/user/1
```

Повторный запрос — данные из кэша:

```bash
curl http://localhost:8080/api/cache/user/1
```

Очистить кэш для пользователя:

```bash
curl -X DELETE http://localhost:8080/api/cache/user/1
```

Статистика Redis (количество ключей):

```bash
curl http://localhost:8080/api/cache/stats
```

---

## Запуск тестов

Тесты автоматически запускаются при старте контейнера `tests`. Для запуска вручную используйте:

```bash
docker-compose up tests
```

Если требуется выполнить тесты локально (вне Docker), убедитесь, что PostgreSQL и Redis запущены, и выполните:

```bash
dotnet test
```

Результаты тестов включают вывод времени выполнения, планы запросов и проверки ACID/изоляции/кэширования.

---

## Полный сброс окружения

Чтобы удалить все данные (включая PostgreSQL volume) и начать с чистого состояния:

```bash
docker-compose down -v
```

После этого можно снова поднять контейнеры:

```bash
docker-compose up --build -d
```

---

## Структура проекта

```
lab2-indexes-transactions/
├── .dockerignore
├── docker-compose.yml
├── README.md
├── Lab2.IndexesTransactions.Api/       # Web API
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   ├── Services/
│   └── ...
├── Lab2.IndexesTransactions.Tests/     # xUnit тесты
│   ├── CacheTests.cs
│   ├── IndexesTests.cs
│   ├── IsolationTests.cs
│   ├── TransactionTests.cs
│   ├── IntegrationTestFactory.cs
│   └── Models/
└── ...
```

## Остановка

Остановить все контейнеры и удалить volumes (включая данные):

```bash
docker-compose down -v
```
````
