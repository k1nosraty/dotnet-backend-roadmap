# نمونهٔ درس 062 — Identity و مدیریت امن حساب

[درس فارسی، راه‌اندازی، تمرین و معیار عبور](../../lessons/06-security/062-identity/README.md)

.NET SDK 10 و Python 3 لازم‌اند. بررسی ساخت و مرز بستهٔ HTTP:

```powershell
dotnet build samples/61-identity/SupportDesk.csproj
python scripts/security_smoke.py --build-only
```

برای اجرای زنده، SUPPORTDESK_PG را به دیتابیس PostgreSQL اختصاصی آزمایش تنظیم کن.
اجرای عادی آزمایش کنسولی حساب است: dotnet run --project samples/61-identity/SupportDesk.csproj. هر اجرا schema تصادفی می‌سازد و همان را پاک می‌کند؛ --keep برای بازرسی دستی است. هیچ password، hash یا token چاپ نمی‌شود.


## بررسی زنده

```powershell
python scripts/security_smoke.py
```

PostgreSQL و برای cleanup آزمایش HTTP به psql نیاز است. تنظیمات SUPPORTDESK_TEST_PG* در [فهرست بخش امنیت](../../lessons/06-security/README.md) آمده است.
--build-only اجرای SQL، reset و authentication واقعی را ثابت نمی‌کند.
این چهار درس هنوز سازمان، نقش، مالکیت و امنیت production کامل TeamDesk نیستند؛ درس‌های ۶۵ تا ۷۰ ادامه دارند.
