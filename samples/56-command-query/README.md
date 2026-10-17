# نمونهٔ درس 057 — تفکیک command و query بدون اجبار ابزار

[درس فارسی، تمرین مستقل و معیار عبور](../../lessons/05-architecture/057-cqrs/README.md)

از ریشهٔ مخزن با .NET SDK 10 اجرا کن:

```powershell
dotnet run --project samples/56-command-query/SupportDesk.csproj -- --check
dotnet run --project samples/56-command-query/SupportDesk.csproj -- --urls http://127.0.0.1:5057
python scripts/architecture_final_smoke.py
```

اجرای عادی Web host است و تا Ctrl+C روشن می‌ماند.
پیش‌فرض، adapter حافظه و یک تیکت InProgress با Id=1 است؛ restart آن را reset می‌کند.
GET /api/tickets و GET /api/tickets/1 خواندن مستقل را نشان می‌دهند؛ queryها state را تغییر نمی‌دهند.
این snapshot package خارجی ندارد؛ query و command دو interface روی همان storage هستند.



## محدودیت تحویل

adapter فایل تک‌process و path یکسان را هماهنگ می‌کند؛ چند process هم‌زمان و قطع برق تضمین نمی‌شوند.
API پایدار PostgreSQL درس ۴۸ مرجع مستقل است و در این snapshot با portهای جدید یکپارچه نشده است.
مدیریت حساب، نقش و مالکیت، بخش بعدی امنیت است؛ این نمونه را معادل محصول چندسازمانی امن در نظر نگیر.

## بررسی خودکار

--check با failure از برنامه خارج می‌شود؛ موفقیت، پیام‌های PASS متناسب با query، validation و assembly و lifecycle می‌دهد.
اسکریپت Python همهٔ چهار نمونهٔ پایانی را می‌سازد و درخواست واقعی HTTP، ورودی خراب، audit dependency و publish نهایی را بررسی می‌کند.
در TeamDesk نهایی، create/resolve رقابتی، restart واقعی و عدم استفادهٔ مجدد از Id و حفظ فایل خراب بررسی می‌شوند.
نتیجهٔ واقعی هر commit در Actions ثبت می‌شود.
