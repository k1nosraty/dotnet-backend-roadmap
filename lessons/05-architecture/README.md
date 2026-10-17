# بخش پنجم — معماری و مرز مسئولیت‌ها

درس‌های ۴۹ تا ۵۶ آماده‌اند؛ درس‌های ۵۷ تا ۶۰ همچنان برنامه‌ریزی‌شده‌اند.
پیش‌نیاز، بخش چهارم و شناخت Domain، Application و دسترسی به داده است.
در این گروه ابتدا مسئولیت را مشخص می‌کنیم، سپس چیدمان فایل و مرز compile و orchestration را بررسی می‌کنیم.

| درس | موضوع | وضعیت |
| --- | --- | --- |
| 049 | [تشخیص مسئولیت کلاس‌ها](049-responsibilities/README.md) | نوشته‌شده |
| 050 | [ساختار پوشه‌ای بر اساس لایه یا قابلیت](050-folders/README.md) | نوشته‌شده |
| 051 | [جداسازی assembly و جهت وابستگی](051-projects/README.md) | نوشته‌شده |
| 052 | [application service و orchestration](052-application-services/README.md) | نوشته‌شده |
| 053 | [قواعد domain و aggregate](053-domain/README.md) | نوشته‌شده |
| 054 | [port و adapter و مرز infrastructure](054-ports/README.md) | نوشته‌شده |
| 055 | [mapping و هزینهٔ abstraction](055-mapping/README.md) | نوشته‌شده |
| 056 | [vertical slice و مقایسهٔ عملی](056-vertical-slices/README.md) | نوشته‌شده |

## مسیر عملی

نمونه‌ها مستقل‌اند؛ درس‌های ۴۹ تا ۵۵ اجرای کنسولی دارند و درس ۵۶ یک Web host با endpointهای feature است. نمونه‌های ۵۴ به بعد علاوه بر adapter حافظه، adapter آزمایشی JSON هم دارند؛ برای اجرا به تنظیم PostgreSQL وابسته نیستند.
نسخهٔ پایدار بخش چهارم حفظ شده است؛ ذخیرهٔ حافظه‌ای این نمونه‌ها جای آن را نمی‌گیرد.
برای ساخت هر نمونه فرمان متن همان درس را اجرا کن؛ SDK 10 لازم است و package تازه‌ای اضافه نشده است.

```powershell
python scripts/check_content.py
python scripts/architecture_smoke.py
python scripts/architecture_next_smoke.py
```

اسکریپت architecture_smoke رفتار چهار snapshot اول، جابه‌جایی بدون تغییر رفتار، referenceهای مستقیم، خطاهای compile عمدی و خروجی publish را بررسی می‌کند.
اسکریپت architecture_next_smoke چهار نمونهٔ بعدی، بازسازی aggregate، قرارداد adapterها، restart فایل، mapping و HTTP واقعی featureها را بررسی می‌کند.
هر دو روی Windows و Linux در workflow دوره اجرا می‌شوند؛ نتیجهٔ واقعی هر commit در Actions مشخص است.
مدل immutable و کنترل نسخهٔ این adapter فقط داخل process تضمین دارند؛ PostgreSQL و HTTP در اسکریپت‌های بخش چهارم جدا بررسی می‌شوند.

## مرز این مرحله

این بخش هنوز تحویل نهایی TeamDesk یا مدیریت کاربر و مجوز نیست.
aggregate، port/adapter، mapping و چیدمان feature تا درس ۵۶ بررسی شده‌اند. CQRS، FluentValidation، تست معماری و تحویل TeamDesk موضوع درس‌های ۵۷ تا ۶۰ هستند.
هیچ فایل آموزشی خالی برای این عنوان‌ها ایجاد نشده است.

[بخش چهارم](../04-data/README.md) · [نقشهٔ کل دوره](../../ROADMAP.md)
