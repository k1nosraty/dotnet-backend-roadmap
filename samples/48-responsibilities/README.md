# نمونهٔ درس 049 — تشخیص مسئولیت کلاس‌ها

[متن فارسی درس و تمرین مستقل](../../lessons/05-architecture/049-responsibilities/README.md)

از ریشهٔ مخزن با .NET SDK 10 اجرا کن:

```powershell
dotnet run --project samples/48-responsibilities/SupportDesk.csproj
dotnet run --project samples/48-responsibilities/SupportDesk.csproj -- --check
python scripts/architecture_smoke.py
```

خروجی عادی: `Success: 1 Closed notes=1`.
خروجی بررسی: `PASS: architecture behavior`.
اجرای بررسی با assertion شکست‌خورده exit غیرصفر دارد.
این snapshot مستقل است؛ هر اجرا با seed تازه شروع می‌شود و داده در حافظه است.
به PostgreSQL یا package خارجی نیاز ندارد و جای API پایدار درس ۴۸ نیست.

این مرحله یک assembly دارد؛ پوشه و namespace به‌تنهایی مرز دسترسی compile ایجاد نمی‌کنند.

## بررسی‌ها

شناسهٔ نامعتبر و ناموجود، قواعد بستن، ثبات داده پس از شکست، دو نویسنده با snapshot یکسان، لغو و حفظ هویت بررسی می‌شوند.

این بررسی‌های حافظه‌ای اثبات SQL یا durability نیستند؛ آزمایش‌های PostgreSQL بخش چهارم جدا هستند.

برای publish نمونهٔ چندپروژه‌ای، از مسیر پروژهٔ host استفاده کن و تمام پوشهٔ خروجی را منتقل کن:

```powershell
dotnet publish samples/48-responsibilities/SupportDesk.csproj -c Release -o artifacts/lesson-49
dotnet artifacts/lesson-49/SupportDesk.dll --check
```

پوشهٔ artifacts را commit نکن. فایل‌های Program و Verification مسئول نمایش و بررسی‌اند؛ قواعد اصلی در Domain و هماهنگی در Application قرار دارند.
