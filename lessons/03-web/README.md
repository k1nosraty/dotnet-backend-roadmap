# بخش سوم — HTTP و ASP.NET Core

هر ۱۲ درس ۰۲۳ تا ۰۳۴ کامل شده‌اند. این بخش ۱۲ نمونهٔ وب مستقل دارد.
از HTTP و Minimal API شروع می‌کنیم و به Controller، مرزهای Application/Domain/Infrastructure و تحویل API می‌رسیم.

| درس | موضوع | وضعیت |
| --- | --- | --- |
| 023 | [HTTP و status code و idempotency](023-http/README.md) | نوشته‌شده |
| 024 | [host و pipeline و تنظیمات اجرا](024-web-host/README.md) | نوشته‌شده |
| 025 | [routing و binding](025-routing/README.md) | نوشته‌شده |
| 026 | [controller و action و قرارداد API](026-controllers/README.md) | نوشته‌شده |
| 027 | [DTO و serialization و mapping دستی](027-dto/README.md) | نوشته‌شده |
| 028 | [validation و ProblemDetails](028-validation/README.md) | نوشته‌شده |
| 029 | [middleware و مدیریت خطا](029-middleware/README.md) | نوشته‌شده |
| 030 | [configuration و options و secrets](030-configuration/README.md) | نوشته‌شده |
| 031 | [DI scope و lifetime در درخواست](031-di-scopes/README.md) | نوشته‌شده |
| 032 | [طراحی REST و pagination](032-rest/README.md) | نوشته‌شده |
| 033 | [OpenAPI و مصرف API](033-openapi/README.md) | نوشته‌شده |
| 034 | [تحویل SupportDesk API در حافظه](034-api-delivery/README.md) | نوشته‌شده |

## مسیر پروژه

درس‌های ۰۲۳ تا ۰۲۵ رفتار HTTP، host و binding را با handlerهای کوچک روشن می‌کنند.
از درس ۰۲۶ routeها به controller و use case و storage به کلاس‌های جدا منتقل می‌شوند.
هر snapshot مستقل است و README آن تغییر نسبت به مرحلهٔ قبل و فهرست فایل‌ها را دارد.
در پایان create، read، title، note، status، delete و pagination در یک API کنار هم قرار می‌گیرند.

## بررسی خودکار

```powershell
python scripts/check_content.py
python scripts/web_smoke.py
python scripts/controller_smoke.py
python scripts/domain_smoke.py
```

ساخت، status و header و JSON، ورودی خراب، تنظیمات نامعتبر، scope، pagination، سند OpenAPI، concurrency و publish بررسی می‌شوند.
نتیجهٔ هر commit را در workflow «Check course» ببین.

## تحویل مستقل

[ارزیابی پایان بخش](034-api-delivery/README.md) را انجام بده و تصمیم‌های ساختار را با نام کلاس‌های پروژهٔ خودت توضیح بده.
نمونهٔ نهایی [SupportDesk API](../../samples/33-supportdesk-api/README.md) مرجع قابل اجراست.
دادهٔ تمام نمونه‌های این بخش در حافظهٔ process است؛ restart داده را پاک می‌کند.
PostgreSQL و persistence از بخش بعد شروع می‌شوند؛ بخش چهارم هنوز برنامه‌ریزی‌شده است.

[نقشهٔ کل دوره](../../ROADMAP.md)

## مقایسهٔ محدودهٔ CLI و API نهایی

| قابلیت | CLI درس ۰۲۲ | API درس ۰۳۴ |
| --- | --- | --- |
| عنوان و Id معتبر | دارد | دارد |
| rename و note روی Closed | ممنوع | ممنوع |
| note سه تا پانصد، پنج‌تایی و غیرتکراری | دارد | دارد |
| close با note از InProgress | دارد | دارد |
| تکرار درخواست وضعیت جاری | command تکراری رد می‌شود | PUT همان هدف no-op موفق است |
| اولویت Low/Normal/High | دارد | هنوز منتقل نشده |
| گزارش همهٔ وضعیت‌ها | دارد | هنوز منتقل نشده |
| فیلتر و pagination HTTP | ندارد | دارد |
| persistence پس از restart | JSON تک‌نویسنده | ندارد؛ PostgreSQL بخش چهارم |

نمونه‌های میانی scope کوچک دارند؛ صرف انتقال presentation همهٔ قابلیت‌های CLI را خودکار منتقل نمی‌کند.
قواعد مرتبطی که در تحویل API حضور دارند باید مانند بخش دوم در Domain حفظ شوند.
