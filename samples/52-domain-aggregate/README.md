# نمونهٔ درس 053 — قواعد domain و aggregate

[درس فارسی، تمرین و معیار عبور](../../lessons/05-architecture/053-domain/README.md)

از ریشهٔ مخزن با .NET SDK 10 اجرا کن:

```powershell
dotnet run --project samples/52-domain-aggregate/SupportDesk.csproj -- --check
python scripts/architecture_next_smoke.py
```

اجرای عادی نتیجهٔ یک use case را چاپ می‌کند:

```powershell
dotnet run --project samples/52-domain-aggregate/SupportDesk.csproj
```

خروجی عادی `Success: 1 Closed notes=1` است. هر اجرای حافظه‌ای seed تازه دارد.
این نمونه مستقل و چهارپروژه‌ای است؛ host سه کتابخانهٔ Domain و Application و Infrastructure را build می‌کند.
package خارجی تازه یا PostgreSQL لازم نیست؛ persistence دیتابیس در نمونهٔ درس ۴۸ باقی مانده است.

## بررسی‌ها

تمام نمونه‌ها قواعد قبلی orchestration و aggregate و بازسازی دادهٔ نامعتبر را بررسی می‌کنند.



هر failure بررسی exit غیرصفر دارد؛ این بررسی‌ها تضمین همهٔ شرایط محصول یا خرابی سخت‌افزار نیستند.

## محدودیت‌ها

host پیش‌فرض از حافظه استفاده می‌کند و با restart state را از دست می‌دهد.
مدل این snapshot فقط با رفتارهای Domain و factory معتبر تغییر می‌کند.
این مرحله هنوز مدیریت کاربر، مجوز، گزارش کامل یا محصول TeamDesk نهایی نیست.
