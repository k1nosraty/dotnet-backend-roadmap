# درس 058 — FluentValidation پس از بررسی سازگاری

## خروجی قابل مشاهده

اعتبارسنجی ResolveTicket را در یک validator صریح تعریف می‌کنی.
HTTP برای ورودی خراب، errors فیلدمحور می‌دهد و اجرای غیر HTTP هم ورودی نامعتبر را رد می‌کند.
قواعد وضعیت و مجموعهٔ noteها در Domain حفظ می‌شوند.

پیش‌نیاز: [درس 057](../057-cqrs/README.md)، guard، nullability و validation بخش وب.
این اولین استفادهٔ FluentValidation در دوره است؛ بقیهٔ commandهای کوچک هنوز guardهای دستی دارند تا هزینهٔ انتخاب ابزار روشن بماند.

## قبل از نصب: آیا مسئله داریم؟

ممکن است یک ورودی ResolveTicket هم شناسهٔ غیرمثبت داشته باشد و هم توضیح کوتاه. می‌خواهیم به‌جای توقف روی اولین خطا، هر دو را به client بدهیم و همین قواعد در فراخوانی مستقیم handler نیز اجرا شوند. برای این کار یک `validator` می‌نویسیم؛ کلاسی که ورودی را با مجموعه‌ای از ruleهای مشخص بررسی و فهرست خطاها را تولید می‌کند.
guard دستی برای همین اندازه هنوز انتخاب معتبری است.
کتابخانه را برای یادگیری این tradeoff وارد می‌کنیم؛ الزام استفاده در همهٔ کلاس‌ها نداریم.

## نسخه و مبنا

در 2026-10-17 منابع رسمی بررسی شدند:

| مورد | انتخاب نمونه |
| --- | --- |
| package | FluentValidation |
| نسخهٔ ثابت | 12.2.0 |
| target نمونه | net10.0 |
| حداقل target خانوادهٔ ۱۲ | .NET 8 |
| license اعلام‌شدهٔ package | Apache-2.0 |
| وابستگی package اضافی | برای این استفاده لازم نیست |

در این نمونه، package اصلی در Application نصب شده و Domain وابستگی به آن ندارد.
NuGet سازگاری package با net10.0 را نشان می‌دهد؛ build و اجرای تست‌ها شاهد سازگاری همین نمونه‌اند.
سازگاری target به معنی صحت تمام ruleها نیست.

```powershell
dotnet add samples/57-fluent-validation/src/Application/SupportDesk.Application.csproj package FluentValidation --version 12.2.0
```

در سورس مرجع PackageReference از قبل ثبت شده است؛ برای run نیازی به افزودن دوباره نیست.

## validator کوچک و pure

src/Application/Features/ResolveTicket/ResolveTicketValidator.cs:

```csharp
public sealed class ResolveTicketValidator : AbstractValidator<ResolveTicketCommand>
{
    public ResolveTicketValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be positive.");
        RuleFor(x => x.Resolution)
            .Must(text => text is not null && text.Trim().Length is >= 3 and <= 500)
            .WithMessage("Resolution must contain 3-500 characters after trim.");
    }
}
```

متد `RuleFor` property هدف را انتخاب می‌کند و شرط‌های بعدی روی همان مقدار اعمال می‌شوند. با `Must` شرط سفارشیِ طول پس از Trim را می‌نویسیم. این بررسی مقدار ذخیره‌شده یا خود command را تغییر نمی‌دهد؛ فقط پاسخ معتبر/نامعتبر می‌سازد و I/O ندارد. متد `Transform` که در بعضی مثال‌های قدیمی دیده می‌شود در خانوادهٔ ۱۲ در دسترس نیست.
در فراخوانی تست، `new ResolveTicketCommand(...)` را صریح بنویس؛ `new(...)` کنار overloadهای ValidateAsync ممکن است خطای ابهام CS0121 بدهد.
خروجی normalization همچنان در command handler برای رفتار Domain مصرف می‌شود.
مدل null به‌عنوان root validator قرارداد دیگری دارد؛ endpoint یک request لازم دارد و use case هم ArgumentNullException guard دارد.

## اجرای صریح

کد endpoint به‌صورت scoped یک ResolveTicketValidator می‌گیرد:

```csharp
var validation = await validator.ValidateAsync(command, ct);
if (!validation.IsValid)
    return Results.ValidationProblem(validation.Errors.GroupBy(x => x.PropertyName)
        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()));
```

خطاهای یک فیلد در آرایهٔ همان property جمع می‌شوند.
برای Id=0 و resolution کوتاه، هر دو Id و Resolution در errors هستند.
invalid input پیش از load/save رد می‌شود.
registration به‌صورت صریح است و package اسکن خودکار لازم ندارد.

## چرا auto-validation اضافه نمی‌کنیم؟

نمونه Minimal API است و مسیر manual واضح و قابل بررسی است.
مستندات فعلی FluentValidation برای استفاده از pipeline قدیمی ASP.NET auto-validation توصیه نمی‌کنند.
package FluentValidation.AspNetCore را برای این درس نصب نکن.
اگر rule async داشته باشی، مسیر اجرای مناسب و cancellation را جدا بررسی کن.
ValidateAsync برای ruleهای sync فعلی هم درست است.

## اجرای خارج از HTTP

کلاس ResolveTicket درون Application از همان ResolveTicketValidator استفاده می‌کند.
بنابراین CLI یا test که handler را مستقیم صدا بزند، به guard HTTP وابسته نیست.
در مسیر HTTP، endpoint یک بار validator را اجرا می‌کند تا errors فیلدمحور بسازد؛ handler نیز برای حفظ قراردادِ فراخوانی مستقیم دوباره همان validator را اجرا می‌کند. تعریف قانون فقط یک جاست، اما بررسی دو بار انجام می‌شود. چون ruleهای فعلی pure و محلی‌اند، این هزینه در ADR همین نمونه پذیرفته شده است؛ اگر بررسی به دیتابیس یا سرویس بیرونی وصل شود، باید این تصمیم را بازنگری کرد.
validator وابستگی خارجی ندارد و با new ساخته می‌شود؛ اگر dependency تازه بخواهد، constructor handler و composition را بازنگری کن.

اجرای rule متصل به دیتابیس را با همین تصمیم دوبار اجرا نکن.
uniqueness check قبل از save هم race دارد و جای UNIQUE constraint یا commit هماهنگ را نمی‌گیرد.
اینجا تمام ruleها محلی‌اند.

## validator جای Domain نیست

متن معتبر برای تیکت Closed ممکن است از validator عبور کند، ولی Ticket.AddNote همچنان آن را رد می‌کند.
سقف پنج note و duplicate به کل aggregate وابسته‌اند و در validator ورودی کپی نشده‌اند.
خطای business بعد از load به Conflict می‌رسد.
خطای ورودی به Invalid یا ValidationProblem می‌رسد.
قطع storage هم به خطای validation تبدیل نمی‌شود.

## اجرا و بررسی

```powershell
dotnet run --project samples/57-fluent-validation/SupportDesk.csproj -- --check
dotnet run --project samples/57-fluent-validation/SupportDesk.csproj -- --urls http://127.0.0.1:5058
```

در ترمینال جدا:

```powershell
Invoke-RestMethod -Method Post -Uri 'http://127.0.0.1:5058/api/tickets/0/resolve' -ContentType 'application/json' -Body '{"resolution":"ab"}'
```

ابزار PowerShell برای پاسخ 400 خطا نمایش می‌دهد؛ body پاسخ را برای errors بررسی کن.
درست بودن این پاسخ با اسکریپت هم بررسی می‌شود:

```powershell
python scripts/architecture_final_smoke.py
```

مجموعهٔ ValidationChecks شامل null، فاصله، طول ۲، مرز معتبر ۳ و ۵۰۰، طول ۵۰۱ و قواعد تیکت بسته است.
ساخت و audit dependency در CI اجرا می‌شود؛ گزارش نبود vulnerability فقط مربوط به زمان همان اجراست.

## هزینه و روش حذف

برای حذف کتابخانه، امضای command و OperationResult و قواعد Domain را حفظ کن.
validator را با تابع یا نوع نتیجهٔ دستی دارای errors جایگزین کن و PackageReference و registration را بردار.
همان boundary caseها و HTTP contract باید عبور کنند.
اگر حذف ابزار نیاز به تغییر قواعد Domain داشت، مرزها را درست جدا نکرده‌ای.

## تمرین مستقل

برای RenameTicket یک validator با طول پس از Trim و شناسهٔ مثبت بساز.
errors ورودی باید Title و Id را نشان دهند.
عنوان معتبر روی تیکت Closed همچنان Conflict بدهد.
یک نسخهٔ guard دستی هم نگه دار و تفاوت خوانایی و dependency را مقایسه کن.

### سرنخ

اول سناریوهای پذیرش را مستقل از کتابخانه بنویس.
از validator برای پرسیدن وضعیت فعلی تیکت استفاده نکن؛ آن قانون به Domain تعلق دارد.

## معیار عبور

- دو خطای ورودی هم‌زمان در errors دیده شوند.
- command نامعتبر مستقیم هم چیزی ذخیره نکند.
- مرز طول پس از Trim با قرارداد قبلی برابر بماند.
- Domain به FluentValidation reference ندهد.
- هزینهٔ اجرای دوباره و روش حذف ابزار را توضیح بدهی.

## منابع رسمی

بررسی در 2026-10-17:

- [package ثابت FluentValidation 12.2.0](https://www.nuget.org/packages/FluentValidation/12.2.0)
- [تغییرهای نسخهٔ ۱۲](https://docs.fluentvalidation.net/en/latest/upgrading-to-12.html)
- [manual validation و ASP.NET](https://docs.fluentvalidation.net/en/latest/aspnet.html)

[قبلی](../057-cqrs/README.md) · [بعدی](../059-architecture-tests/README.md)
