# بخش سوم — HTTP و ASP.NET Core

سه درس ۰۲۳ تا ۰۲۵ آماده‌اند؛ ۹ درس دیگر این بخش هنوز برنامه‌ریزی‌شده‌اند.

| درس | موضوع | وضعیت |
| --- | --- | --- |
| 023 | [HTTP و status code و idempotency](023-http/README.md) | نوشته‌شده |
| 024 | [host و pipeline و تنظیمات اجرا](024-web-host/README.md) | نوشته‌شده |
| 025 | [routing و binding](025-routing/README.md) | نوشته‌شده |
| 026 | controller و action و قرارداد API | برنامه‌ریزی‌شده |
| 027 | DTO و serialization و mapping دستی | برنامه‌ریزی‌شده |
| 028 | validation و ProblemDetails | برنامه‌ریزی‌شده |
| 029 | middleware و مدیریت خطا | برنامه‌ریزی‌شده |
| 030 | configuration و options و secrets | برنامه‌ریزی‌شده |
| 031 | DI scope و lifetime در درخواست | برنامه‌ریزی‌شده |
| 032 | طراحی REST و pagination | برنامه‌ریزی‌شده |
| 033 | OpenAPI و مصرف API | برنامه‌ریزی‌شده |
| 034 | تحویل SupportDesk API در حافظه | برنامه‌ریزی‌شده |

در شروع از Minimal API کوچک استفاده می‌کنیم تا HTTP و host دیده شوند.
درس controller و DTO مسیر توسعهٔ ساختار را ادامه می‌دهد؛ مدل کامل CLI هنوز به وب منتقل نشده است.
نمونه‌های فعلی حافظه‌ای و محلی‌اند و restart داده را پاک می‌کند.

## بررسی HTTP واقعی

`python scripts/web_smoke.py` نمونه‌های وب را build و serverهای موقت loopback اجرا می‌کند.
status، header، JSON، routing و configuration بررسی می‌شوند و processها پس از بررسی بسته می‌شوند.

[نقشهٔ کل دوره](../../ROADMAP.md)
