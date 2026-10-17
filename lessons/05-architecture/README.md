# بخش پنجم — معماری و مرز مسئولیت‌ها

درس‌های ۴۹ تا ۵۲ آماده‌اند؛ درس‌های ۵۳ تا ۶۰ همچنان برنامه‌ریزی‌شده‌اند.
پیش‌نیاز، بخش چهارم و شناخت Domain، Application و دسترسی به داده است.
در این گروه ابتدا مسئولیت را مشخص می‌کنیم، سپس چیدمان فایل و مرز compile و orchestration را بررسی می‌کنیم.

| درس | موضوع | وضعیت |
| --- | --- | --- |
| 049 | [تشخیص مسئولیت کلاس‌ها](049-responsibilities/README.md) | نوشته‌شده |
| 050 | [ساختار پوشه‌ای بر اساس لایه یا قابلیت](050-folders/README.md) | نوشته‌شده |
| 051 | [جداسازی assembly و جهت وابستگی](051-projects/README.md) | نوشته‌شده |
| 052 | [application service و orchestration](052-application-services/README.md) | نوشته‌شده |

## مسیر عملی

نمونه‌های این گروه مستقل، کنسولی و حافظه‌ای هستند تا مقایسهٔ معماری به تنظیم PostgreSQL وابسته نباشد.
نسخهٔ پایدار بخش چهارم حفظ شده است؛ ذخیرهٔ حافظه‌ای این نمونه‌ها جای آن را نمی‌گیرد.
برای ساخت هر نمونه فرمان متن همان درس را اجرا کن؛ SDK 10 لازم است و package تازه‌ای اضافه نشده است.

```powershell
python scripts/check_content.py
python scripts/architecture_smoke.py
```

اسکریپت دوم رفتار هر چهار snapshot، جابه‌جایی بدون تغییر رفتار، referenceهای مستقیم، خطاهای compile عمدی و خروجی publish را بررسی می‌کند.
روی Windows و Linux در workflow دوره اجرا می‌شود؛ نتیجهٔ واقعی هر commit در Actions مشخص است.
مدل immutable و کنترل نسخهٔ این adapter فقط داخل process تضمین دارند؛ PostgreSQL و HTTP در اسکریپت‌های بخش چهارم جدا بررسی می‌شوند.

## مرز این مرحله

این بخش هنوز تحویل نهایی TeamDesk یا مدیریت کاربر و مجوز نیست.
aggregate، port/adapter، mapping، vertical slice، CQRS، FluentValidation، تست معماری و تحویل TeamDesk موضوع درس‌های بعدی‌اند.
هیچ فایل آموزشی خالی برای این عنوان‌ها ایجاد نشده است.

[بخش چهارم](../04-data/README.md) · [نقشهٔ کل دوره](../../ROADMAP.md)
