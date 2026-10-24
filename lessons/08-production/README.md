# بخش هشتم — محیط واقعی و عملیات

هر هشت درس ۷۹ تا ۸۶ آماده‌اند؛ بخش محیط واقعی و عملیات کامل شده است.
پیش‌نیاز این بخش، تست و عیب‌یابی بخش هفتم است؛ نمونه‌ها عمداً کوچک‌اند تا مسائل استقرار را جدا ببینی.

| درس | موضوع | وضعیت |
| --- | --- | --- |
| 079 | [Docker و image و محیط اجرا](079-docker/README.md) | نوشته‌شده |
| 080 | [publish و reverse proxy و TLS](080-deployment/README.md) | نوشته‌شده |
| 081 | [CI/CD و quality gate](081-ci/README.md) | نوشته‌شده |
| 082 | [OpenTelemetry و health check](082-observability/README.md) | نوشته‌شده |
| 083 | [cache و invalidation و Redis](083-caching/README.md) | نوشته‌شده |
| 084 | [صف و پردازش job و ارزیابی Hangfire یا Quartz](084-jobs/README.md) | نوشته‌شده |
| 085 | [load test و profiling و بودجهٔ latency](085-performance/README.md) | نوشته‌شده |
| 086 | [backup و restore و rollback و تحویل worker](086-operations/README.md) | نوشته‌شده |

## اجرا و بررسی

از ریشهٔ مخزن با SDK 10 و Python 3:

```powershell
python scripts/check_content.py
python scripts/production_smoke.py
python scripts/production_docker_smoke.py
python scripts/operations_smoke.py
python scripts/operations_smoke.py --with-docker
```

فرمان دوم تست‌های WAF و خروجی واقعی SDK OpenTelemetry، publish در Release، HTTP روی فایل‌های منتشرشده و gate منفی artifact را اجرا می‌کند.
فرمان سوم Docker CLI و engine با Linux containers و Compose می‌خواهد؛ نبود Docker شکست است و fallback ساختگی ندارد.
در CI، Production labs دو runner Windows/Linux برای publish و تست و یک job کانتینر واقعی و یک job دریافت artifact دارد. workflow قدیمی Check course بررسی‌های بخش‌های قبلی را نگه می‌دارد.

TLS این بخش با CA محلی و trust محدود به درخواست تست می‌شود؛ گواهی عمومی صادر نمی‌شود. artifact-gate فقط بسته را روی runner اجرا می‌کند و deploy خارجی ندارد.
نمونه‌ها حساب و اطلاعات کاربر ندارند و جای نسخهٔ امن و کامل TeamDesk را نمی‌گیرند. نمونه‌های پایدار صف، cache و تمرین restore/rollback در پایان همین بخش آمده‌اند؛ تضمین‌های محلی و محدودیت‌های محصول در هر درس مشخص شده‌اند.

[بخش هفتم](../07-testing/README.md) · [نقشهٔ دوره](../../ROADMAP.md)

ادامهٔ این بخش: operations_smoke در حالت عادی ۱۵ تست cache/قرارداد و load محدود را اجرا و suite دیتابیس را build می‌کند. حالت Docker هشت تست Redis/PostgreSQL و rehearsal بازیابی و rollback worker را اجرا می‌کند. job مستقل Real Redis, job leases and recovery در CI اجباری است و artifact-gate به موفقیت آن هم وابسته است. [محیط دستی یک‌بارمصرف](../../handbook/operations-lab.md) برای اجراهای فردی آماده است.
