# بخش ششم — امنیت و مدیریت حساب

درس‌های ۶۱ تا ۶۸ آماده‌اند؛ درس‌های ۶۹ و ۷۰ هنوز برنامه‌ریزی‌شده‌اند.
پیش‌نیاز، تحویل معماری بخش پنجم و PostgreSQL بخش چهارم است.

| درس | موضوع | وضعیت |
| --- | --- | --- |
| 061 | [مدل تهدید و مرز اعتماد](061-threat-model/README.md) | نوشته‌شده |
| 062 | [Identity و مدیریت امن حساب](062-identity/README.md) | نوشته‌شده |
| 063 | [hash رمز و سیاست بازیابی](063-passwords/README.md) | نوشته‌شده |
| 064 | [cookie و bearer و انتخاب روش](064-authentication/README.md) | نوشته‌شده |
| 065 | [policy و نقش و مجوز](065-authorization/README.md) | نوشته‌شده |
| 066 | [کنترل مالکیت و جلوگیری از IDOR](066-ownership/README.md) | نوشته‌شده |
| 067 | [CORS و CSRF و injection](067-web-security/README.md) | نوشته‌شده |
| 068 | [rate limiting و کنترل سوءاستفاده](068-rate-limits/README.md) | نوشته‌شده |

## ترتیب اجرا

ابتدا مدل تهدید و default deny، سپس چرخهٔ حساب در PostgreSQL، بعد hash و reset، و در پایان cookie و bearer را انجام بده.
درس‌های ۶۲ و ۶۳ آزمایش کنسولی‌اند؛ درس ۶۴ یک host مستقل authentication با schema و key ring اختصاصی است.
این نمونه‌ها عمداً routeهای تیکتِ بدون مجوز نسخهٔ قبلی را به‌صورت عمومی کپی نمی‌کنند.
درس‌های ۶۵ تا ۶۸ policy و grant فعلی، مالکیت و سازمان، CORS و CSRF و query پارامتری و محدودیت درخواست را در چهار snapshot مستقل بررسی می‌کنند. همهٔ نمونه‌ها مدل آموزشی‌اند؛ چرخهٔ کامل محصول و عملیات حساب هنوز تکمیل نشده است.

```powershell
python scripts/check_content.py
python scripts/security_smoke.py --build-only
python scripts/security_smoke.py
python scripts/security_next_smoke.py --build-only
python scripts/security_next_smoke.py
```

SDK 10 و Python 3 لازم است. --build-only همهٔ چهار نمونه را build می‌کند و header جعلی و default deny درس ۶۱ را با HTTP بررسی می‌کند؛ SQL و login واقعی را اجرا نمی‌کند.
اجرای کامل به PostgreSQL واقعی و psql نیاز دارد.

## اتصال آزمایش

[راهنمای PostgreSQL](../../handbook/postgresql-lab.md) را بخوان و SUPPORTDESK_PG را برای database اختصاصی تنظیم کن.
برای cleanup schemaهای تصادفی HTTP، psql از پیش‌فرض‌های CI یعنی host=127.0.0.1، port=5432، database=supportdesk_lab، user=course و password=course-ci-only استفاده می‌کند.
اگر اتصال محلی تو فرق دارد، این envها را هم با همان server و database و role هماهنگ کن:

- SUPPORTDESK_TEST_PGHOST
- SUPPORTDESK_TEST_PGPORT
- SUPPORTDESK_TEST_PGDATABASE
- SUPPORTDESK_TEST_PGUSER
- SUPPORTDESK_TEST_PGPASSWORD

فقط schema تصادفیِ ساخته‌شده توسط تست حذف می‌شود؛ هیچ database یا schema موجود برنامه حذف نمی‌شود.
schemaهای auth62 و auth63 توسط Lab و schemaهای auth64 تا auth68 توسط اسکریپت و فرمان init محلی مدیریت می‌شوند.
مدیریت migration محیط واقعی با این آزمایش fresh-schema جایگزین نشده است.

## مرزهای تأیید

hash و token دست‌ساز نداریم؛ Identity و Data Protection امکانات اصلی را فراهم می‌کنند.
نمونهٔ cookie از همین مرحله CSRF می‌خواهد. protected bearer این نمونه JWT استاندارد یا سرویس کامل OAuth/OIDC نیست.
ارسال ایمیل واقعی، ثبت‌نام self-service، MFA کامل، مدیریت عمومی نقش/سازمان و انتقال مالکیت، محدودیت توزیع‌شده، تنظیم proxy و مدیریت production کلید هنوز تحویل نشده‌اند. role/ownership و rate limit محلی در درس‌های ۶۵ تا ۶۸ پوشش دارند.
تست‌ها password یا token چاپ نمی‌کنند؛ فایل کلید و schema تست موقت‌اند.

[بخش پنجم](../05-architecture/README.md) · [نقشهٔ دوره](../../ROADMAP.md)

## دامنهٔ تست‌های ادامهٔ امنیت

security_next_smoke چهار host را build و anonymous/credential جعلی را بررسی می‌کند. حالت کامل، publish مستقل، شش حساب PostgreSQL، شرط role و permission، حذف grant با token قبلی، دسترسی بین مالکان و سازمان‌ها، رد mass assignment، جست‌وجوی مخرب، CORS، CSRF و درخواست‌های هم‌زمان و 429 را اجرا می‌کند.
تست CORS از HTTP header استفاده می‌کند؛ آزمون browser و انقضای واقعی پنجره و load گسترده و چند replica اجرا نمی‌شوند.
