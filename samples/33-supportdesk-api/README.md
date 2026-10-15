# SupportDesk — درس ۳۴

[متن کامل درس](../../lessons/03-web/034-api-delivery/README.md) · [فهرست بخش سوم](../../lessons/03-web/README.md)

## تغییر این مرحله

افزودن notes، status transition، MutationResult محافظت‌شده و mutation اتمیک؛ حفظ قواعد پنج note غیرتکراری و منع ویرایش Closed از بخش دوم.

این snapshot مستقل است؛ فایل‌های مرحلهٔ قبل را برای اجرای آن لازم نداری.
داده پس از restart پاک می‌شود و فقط در همان process مشترک است.

## اجرا

```powershell
dotnet run --project samples/33-supportdesk-api/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5340
```

فرمان از ریشهٔ repository است. GET /health باید ۲۰۰ بدهد.
مسیرهای Ticket با /api/tickets شروع می‌شوند.
برای diagnostics و سند OpenAPI در مرحله‌های مربوط، DOTNET_ENVIRONMENT را پیش از اجرا Development کن.
در Production این مسیرهای آموزشی map نمی‌شوند.

## فایل‌ها

- `Application/ITicketStore.cs`
- `Application/MutationResult.cs`
- `Application/RequestStamp.cs`
- `Application/TicketService.cs`
- `Configuration/TicketOptions.cs`
- `Contracts/AddNoteRequest.cs`
- `Contracts/ChangeStatusRequest.cs`
- `Contracts/CreateTicketRequest.cs`
- `Contracts/PageResponse.cs`
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

## قواعد note و وضعیت

متن note پس از Trim بین ۳ و ۵۰۰ است؛ حداکثر پنج note با مقایسهٔ تکرار Ordinal.
rename و افزودن note پس از Closed مجاز نیستند.
ورودی خراب ۴۰۰، منبع ناموجود ۴۰۴ و تعارض قواعد ۴۰۹ می‌دهد.
تغییر وضعیت Open به InProgress و سپس Closed با حداقل یک note مجاز است.
درخواست همان وضعیت فعلی no-op موفق است.

## بررسی

```powershell
python scripts/controller_smoke.py
python scripts/domain_smoke.py
```

اسکریپت از ریشهٔ دوره همهٔ نه نمونهٔ controller را build و روی loopback اجرا می‌کند.
قرارداد دقیق و تمرین‌های این snapshot در متن درس قرار دارند.
این API محلی جای نسخهٔ احرازهویت‌شده با دیتابیس پایدار را نمی‌گیرد.
