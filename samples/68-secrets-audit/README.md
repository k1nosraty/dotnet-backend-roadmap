# نمونهٔ درس 69 — پایان امنیت

[متن درس و راه‌اندازی](../../lessons/06-security/069-secrets/README.md) شامل env و جدول حساب‌های موقت و محدودیت‌هاست.
این snapshot مستقل و آموزشی است؛ از schema و key نسخهٔ دیگر استفاده نکن.

```powershell
dotnet build samples/68-secrets-audit/SupportDesk.csproj
python scripts/security_final_smoke.py --build-only
python scripts/security_final_smoke.py
```

SUPPORTDESK_PG با schema تازهٔ auth69_ و TEAMDESK_KEYS با مسیر absolute خصوصی لازم است.
--init-lab فقط در Development و TEAMDESK_INSECURE_LAB=1 و با password موقت از env حساب می‌سازد؛ ایمیل یا ثبت‌نام production نداریم.
Audit تیکت و تغییر آن در PostgreSQL اتمیک‌اند؛ console log شاهد اتمیک یا tamper-proof نیست.
اسکریپت کامل نیاز به PostgreSQL و psql دارد و فقط schemaهای تصادفی خودش را حذف می‌کند.
