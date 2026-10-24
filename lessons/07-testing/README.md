# بخش هفتم — تست و عیب‌یابی

هر هشت درس ۷۱ تا ۷۸ آماده‌اند؛ بخش تست و عیب‌یابی کامل شده است.
پیش‌نیاز، معماری بخش پنجم و بازبینی امنیت بخش ششم است.

| درس | موضوع | وضعیت |
| --- | --- | --- |
| 071 | [unit test و xUnit](071-unit/README.md) | نوشته‌شده |
| 072 | [boundary و negative case و test double](072-test-design/README.md) | نوشته‌شده |
| 073 | [integration و WebApplicationFactory](073-integration/README.md) | نوشته‌شده |
| 074 | [تست دیتابیس و Testcontainers](074-database-tests/README.md) | نوشته‌شده |
| 075 | [HttpClientFactory و timeout و resilience](075-http-resilience/README.md) | نوشته‌شده |
| 076 | [BackgroundService و scope و shutdown](076-background-work/README.md) | نوشته‌شده |
| 077 | [ILogger و structured logging و Serilog](077-logging/README.md) | نوشته‌شده |
| 078 | [debugger و trace و بازتولید خطا](078-debugging/README.md) | نوشته‌شده |

## اجرای مرحله‌ای

```powershell
python scripts/check_content.py
python scripts/testing_smoke.py
python scripts/testing_next_smoke.py
python scripts/testing_smoke.py --with-docker
```

SDK 10 و Python 3 لازم است؛ NuGet packages بار اول restore می‌شوند.
حالت عادی، unit و test double و WAF و mutation هدفمند و closed gate روی Kestrel منتشرشده را اجرا می‌کند و پروژهٔ PostgreSQL را فقط build می‌کند.
--with-docker همان بررسی‌ها و suite کانتینر را اجرا می‌کند. --docker-only فقط suite کانتینر را اجرا می‌کند و برای job مستقل CI است.
Docker CLI و engine با Linux containers باید در دسترس باشند؛ نبود Docker در حالت کامل شکست است، نه skip موفق.
تست DB از PostgreSQL موقت خودش و پورت پویا و password تصادفی استفاده می‌کند؛ SUPPORTDESK_PG دیتابیس کاربر لازم نیست.
همهٔ تست‌های درخواستی باید در TRX executed باشند و failed و skipped نداشته باشند. یک شکست عمدی در کپی موقت برای اثبات تشخیص regression جداگانه انتظار می‌رود.

## مرز نمونه‌ها

این snapshotها برای آموزش سطح‌های تست کوچک شده‌اند؛ lifecycle کامل و account service محصول را جایگزین نمی‌کنند.
Application نتیجهٔ مستقل از HTTP دارد و endpoint نگاشت HTTP را انجام می‌دهد. هویت ساختگی فقط در test assembly ثبت می‌شود؛ host عادی به آن اعتماد ندارد.
TestServer از pipeline واقعی app استفاده می‌کند ولی TLS/proxy و مرورگر واقعی نیست. PostgreSQL واقعی در درس ۷۴ جای حافظه را می‌گیرد؛ صدور credential همچنان موضوع تست‌های بخش امنیت است.
bootstrap schema خالی، تست migration یا backup/restore نیست. ریسک‌های شبکه، deployment و load را جدا بررسی کن.

[فهرست تست‌ها](../../tests/catalog.json) · [بخش ششم](../06-security/README.md) · [نقشهٔ دوره](../../ROADMAP.md)

درس‌های ۷۵ تا ۷۸ چهار lab کوچک مستقل دارند. testing_next_smoke تست‌های HTTP pipeline، cancellation و scope worker، JSON log و regression عددی را اجرا می‌کند؛ Docker لازم ندارد. transport HTTP ساختگی است؛ تست شبکهٔ واقعی ادعا نمی‌شود.
