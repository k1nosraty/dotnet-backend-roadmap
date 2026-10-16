# نمونهٔ درس 044

[آموزش کامل](../../lessons/04-data/044-tracking/README.md)

از ریشهٔ مخزن:

```powershell
dotnet run --project samples/43-tracking/SupportDesk.csproj
```

.NET SDK 10 و PostgreSQL لازم است. [اتصال تمرین](../../handbook/postgresql-lab.md) را تنظیم کن.
فایل Program سناریوی اجرایی و assertionها را دارد؛ Persistence mapping و Infrastructure مالک آزمایش است.
موفقیت با PASS: lesson scenario و exit code صفر مشخص می‌شود.
هر اجرا schema تازه می‌سازد و آن را پاک می‌کند؛ --keep فقط برای بازرسی است.
