# بخش هشتم — محیط واقعی و عملیات

درس‌های ۷۹ تا ۸۲ آماده‌اند. درس‌های ۸۳ تا ۸۶ هنوز برنامه‌ریزی‌شده‌اند.
پیش‌نیاز این بخش، تست و عیب‌یابی بخش هفتم است؛ نمونه‌ها عمداً کوچک‌اند تا مسائل استقرار را جدا ببینی.

| درس | موضوع | وضعیت |
| --- | --- | --- |
| 079 | [Docker و image و محیط اجرا](079-docker/README.md) | نوشته‌شده |
| 080 | [publish و reverse proxy و TLS](080-deployment/README.md) | نوشته‌شده |
| 081 | [CI/CD و quality gate](081-ci/README.md) | نوشته‌شده |
| 082 | [OpenTelemetry و health check](082-observability/README.md) | نوشته‌شده |
| 083 | cache و invalidation و Redis | برنامه‌ریزی‌شده |
| 084 | صف و پردازش job و ارزیابی Hangfire یا Quartz | برنامه‌ریزی‌شده |
| 085 | load test و profiling و بودجهٔ latency | برنامه‌ریزی‌شده |
| 086 | backup و restore و rollback و تحویل worker | برنامه‌ریزی‌شده |

## اجرا و بررسی

از ریشهٔ مخزن با SDK 10 و Python 3:

```powershell
python scripts/check_content.py
python scripts/production_smoke.py
python scripts/production_docker_smoke.py
```

فرمان دوم تست‌های WAF و خروجی واقعی SDK OpenTelemetry، publish در Release، HTTP روی فایل‌های منتشرشده و gate منفی artifact را اجرا می‌کند.
فرمان سوم Docker CLI و engine با Linux containers و Compose می‌خواهد؛ نبود Docker شکست است و fallback ساختگی ندارد.
در CI، Production labs دو runner Windows/Linux برای publish و تست و یک job کانتینر واقعی و یک job دریافت artifact دارد. workflow قدیمی Check course بررسی‌های بخش‌های قبلی را نگه می‌دارد.

TLS این بخش با CA محلی و trust محدود به درخواست تست می‌شود؛ گواهی عمومی صادر نمی‌شود. artifact-gate فقط بسته را روی runner اجرا می‌کند و deploy خارجی ندارد.
نمونه‌ها حساب و اطلاعات کاربر ندارند و جای نسخهٔ امن و کامل TeamDesk را نمی‌گیرند. پایداری صف، cache و عملیات داده در ادامهٔ همین بخش می‌آیند.

[بخش هفتم](../07-testing/README.md) · [نقشهٔ دوره](../../ROADMAP.md)
