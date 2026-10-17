# نمونهٔ درس 051 — جداسازی assembly و جهت وابستگی

[متن فارسی درس و تمرین مستقل](../../lessons/05-architecture/051-projects/README.md)

از ریشهٔ مخزن با .NET SDK 10 اجرا کن:

```powershell
dotnet run --project samples/50-projects/SupportDesk.csproj
dotnet run --project samples/50-projects/SupportDesk.csproj -- --check
python scripts/architecture_smoke.py
```

خروجی عادی: `Success: 1 Closed notes=1`.
خروجی بررسی: `PASS: architecture behavior`.
اجرای بررسی با assertion شکست‌خورده exit غیرصفر دارد.
این snapshot مستقل است؛ هر اجرا با seed تازه شروع می‌شود و داده در حافظه است.
به PostgreSQL یا package خارجی نیاز ندارد و جای API پایدار درس ۴۸ نیست.

پروژهٔ ریشه host است و سه کتابخانه زیر src دارد. build host آن‌ها را هم می‌سازد؛ سورس کتابخانه‌ها دوباره در host compile نمی‌شود.

## بررسی‌ها

شناسهٔ نامعتبر و ناموجود، قواعد بستن، ثبات داده پس از شکست، دو نویسنده با snapshot یکسان، لغو و حفظ هویت بررسی می‌شوند.

این بررسی‌های حافظه‌ای اثبات SQL یا durability نیستند؛ آزمایش‌های PostgreSQL بخش چهارم جدا هستند.

برای publish نمونهٔ چندپروژه‌ای، از مسیر پروژهٔ host استفاده کن و تمام پوشهٔ خروجی را منتقل کن:

```powershell
dotnet publish samples/50-projects/SupportDesk.csproj -c Release -o artifacts/lesson-51
dotnet artifacts/lesson-51/SupportDesk.dll --check
```

پوشهٔ artifacts را commit نکن. فایل‌های Program و Verification مسئول نمایش و بررسی‌اند؛ قواعد اصلی در Domain و هماهنگی در Application قرار دارند.
