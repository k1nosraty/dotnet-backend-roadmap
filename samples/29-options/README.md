# SupportDesk — درس ۳۰

[متن کامل درس](../../lessons/03-web/030-configuration/README.md) · [فهرست بخش سوم](../../lessons/03-web/README.md)

## تغییر این مرحله

افزودن TicketOptions با ValidateOnStart، environment override و endpoint آموزشی settings.

این snapshot مستقل است؛ فایل‌های مرحلهٔ قبل را برای اجرای آن لازم نداری.
داده پس از restart پاک می‌شود و فقط در همان process مشترک است.

## اجرا

```powershell
dotnet run --project samples/29-options/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5300
```

فرمان از ریشهٔ repository است. GET /health باید ۲۰۰ بدهد.
مسیرهای Ticket با /api/tickets شروع می‌شوند.
برای diagnostics و سند OpenAPI در مرحله‌های مربوط، DOTNET_ENVIRONMENT را پیش از اجرا Development کن.
در Production این مسیرهای آموزشی map نمی‌شوند.

## فایل‌ها

- `Application/ITicketStore.cs`
- `Application/TicketService.cs`
- `Configuration/TicketOptions.cs`
- `Contracts/CreateTicketRequest.cs`
- `Contracts/ReplaceTitleRequest.cs`
- `Contracts/TicketResponse.cs`
- `Controllers/TicketsController.cs`
- `Domain/Ticket.cs`
- `Domain/TicketTitle.cs`
- `Hosting/ApiExceptionHandler.cs`
- `Hosting/RequestIdMiddleware.cs`
- `Infrastructure/InMemoryTicketStore.cs`
- `Mapping/TicketMapping.cs`
- `Program.cs`
- `SupportDesk.csproj`
- `appsettings.json`

## بررسی

```powershell
python scripts/controller_smoke.py
```

اسکریپت از ریشهٔ دوره همهٔ نه نمونهٔ controller را build و روی loopback اجرا می‌کند.
قرارداد دقیق و تمرین‌های این snapshot در متن درس قرار دارند.
این API محلی جای نسخهٔ احرازهویت‌شده با دیتابیس پایدار را نمی‌گیرد.
