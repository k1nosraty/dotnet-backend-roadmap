# نمونهٔ درس 052 — application service و orchestration

[متن فارسی درس و تمرین مستقل](../../lessons/05-architecture/052-application-services/README.md)

از ریشهٔ مخزن با .NET SDK 10 اجرا کن:

```powershell
dotnet run --project samples/51-application-services/SupportDesk.csproj
dotnet run --project samples/51-application-services/SupportDesk.csproj -- --check
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
ResolveTicket علاوه بر این‌ها، شکست مرحلهٔ دوم، note تکراری و سقف note، commit واحد و خطای مصنوعی store را بررسی می‌کند.
این بررسی‌های حافظه‌ای اثبات SQL یا durability نیستند؛ آزمایش‌های PostgreSQL بخش چهارم جدا هستند.

برای publish نمونهٔ چندپروژه‌ای، از مسیر پروژهٔ host استفاده کن و تمام پوشهٔ خروجی را منتقل کن:

```powershell
dotnet publish samples/51-application-services/SupportDesk.csproj -c Release -o artifacts/lesson-52
dotnet artifacts/lesson-52/SupportDesk.dll --check
```

پوشهٔ artifacts را commit نکن. فایل‌های Program و Verification مسئول نمایش و بررسی‌اند؛ قواعد اصلی در Domain و هماهنگی در Application قرار دارند.
