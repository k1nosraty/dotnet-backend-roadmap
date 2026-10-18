# نمونهٔ درس 064 — cookie و bearer و انتخاب روش

[درس فارسی، راه‌اندازی، تمرین و معیار عبور](../../lessons/06-security/064-authentication/README.md)

.NET SDK 10 و Python 3 لازم‌اند. بررسی ساخت و مرز بستهٔ HTTP:

```powershell
dotnet build samples/63-cookie-bearer/SupportDesk.csproj
python scripts/security_smoke.py --build-only
```

برای اجرای زنده، SUPPORTDESK_PG را به دیتابیس PostgreSQL اختصاصی آزمایش تنظیم کن.

این host فقط پس از راه‌اندازی lab طبق درس ۶۴ اجرا می‌شود: TEAMDESK_KEYS، اتصال با schema تازه و --init-lab لازم‌اند. حساب نمونه از env محلی provision می‌شود و endpoint ثبت‌نام یا ایمیل خودکار عمومی نداریم. init فقط در Development با TEAMDESK_INSECURE_LAB=1 مجاز است. host عادی DDL اجرا نمی‌کند. کلید و DB ثابت در restart حفظ می‌شوند؛ مسیرهای cookie تغییر‌دهنده CSRF می‌خواهند.

## بررسی زنده

```powershell
python scripts/security_smoke.py
```

PostgreSQL و برای cleanup آزمایش HTTP به psql نیاز است. تنظیمات SUPPORTDESK_TEST_PG* در [فهرست بخش امنیت](../../lessons/06-security/README.md) آمده است.
--build-only اجرای SQL، reset و authentication واقعی را ثابت نمی‌کند.
این چهار درس هنوز سازمان، نقش، مالکیت و امنیت production کامل TeamDesk نیستند؛ درس‌های ۶۵ تا ۷۰ ادامه دارند.
