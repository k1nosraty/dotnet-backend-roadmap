# درس ۰۷۸ — خطا را تکرار کن، بعد اصلاحش کن

گزارش خطا: «وقتی شمارهٔ صفحه خیلی بزرگ است، به‌جای نتیجهٔ خالی، اولین تیکت‌ها برمی‌گردند.»
قرار نیست با حدس کد را عوض کنیم. ورودی مشخص می‌سازیم، مقدار اشتباه را در debugger می‌بینیم و اصلاح را با تست نگه می‌داریم.

## ۱. اجرای بازتولید کوچک

از ریشهٔ مخزن با SDK 10:

```powershell
dotnet run --project samples/77-debugging/SupportDesk.csproj
dotnet test tests/078-debugging/SupportDesk.Tests.csproj
```

خروجی به این ترتیب است؛ trace و span در هر اجرا تغییر می‌کنند:

```text
broken=[101,102]
trace=<trace-id>; span=<span-id>; operation=tickets.page
fixed=[]
```

متد آزمایشی `OverflowRepro.ReadBroken` فقط نسخهٔ معیوبِ جداشده برای آزمایش است. مسیر درست برنامه `PageQuery.Read` است و آن متد هرگز نسخهٔ خراب را صدا نمی‌زند.
ورودی هر دو یکسان است: tickets برابر 101، 102، 103؛ page برابر int.MaxValue و size برابر 2.

## ۲. چرا این ورودی مهم است؟

فرمولِ تعداد مواردی که باید رد کنیم:

```text
offset = (page - 1) * size
```

| مقدار | نتیجه |
| --- | --- |
| بزرگ‌ترین int مثبت | 2147483647 |
| page منهای یک | 2147483646 |
| ضرب صحیح ریاضی در دو | 4294967292 |
| نتیجهٔ ضرب unchecked با int | -4 |

در این قطعه، ضرب با `int` و در حالت `unchecked` انجام می‌شود؛ عدد خارج از بازه به exception تبدیل نمی‌شود و نتیجهٔ سرریزِ این ورودی منفی چهار است. سپس `Skip(-4)` در LINQ مانند ردکردن صفر عضو عمل می‌کند، پس اولین موارد فهرست برمی‌گردند.
خطا exception نمی‌دهد؛ **نتیجه اشتباه است**. بنابراین فقط دنبال stack trace گشتن کافی نیست.

## ۳. در debugger چه کار کنم؟

در Visual Studio پوشه یا پروژهٔ samples/77-debugging/SupportDesk.csproj را باز کن و Debug configuration را انتخاب کن.
در فایل PageQuery.cs روی خط محاسبهٔ offset در ReadBroken یک breakpoint بگذار، سپس اجرای Debug را شروع کن.

| عمل | چیزی که بررسی می‌کنی |
| --- | --- |
| توقف روی breakpoint | در Locals یا Watch مقدار page و size را ببین |
| Step Over | خط محاسبه را اجرا کن؛ offset اکنون -4 است |
| Call Stack | ببین Program این متد را با چه ورودی‌ای صدا زده است |
| breakpoint در PageQuery.Read | همان ورودی را با offset از نوع long مقایسه کن |
| Continue | خروجی broken و fixed را ثبت کن |

اگر متد بارها فراخوانی می‌شود، `conditional breakpoint` با شرط `page == int.MaxValue` بگذار؛ debugger فقط برای ورودی منطبق متوقف می‌شود و لازم نیست از همهٔ فراخوانی‌های عادی عبور کنی.
برای خطایی که واقعاً exception می‌دهد از Exception Settings و توقف هنگام throw کمک بگیر؛ برای این خطای منطقی شرط و Watch مفیدتر است.
debugger را به محیط واقعیِ دارای دادهٔ حساس وصل نکن؛ همین ورودی مصنوعی برای تحلیل کافی است.

## ۴. اصلاح چرا این‌طور نوشته شده؟

قسمت مهم متد درست:

```csharp
long offset = ((long)page - 1) * size;
```

تبدیل به long **قبل از ضرب** انجام شده است. نوشتن `(long)((page - 1) * size)` دیر است؛ مقدار int قبلاً خراب شده است.
پیش از cast نهایی به int، offset با tickets.Count مقایسه می‌شود. فقط اگر offset در محدودهٔ مجموعه باشد آن را به int تبدیل می‌کنیم.
page باید مثبت و size بین ۱ و ۱۰۰ باشد؛ ورودی نامعتبر به exception مشخص تبدیل می‌شود.
این مثال paging حافظه‌ای و مرتبِ دادهٔ مصنوعی است؛ query SQL، مجوز و محدودیت هزینهٔ درخواست را جایگزین نمی‌کند.

## ۵. تستِ جلوگیری از تکرار خطا

تست `Historical_repro_returns_first_page_instead_of_empty` هم خطای قدیمی را نشان می‌دهد و هم از متد درست خروجی خالی می‌خواهد.
تست‌های عادی صفحهٔ اول، دوم و بعد از پایان را نگه می‌دارند تا اصلاحِ ورودی بزرگ رفتار معمولی را خراب نکند.
تست‌های ورودی نامعتبر نیز قرارداد را روشن می‌کنند.

برای دیدن قدرت تست، در **کپی موقت** پروژه تبدیل `(long)` را حذف کن و تست تاریخی را اجرا کن؛ باید روی Assert.Empty شکست بخورد.
سپس کپی را حذف کن. اسکریپت testing_next_smoke همین آزمایش منفی را خودکار در پوشهٔ موقت انجام می‌دهد؛ فایل‌های اصلی را خراب نمی‌کند.
تست شکست‌خورده را برای سبز کردن suite حذف یا skip نکن.

## ۶. trace با log چه فرقی دارد؟

هر log رخداد را توضیح می‌دهد؛ span بازهٔ یک عملیات را نشان می‌دهد و trace عملیات مرتبط را به هم وصل می‌کند.
متد `PageQuery.Read` با `ActivitySource` یک span به نام `tickets.page` می‌سازد. اطلاعات همراه هر span به‌شکل جفت نام/مقدار، `tag` نام دارند؛ اینجا فقط page، size و شمار نتیجه ثبت می‌شوند. مقدار عنوان یا دادهٔ خصوصی تیکت وارد tag نمی‌شود.
در Program یک ActivityListener محلی ثبت شده تا این span جمع‌آوری شود و شناسه‌هایش چاپ شوند.
بدون listener ممکن است StartActivity مقدار null بدهد؛ `activity?.` برای همین است و منطق برنامه نباید به فعال بودن telemetry وابسته باشد.
تست parent، برابری TraceId و پیوند ParentSpanId را بررسی می‌کند.

| شناسه | معنی |
| --- | --- |
| TraceId | زنجیرهٔ عملیات مرتبط |
| SpanId | یک عملیات مشخص در آن زنجیره |
| ParentSpanId | عملیات والد همین span |

این listener یک ابزار محلیِ آموزش است؛ exporter و collector و OpenTelemetry production در این درس نصب نشده‌اند.
شناسهٔ trace هم credential یا هویت تأییدشدهٔ کاربر نیست. در بخش عملیات، جمع‌آوری واقعی را بررسی می‌کنیم.

## تمرین پایان بخش هفتم

یک یادداشت بازتولید بنویس: انتظار، نتیجهٔ واقعی، ورودی دقیق، علت، اصلاح و نام تست regression.
سپس به worker درس ۷۶ یک ActivitySource اضافه کن و برای هر job یک span بساز؛ JobId را فقط در log نگه دار و payload را به tag نده.
تست نشان دهد لغو job scope را آزاد می‌کند و span هم پایان می‌یابد. سرویس خارجی یا exporter برای تمرین لازم نیست.

معیار عبور: خطای عددی را با مقدار واقعی توضیح بدهی، تست اصلاح را نگه داری و log و span و debugger را با هم اشتباه نگیری.

## منابع رسمی

- [Debugging a C# application in Visual Studio](https://learn.microsoft.com/en-us/visualstudio/get-started/csharp/tutorial-debugger?view=visualstudio)
- [checked و unchecked](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/checked-and-unchecked)
- [Distributed tracing instrumentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs)

[قبلی](../077-logging/README.md) · [فهرست بخش](../README.md) · [بخش بعد: محیط واقعی و عملیات](../../08-production/README.md)
[درس بعد: Docker](../../08-production/079-docker/README.md) آماده است.
