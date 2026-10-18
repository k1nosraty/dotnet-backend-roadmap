# درس ۰۷۷ — لاگی که بتوانی در آن جست‌وجو کنی

جملهٔ «ارسال تمام شد» کافی نیست. باید بتوانیم بپرسیم «job با این شناسه چه نتیجه‌ای داشت؟»؛ بدون اینکه ایمیل و متن پیام را در log ذخیره کنیم.
در این درس یک event کوچک را از ILogger تا JSON دنبال می‌کنیم.

## ۱. اجرای نمونه

از ریشهٔ مخزن با SDK 10 اجرا کن:

```powershell
dotnet run --project samples/76-structured-logging/SupportDesk.csproj
dotnet test tests/077-logging/SupportDesk.Tests.csproj
```

یک خط JSON می‌بینی. زمان آن در هر اجرا متفاوت است. بخش‌های مهم خروجی این‌ها هستند:

| فیلد | مقدار/معنی |
| --- | --- |
| `Level` | Information |
| `MessageTemplate` | Delivery finished with {Outcome} |
| `Properties.Outcome` | delivered |
| `Properties.JobId` | 11111111-1111-1111-1111-111111111111 |
| `Properties.EventId.Id` | 7701 |
| `Properties.SourceContext` | SupportDesk.JobLog |

خروجی را مثل جدول داده ببین: Outcome یک فیلد مستقل است، نه بخشی که بعداً از جمله استخراج کنیم.

## ۲. ILogger و Serilog هر کدام چه می‌کنند؟

| جزء | نقش در همین نمونه |
| --- | --- |
| `JobLog` در `JobLog.cs` | event کسب‌وکاری با اطلاعات مجاز می‌سازد |
| `ILogger<JobLog>` | قرارداد استانداردی که JobLog به آن وابسته است |
| `AddSerilog` در `Program.cs` | اتصال logging میزبان به provider Serilog |
| `JsonLineSink` | نوشتن هر event با JsonFormatter روی یک خط |

کلاس JobLog نیازی ندارد logger استاتیک Serilog را صدا بزند.
Program تصمیم می‌گیرد خروجی کجا و با چه فرمتی نوشته شود. در این lab، مقصد stdout است؛ فایل یا سرور log خارجی تنظیم نشده است.
ClearProviders جلوی خروجی تکراری providerهای پیش‌فرض را می‌گیرد.

## ۳. template را ثابت نگه دار

این قسمت از متد Finished را در فایل پیدا کن:

```csharp
logger.LogInformation(new EventId(7701, "DeliveryFinished"),
    "Delivery finished with {Outcome}", "delivered");
```

قالب ثابت است؛ مقدار Outcome جدا ارسال شده است. اگر دو job مختلف داشته باشیم، قالب مشترک آن‌ها را می‌توان یک گروه دانست.
نوشتن `$"Delivery finished with {result}"` مقدار را زود به رشته تبدیل می‌کند؛ در نتیجه این فراخوانی فیلد مستقل Outcome تولید نمی‌کند.
برای search و alert، اسم فیلدها و معنای event را بی‌دلیل تغییر نده.
EventId شمارهٔ نوع رخداد است، JobId شناسهٔ یک کار مشخص؛ این دو را جابه‌جا نکن.

## ۴. scope یعنی اطلاعات مشترکِ یک محدوده

Finished با BeginScope یک JobId ثبت می‌کند. Enrich.FromLogContext در تنظیم Serilog، اطلاعات scope را به event می‌رساند.
پس از خروج از using، JobId نباید به رخداد بعدی نشت کند.
تست `Scope_does_not_leak_into_later_events` دقیقاً رخداد دوم خارج از scope را بررسی می‌کند.

scope برای ارتباط چند event مفید است؛ شناسهٔ job به‌خودی‌خود هویت کاربر یا مجوز دسترسی نیست.
شناسه‌ها را به برچسب metric با تعداد نامحدود تبدیل نکن؛ کاربرد log با metric فرق دارد.

## ۵. چه چیزی را ننویسیم؟

Notification برای انجام کار، Email و Body دارد، اما JobLog فقط Id و یک outcome ثابت را می‌فرستد.

| داده | تصمیم نمونه |
| --- | --- |
| JobId داخلی | ثبت در scope |
| نتیجهٔ delivered/rejected | ثبت به‌صورت فیلد ثابت |
| Email و Body | ثبت نمی‌شوند |
| token، header، connection string | اصلاً وارد logger نمی‌شوند |
| متن خام exception یا request | ثبت نمی‌شود |

JSON formatter رشته‌ها را encode می‌کند، ولی **secret را حذف نمی‌کند**. اگر کل notification را با destructuring مثل `{@Notification}` بفرستی، اطلاعات حساس همچنان ذخیره می‌شوند.
تست، ایمیل و body شامل password و newline می‌دهد و عدم وجود آن‌ها و تک‌خط بودن خروجی را بررسی می‌کند.
این شاهدِ همین مسیر JobLog است؛ تضمین پاک‌سازی خودکار همهٔ logهای برنامه یا providerهای دیگر نیست.

## ۶. level و عمر logger

ارسال موفق Information است؛ رد قابل انتظار Warning است. Error برای خرابی‌ای است که واقعاً نیازمند رسیدگی است، نه هر ورودی نامعتبر.
فیلتر حداقل Information در این نمونه eventهای Debug را کنار می‌گذارد.
در پایان، dispose شدن Host، provider خودش را dispose می‌کند. یک logger استاتیک دوم با عمر نامشخص نساخته‌ایم.
در سرویس واقعی، retention، دسترسی، ظرفیت و خرابی مقصد log هم باید مشخص شوند؛ این lab سامانهٔ audit پایدار نیست.

## تمرین

1. در Program نتیجه را Rejected کن. Level و EventId و Outcome را با جدول بالا مقایسه کن.
2. دو notification با شناسه‌های متفاوت ثبت کن و نشان بده template ثابت است.
3. تستی اضافه کن که event دوم JobId خودش را داشته باشد؛ body هیچ‌کدام نباید دیده شود.
4. بنویس اگر نوشتن stdout شکست بخورد، چه تصمیم عملیاتی برای سرویس لازم داری؛ «هرگز log گم نمی‌شود» ادعا نکن.

معیار عبور: بتوانی از روی JSON، event و job را تشخیص بدهی و بدون چاپ payload علت رد را در سطح قرارداد توضیح بدهی.

## منابع رسمی

- [Logging in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/overview)
- [Serilog.Extensions.Hosting](https://github.com/serilog/serilog-extensions-hosting)
- [Serilog structured data](https://github.com/serilog/serilog/wiki/Structured-Data)
- [نسخهٔ مرجع Serilog.Extensions.Hosting 10.0.0](https://www.nuget.org/packages/Serilog.Extensions.Hosting/10.0.0)

[قبلی](../076-background-work/README.md) · [بعدی](../078-debugging/README.md) · [فهرست بخش](../README.md)
