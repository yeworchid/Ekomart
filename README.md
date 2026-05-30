## Проект

**Название:** Ekomart  
**Тип:** веб-приложение на ASP.NET Core MVC  
**Предметная область:** интернет-магазин с пользовательской витриной, корзиной, заказами, подписками и административной панелью.

Цель проекта — реализовать рабочее MVC-приложение для демонстрации основных пользовательских и административных сценариев интернет-магазина с разграничением доступа, нормализованной БД, подписками, AJAX-операциями, логированием и развёртыванием.

## Быстрый запуск

### Запуск через Docker

Из корня проекта:

```bash
docker compose up --build
```

После запуска сайт доступен по адресу:

```text
http://localhost:5016
```

`docker-compose.yml` поднимает PostgreSQL, MongoDB, одноразовый `seeder` для миграций и тестовых данных, а затем веб-приложение `Ekomart.Web`.

Проверить контейнеры:

```bash
docker compose ps
```

Остановить проект:

```bash
docker compose down
```

Сбросить БД и seed-данные:

```bash
docker compose down -v
docker compose up --build
```

### Локальный запуск без Docker-контейнера для Web

Если веб-приложение запускается локально через .NET, а базы остаются в Docker:

```bash
docker compose up -d postgres mongo
dotnet run --project Ekomart.Seeder/Ekomart.Seeder.csproj
dotnet run --project Ekomart.Web/Ekomart.Web.csproj --urls http://localhost:5016
```

### Тестовые учётные записи

Пароль для всех seed-пользователей:

```text
Ekomart123!
```

| Роль | Email |
| --- | --- |
| User | `user@ekomart.test` |
| Manager | `manager@ekomart.test` |
| Admin | `admin@ekomart.test` |
