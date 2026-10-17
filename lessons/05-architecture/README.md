# بخش پنجم — معماری و مرز مسئولیت‌ها

تمام ۱۲ درس ۴۹ تا ۶۰ آماده‌اند؛ تحویل بخش، نسخهٔ معماری یکپارچهٔ TeamDesk است.
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
| 057 | [تفکیک command و query بدون اجبار ابزار](057-cqrs/README.md) | نوشته‌شده |
| 058 | [FluentValidation پس از بررسی سازگاری](058-validation-library/README.md) | نوشته‌شده |
| 059 | [تست وابستگی‌های معماری و ADR](059-architecture-tests/README.md) | نوشته‌شده |
| 060 | [بازآرایی TeamDesk و دفاع از معماری](060-teamdesk/README.md) | نوشته‌شده |

## مسیر عملی

نمونه‌ها مستقل‌اند؛ درس‌های ۴۹ تا ۵۵ اجرای کنسولی دارند و درس‌های ۵۶ تا ۶۰ Web host با endpointهای feature هستند. نمونه‌های ۵۴ به بعد علاوه بر adapter حافظه، adapter آزمایشی JSON هم دارند؛ برای اجرا به تنظیم PostgreSQL وابسته نیستند.
نسخهٔ پایدار بخش چهارم حفظ شده است؛ ذخیرهٔ حافظه‌ای این نمونه‌ها جای آن را نمی‌گیرد.
برای ساخت هر نمونه فرمان متن همان درس را اجرا کن؛ SDK 10 لازم است؛ از درس ۵۸، FluentValidation 12.2.0 در Application pin شده است.

```powershell
python scripts/check_content.py
python scripts/architecture_smoke.py
python scripts/architecture_next_smoke.py
python scripts/architecture_final_smoke.py
```

اسکریپت architecture_smoke رفتار چهار snapshot اول، جابه‌جایی بدون تغییر رفتار، referenceهای مستقیم، خطاهای compile عمدی و خروجی publish را بررسی می‌کند.
اسکریپت architecture_next_smoke چهار نمونهٔ بعدی، بازسازی aggregate، قرارداد adapterها، restart فایل، mapping و HTTP واقعی featureها را بررسی می‌کند.
اسکریپت architecture_final_smoke چهار درس پایانی، queryهای مستقل، errors ورودی، policy و ADR، audit package و تحویل منتشرشدهٔ TeamDesk را بررسی می‌کند.
هر سه روی Windows و Linux در workflow دوره اجرا می‌شوند؛ نتیجهٔ واقعی هر commit در Actions مشخص است.
مدل immutable و کنترل نسخهٔ این adapter فقط داخل process تضمین دارند؛ PostgreSQL و HTTP در اسکریپت‌های بخش چهارم جدا بررسی می‌شوند.

## مرز این مرحله

این بخش نسخهٔ معماری TeamDesk در یک workspace را تحویل می‌دهد. سازمان، حساب کاربر، نقش، مجوز و مالکیت موضوع فصل امنیت بعدی هستند.
همهٔ موضوع‌های معماری تا درس ۶۰ اجرا شده‌اند. adapter JSON تحویل، سند نسخهٔ ۲ و NextId مستقل دارد؛ برای آن فایل تازه بساز. PostgreSQL درس ۴۸ مرجع persistence مستقل باقی است و adapter آن هنوز با portهای جدید یکپارچه نشده است.
هیچ فایل آموزشی خالی برای این عنوان‌ها ایجاد نشده است.

[بخش چهارم](../04-data/README.md) · [نقشهٔ کل دوره](../../ROADMAP.md)

[ارزیابی مستقل و دفاع پایان بخش](060-teamdesk/README.md) را پیش از ورود به مدل تهدید و امنیت انجام بده.
