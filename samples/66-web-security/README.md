# نمونهٔ درس 67 — CORS، CSRF و ورودی امن

راهنمای کامل، جدول حساب‌های موقت، راه‌اندازی PostgreSQL و تمرین در [درس 67](../../lessons/06-security/067-web-security/README.md) است.
این snapshot مستقل است؛ DB و key ring آن را با snapshot قبلی ترکیب نکن.

```powershell
dotnet build samples/66-web-security/SupportDesk.csproj
python scripts/security_next_smoke.py --build-only
python scripts/security_next_smoke.py
```

اجرای host به SUPPORTDESK_PG و TEAMDESK_KEYS نیاز دارد. برای init فقط در lab، TEAMDESK_LAB_PASSWORD را از ورودی امن بگیر و Development و TEAMDESK_INSECURE_LAB=1 را صریح تنظیم کن.
فرمان --init-lab شش حساب disposable با نقش و permission ثابت می‌سازد؛ HTTP route اعطای مجوز نداریم.
رمز و token را در repo، URL یا log نگذار. ارسال ایمیل و ثبت‌نام self-service و عملیات تولیدی حساب در این نمونه پیاده نشده‌اند.

ساخت موفق یا بررسی anonymous به معنی تأیید مجوز واقعی DB نیست؛ اجرای کامل اسکریپت، شش حساب و grant فعلی و publish و HTTP را بررسی می‌کند.
