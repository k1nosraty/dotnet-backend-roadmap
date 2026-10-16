# مسیر پروژه‌محور C# و ASP.NET Core

آموزش فارسی برای ساخت بک‌اند قابل نگهداری، از اولین برنامهٔ C# تا یک سرویس قابل استقرار با PostgreSQL.
کد، نام کلاس‌ها و نام پوشه‌ها انگلیسی‌اند؛ توضیحات و تمرین‌ها فارسی هستند.

## از کجا شروع کنم؟

1. [نقشهٔ ۹۰ درس](ROADMAP.md) را ببین.
2. [روش یادگیری و معیار عبور](handbook/learning-method.md) را بخوان.
3. [درس ۰۰۱: راه‌اندازی محیط](lessons/01-foundations/001-environment/README.md) را انجام بده.
4. [درس ۰۰۲: اولین قابلیت](lessons/01-foundations/002-first-feature/README.md) را انجام بده.
5. [درس ۰۰۳: دریافت و اعتبارسنجی ورودی](lessons/01-foundations/003-input-validation/README.md) را انجام بده.
6. [درس ۰۰۴: حلقه و منو](lessons/01-foundations/004-loops/README.md) را انجام بده.
7. [درس ۰۰۵: متدها و مسئولیت‌ها](lessons/01-foundations/005-methods/README.md) را انجام بده.
8. [درس ۰۰۶: مجموعه‌ها و جست‌وجو](lessons/01-foundations/006-collections/README.md) را انجام بده.
9. [درس‌های ۰۰۷ تا ۰۱۲ و ارزیابی پایان بخش اول](lessons/01-foundations/README.md) را ادامه بده.
10. [درس ۰۱۳: کپسوله‌سازی و قواعد مدل](lessons/02-design/013-encapsulation/README.md) را انجام بده.
11. [درس‌های ۰۱۴ تا ۰۲۲](lessons/02-design/README.md) را ادامه بده.
12. [SupportDesk CLI یکپارچه](samples/21-supportdesk-cli/README.md) را اجرا و ارزیابی پایان بخش دوم را انجام بده.
13. [بخش سوم: HTTP و ASP.NET Core](lessons/03-web/README.md) را از درس ۰۲۳ شروع کن.
14. [تحویل SupportDesk API](lessons/03-web/034-api-delivery/README.md) و ارزیابی پایان بخش سوم را انجام بده.
15. [بخش چهارم: PostgreSQL و SQL](lessons/04-data/README.md) را با [آماده‌سازی دیتابیس](handbook/postgresql-lab.md) شروع کن.
16. پیشرفت خودت را در [PROGRESS.md](PROGRESS.md) ثبت کن.

## وضعیت واقعی محتوا

این نسخه شامل چهار بخش اول کامل دوره در [GitHub](https://github.com/k1nosraty/dotnet-backend-roadmap) است: چهل‌وهشت درس نوشته شده، چهل‌وهفت نمونهٔ مستقل C# آماده شده و برنامهٔ هر ۹۰ درس مشخص است.
درس‌های آینده در نقشه وضعیت «برنامه‌ریزی‌شده» دارند و فایل آموزشی خالی برای آن‌ها ساخته نشده است.
SDK در محیط محلی تهیهٔ این نسخه نصب نیست؛ بررسی ساختاری محلی انجام شده است.
ساخت و بررسی رفتار نمونه‌ها در workflow «Check course» روی Windows و Linux در GitHub اجرا می‌شود؛ نتیجهٔ هر commit را در تب Actions ببین.
فرمان ساخت، سناریوهای بررسی و اسکریپت smoke در بسته وجود دارند.

```powershell
python scripts/check_content.py
python scripts/smoke.py
python scripts/web_smoke.py
python scripts/controller_smoke.py
python scripts/domain_smoke.py
# برای نمونه‌های PostgreSQL پس از آماده‌سازی server و SUPPORTDESK_PG:
python scripts/postgres_smoke.py
python scripts/ef_smoke.py
```

این هفت اسکریپت به Python 3 نیاز دارند؛ اسکریپت‌های smoke به SDK 10 هم نیاز دارند. postgres_smoke به PostgreSQL واقعی و متغیر SUPPORTDESK_PG نیز نیاز دارد. ef_smoke برای بخش API علاوه بر این‌ها psql می‌خواهد؛ متغیرهای اتصال تست در README نمونهٔ تحویل توضیح داده شده‌اند. اجرای دستی درس‌های دیتابیس نیز PostgreSQL و اتصال تنظیم‌شده می‌خواهد.

## مبنای فنی

- .NET 10 و ASP.NET Core؛ SDK نسخهٔ 10 لازم است.
- PostgreSQL در بخش دیتابیس.
- ابتدا امکانات استاندارد .NET؛ کتابخانهٔ خارجی هنگام مواجهه با مسئله معرفی می‌شود.
- راه‌اندازی درس اول برای Windows و PowerShell نوشته شده است.
- مبنای نسخهٔ .NET در ۲۰۲۶/۱۰/۱۴ با منابع رسمی بررسی شده است.
- [دانلود رسمی .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

## پروژه‌هایی که می‌سازیم

| پروژه | خروجی | درس‌های محوری |
| --- | --- | --- |
| SupportDesk CLI | ثبت و مدیریت درخواست‌های پشتیبانی در کنسول | ۰۰۱ تا ۰۲۲ |
| SupportDesk API | انتقال قابلیت‌ها به HTTP و ذخیره در PostgreSQL | ۰۲۳ تا ۰۴۸ |
| TeamDesk | سازمان، کاربر، نقش و کنترل دسترسی به درخواست‌ها | ۰۴۹ تا ۰۷۰ |
| Integration Worker | ارتباط HTTP، پردازش پس‌زمینه و بررسی رفتار سرویس | ۰۷۱ تا ۰۸۶ |
| پروژهٔ نهایی ServiceDesk | تحویل مستقل با تست، امنیت، استقرار و مستندات | ۰۸۷ تا ۰۹۰ |

پروژه‌ها مراحل قابل بررسی دارند؛ هر مرحله یک مسئلهٔ واقعی و معیار پذیرش دارد.
پروژهٔ نهایی از روی نمونه کپی نمی‌شود: باید نیازمندی‌های تازه را مستقل پیاده کنی.

## مهارت خروجی

بتوانی نیازمندی را به قرارداد API، قواعد کسب‌وکار و مدل داده تبدیل کنی.
بتوانی مرز controller، service و data access را توضیح بدهی.
بتوانی یک خطا را با تست و لاگ پیدا کنی، اصلاح کنی و از تکرارش جلوگیری کنی.
بتوانی دربارهٔ هزینهٔ هر dependency و abstraction تصمیم بگیری.

تمام کردن متن‌ها معادل تجربهٔ حرفه‌ای نیست؛ معیار این دوره توانایی ساخت و دفاع از تصمیم‌های فنی است.

[گزارش ممیزی محتوایی و فنی درس‌های ۱ تا ۳۴](AUDIT.md) شامل یافته‌ها، اصلاحات و شاهد CI است.

## فایل‌های راهنما

- [Git برای ثبت تمرین](handbook/git-practice.md)
- [مرجع syntaxهای C#](handbook/csharp-syntax.md)
- [چیدمان کلاس‌ها و پوشه‌ها](handbook/project-structure.md)
- [سیاست انتخاب کتابخانه](handbook/libraries.md)
- [شرح پروژهٔ اول](projects/01-support-desk/README.md)
- [قالب درس](templates/lesson.md)
- [راهنمای انتشار](PUBLISH.md)

## قرارداد توسعهٔ دوره

هر درس شامل پیش‌نیاز، توضیح مسئله، کد قابل استفاده، تحلیل، تمرین، سناریوی خطا و معیار عبور است.
تعداد خطوط هدف نیست؛ درس کوتاه کامل بهتر از درس طولانی با تکرار است.
قبل از اضافه کردن نسخه‌های تازهٔ کتابخانه‌ها، مستندات رسمی و سازگاری نسخه‌ها بررسی می‌شود.
