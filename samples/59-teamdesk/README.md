# نمونهٔ درس 060 — بازآرایی TeamDesk و دفاع از معماری

[درس فارسی، تمرین مستقل و معیار عبور](../../lessons/05-architecture/060-teamdesk/README.md)

از ریشهٔ مخزن با .NET SDK 10 اجرا کن:

```powershell
dotnet run --project samples/59-teamdesk/SupportDesk.csproj -- --check
dotnet run --project samples/59-teamdesk/SupportDesk.csproj -- --urls http://127.0.0.1:5060
python scripts/architecture_final_smoke.py
```

اجرای عادی Web host است و تا Ctrl+C روشن می‌ماند.
پیش‌فرض، adapter حافظه و یک تیکت InProgress با Id=1 است؛ restart آن را reset می‌کند.
GET /api/tickets و GET /api/tickets/1 خواندن مستقل را نشان می‌دهند؛ queryها state را تغییر نمی‌دهند.
FluentValidation 12.2.0 در Application pin شده است و اولین restore به NuGet نیاز دارد. validator صریحاً با ValidateAsync اجرا می‌شود؛ Domain از این package مستقل است.
ArchitectureChecks وابستگی assemblyها را بررسی می‌کند و docs/adr چهار تصمیم معماری را ثبت می‌کند.
نسخهٔ TeamDesk ثبت و حذف، note و وضعیت و resolve و عنوان و خواندن را یکپارچه می‌کند. برای فایل آزمایشی پایدار، TEAMDESK_DATA را به یک فایل تازه در پوشهٔ موجود بده. سند نسخهٔ ۲ و NextId مستقل دارد؛ فایل نسخهٔ ۱ درس‌های قبلی را استفاده نکن. تمام فرمان‌ها، قرارداد HTTP و ارزیابی تحویل در درس ۶۰ آمده‌اند.

## محدودیت تحویل

adapter فایل تک‌process و path یکسان را هماهنگ می‌کند؛ چند process هم‌زمان و قطع برق تضمین نمی‌شوند.
API پایدار PostgreSQL درس ۴۸ مرجع مستقل است و در این snapshot با portهای جدید یکپارچه نشده است.
مدیریت حساب، نقش و مالکیت، بخش بعدی امنیت است؛ این نمونه را معادل محصول چندسازمانی امن در نظر نگیر.

## بررسی خودکار

--check با failure از برنامه خارج می‌شود؛ موفقیت، پیام‌های PASS متناسب با query، validation و assembly و lifecycle می‌دهد.
اسکریپت Python همهٔ چهار نمونهٔ پایانی را می‌سازد و درخواست واقعی HTTP، ورودی خراب، audit dependency و publish نهایی را بررسی می‌کند.
در TeamDesk نهایی، create/resolve رقابتی، restart واقعی و عدم استفادهٔ مجدد از Id و حفظ فایل خراب بررسی می‌شوند.
نتیجهٔ واقعی هر commit در Actions ثبت می‌شود.
