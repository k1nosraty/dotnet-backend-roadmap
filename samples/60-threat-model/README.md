# نمونهٔ درس 061 — مدل تهدید و مرز اعتماد

[درس فارسی، راه‌اندازی، تمرین و معیار عبور](../../lessons/06-security/061-threat-model/README.md)

.NET SDK 10 و Python 3 لازم‌اند. بررسی ساخت و مرز بستهٔ HTTP:

```powershell
dotnet build samples/60-threat-model/SupportDesk.csproj
python scripts/security_smoke.py --build-only
```

این sample یک Web host است؛ /health عمومی و /api/tickets همیشه بسته است. ClosedGate عمداً هیچ هویتی را قبول نمی‌کند و provider واقعی نیست. با --urls http://127.0.0.1:5061 روی loopback اجرا کن.



## بررسی زنده

```powershell
python scripts/security_smoke.py
```

PostgreSQL و برای cleanup آزمایش HTTP به psql نیاز است. تنظیمات SUPPORTDESK_TEST_PG* در [فهرست بخش امنیت](../../lessons/06-security/README.md) آمده است.
--build-only اجرای SQL، reset و authentication واقعی را ثابت نمی‌کند.
این چهار درس هنوز سازمان، نقش، مالکیت و امنیت production کامل TeamDesk نیستند؛ درس‌های ۶۵ تا ۷۰ ادامه دارند.
