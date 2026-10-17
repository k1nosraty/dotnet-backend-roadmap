# نمونهٔ درس 055 — mapping و هزینهٔ abstraction

[درس فارسی، تمرین و معیار عبور](../../lessons/05-architecture/055-mapping/README.md)

از ریشهٔ مخزن با .NET SDK 10 اجرا کن:

```powershell
dotnet run --project samples/54-explicit-mapping/SupportDesk.csproj -- --check
python scripts/architecture_next_smoke.py
```

اجرای عادی نتیجهٔ یک use case را چاپ می‌کند:

```powershell
dotnet run --project samples/54-explicit-mapping/SupportDesk.csproj
```

خروجی عادی `Success: 1 Closed notes=1` است. هر اجرای حافظه‌ای seed تازه دارد.
این نمونه مستقل و چهارپروژه‌ای است؛ host سه کتابخانهٔ Domain و Application و Infrastructure را build می‌کند.
package خارجی تازه یا PostgreSQL لازم نیست؛ persistence دیتابیس در نمونهٔ درس ۴۸ باقی مانده است.

## بررسی‌ها

تمام نمونه‌ها قواعد قبلی orchestration و aggregate و بازسازی دادهٔ نامعتبر را بررسی می‌کنند.
نمونه‌های ۵۴ به بعد قرارداد دو adapter حافظه و JSON و حفظ فایل خراب را هم بررسی می‌کنند.
mapping ورودی و فیلدهای دقیق response و جلوگیری از نوشتن state از body هم بررسی می‌شوند.

هر failure بررسی exit غیرصفر دارد؛ این بررسی‌ها تضمین همهٔ شرایط محصول یا خرابی سخت‌افزار نیستند.

## محدودیت‌ها

host پیش‌فرض از حافظه استفاده می‌کند و با restart state را از دست می‌دهد.
adapter JSON در Infrastructure برای مقایسهٔ portهاست: یک path یکسان در یک process هماهنگ می‌شود؛ چند process هم‌زمان، symlink alias و قطع برق تضمین نمی‌شوند. مسیر فایل را فقط در CLI آزمایشی درس‌های ۵۴ و ۵۵ می‌توان با --file تعیین کرد؛ مسیر را ورودی مستقیم مشتری وب نکن.
این مرحله هنوز مدیریت کاربر، مجوز، گزارش کامل یا محصول TeamDesk نهایی نیست.
