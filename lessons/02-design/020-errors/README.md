# درس ۰۲۰ — نتیجهٔ عملیات و exception

[قبلی](../019-separation/README.md) · [خانه](../../../README.md) · [بعدی](../021-refactoring/README.md)

## خروجی درس

برای شکست‌های عادی use case نتیجهٔ مشخص برمی‌گردانی.
برای خطای زیرساخت، لغو و bug مسیر جدا نگه می‌داری.
نوعی می‌سازی که caller بتواند success و failure را بدون رکورد جعلی تشخیص بدهد.

## پیش‌نیاز

exception، nullable، interface و value object.
در این درس result type کوچک خودمان را می‌نویسیم؛ برای فهم قرارداد، NuGet نتیجه‌محور نصب نمی‌کنیم.

## مسئله

عنوان کوتاه و شناسهٔ ناموجود در کار روزمره رخ می‌دهند.
می‌توان آن‌ها را با exception گزارش کرد، اما caller ممکن است برای رفتار عادی پر از try/catch شود.
می‌خواهیم نتیجهٔ قابل انتظار use case را در signature و مسیر branch روشن کنیم.
خرابی دیسک را هم به «عنوان نامعتبر» تبدیل نکنیم.

## ۱. ماتریس تصمیم

| حالت | ابزار این نمونه |
| --- | --- |
| عنوان کوتاه کاربر | Outcome با InvalidInput |
| Id ناموجود | Outcome با NotFound |
| تغییر وضعیت غیرمجاز | Outcome با InvalidState در CLI نهایی |
| خطای واقعی I/O | exception تا مرز presentation |
| لغو token | OperationCanceledException تا مرز مالک لغو |
| استفادهٔ غلط از result | InvalidOperationException |
| bug ناشناخته | پنهان نشود |

این جدول policy همین application است، نه ممنوعیت استفاده از exception برای تمام domain errorها.
در کتابخانه‌ها یا قراردادهای دیگر، انتخاب می‌تواند متفاوت باشد.

## ۲. نتیجهٔ معنی‌دار

```csharp
public enum ErrorCode { InvalidInput, NotFound, InvalidState, CapacityReached }
public sealed record AppError(ErrorCode Code, string Message);
```

Code برای تصمیم caller است و Message برای توضیح انسانی.
caller نباید متن دقیق Message را parse کند تا بفهمد خطا چیست.
متن خطا ممکن است برای زبان یا UX تغییر کند.
در API آینده، code و HTTP status mapping جدا تعریف می‌شوند.

## ۳. Outcome کوچک

```csharp
public sealed class Outcome<T> where T : class
{
    private readonly T? value;
    public AppError? Error { get; }
    public bool IsSuccess => Error is null;
    public T Value => value ?? throw new InvalidOperationException("Failure has no value.");
    private Outcome(T? value, AppError? error) { this.value = value; Error = error; }
    public static Outcome<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(value, null);
    }
    public static Outcome<T> Failure(ErrorCode code, string message)
    {
        if (!Enum.IsDefined(code)) throw new ArgumentOutOfRangeException(nameof(code));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("An error message is required.", nameof(message));
        return new(null, new AppError(code, message));
    }
}
```

constructor خصوصی است؛ factoryها تنها مسیر ساخت عادی‌اند.
Success بدون value و Failure با value هم‌زمان در مسیر عمومی ساخته نمی‌شوند.
where T : class برای کوچک نگه داشتن این نمونه است.
برای نتیجهٔ عددی یا بدون payload، می‌توان نوع wrapper یا طراحی دیگری داشت؛ این Outcome راه‌حل جامع همهٔ پروژه‌ها نیست.

## ۴. قرارداد استفاده

اول IsSuccess را بررسی کن.
در success از Value استفاده کن.
در failure، Error معنی دارد و Value را نخوان.
خواندن Value در failure اشتباه caller است و exception می‌دهد.
از default TicketSummary یا Ticket با شناسهٔ صفر برای نمایش failure استفاده نمی‌کنیم.

## ۵. nullable و Error

کامپایلر رابطهٔ محاسباتی IsSuccess و Error را خودکار همیشه اثبات نمی‌کند.
نمونه در شاخهٔ failure از Error! با قرارداد factoryهای همین type استفاده می‌کند.
این ! به دلیل invariant محدود است، نه حذف بی‌دلیل هشدارهای ورودی.
گزینهٔ طراحی دیگر، accessor جدا مانند RequiredError یا annotationهای جریان است.
این انتخاب را می‌توان در بازآرایی بعدی ارزیابی کرد.

## ۶. TryCreate برای value object

TicketTitle در این نمونه TryCreate دارد:

```csharp
public static bool TryCreate(string? raw, [NotNullWhen(true)] out TicketTitle? title)
```

وقتی true باشد، title معتبر و non-null است.
وقتی false باشد، title null است.
NotNullWhen به تحلیل nullable این رابطه را اطلاع می‌دهد.
attribute خودش validation انجام نمی‌دهد؛ implementation باید قرارداد را رعایت کند.
Create همچنان برای callerی که ساخت نامعتبر را exception می‌خواهد وجود دارد.
هر دو از یک تعریف قانون استفاده می‌کنند.

## ۷. نتیجهٔ ثبت

```csharp
public Outcome<TicketSummary> Register(string? rawTitle)
{
    if (!TicketTitle.TryCreate(rawTitle, out TicketTitle? title))
        return Outcome<TicketSummary>.Failure(ErrorCode.InvalidInput, "Title must contain 3-120 characters.");
    return Outcome<TicketSummary>.Success(writer.Add(title));
}
```

عنوان نامعتبر پیش از صدا زدن writer رد می‌شود.
پس persistence برای شکست عادی ورودی اجرا نمی‌شود.
writer.Add ممکن است IOException بدهد؛ service آن را به InvalidInput تبدیل نمی‌کند.
Success تنها پس از برگشت موفق writer ساخته می‌شود.

## ۸. نتیجهٔ جست‌وجو

Find با Id غیرمثبت InvalidInput می‌دهد.
reader نتیجهٔ null را برای ناموجود برمی‌گرداند و application آن را NotFound می‌کند.
درون application دیگر لازم نیست تمام callerها semantics null reader را از نو تعریف کنند.
این تبدیل یک مرز policy است؛ خطای reader همچنان failure زیرساخت می‌ماند.

## ۹. exception در domain باقی می‌ماند

مدل Ticket می‌تواند برای misuse و نقض invariant guard داشته باشد.
use case برای سناریوی عادی کاربر پیش از mutation وضعیت را بررسی و Outcome مناسب می‌دهد.
نباید catch عمومی همهٔ InvalidOperationExceptionها را به InvalidState تبدیل کنیم؛ بعضی ممکن است bug خودمان باشند.
در CLI تک‌نویسنده، بررسی و mutation در یک جریان روی مدل همان عملیات‌اند.
در دیتابیس concurrent، validation قبل از update جلوی همهٔ raceها را نمی‌گیرد.

## ۱۰. خطای storage را کجا بگیریم؟

Program یا presentation می‌تواند خطای شناخته‌شده را به پیام و کد خروج تبدیل کند.
application نباید IOException را به Success یا NotFound تبدیل کند.
در سیستم واقعی logging علت، مسیر عیب‌یابی و trace باید جدا از پیام عمومی باشد.
نام فایل خصوصی و secret را در پیام عمومی بی‌دلیل چاپ نکن.
در این نمونه پیام محدود و کد خروج داریم؛ logging حرفه‌ای فصل مستقلی دارد.

## ۱۱. لغو outcome عادی نیست

اگر مالک token لغو خواسته باشد، OperationCanceledException باید در مسیر مناسب مدیریت شود.
آن را به InvalidInput یا StorageFailed عمومی تبدیل نکن.
لغو بعد از commit الزاماً داده را rollback نمی‌کند.
در CLI نهایی pre-cancel با کد ۱۳۰ نمایش داده می‌شود؛ این یک policy برنامه است.

## ۱۲. نتیجهٔ بدون عملیات

اگر وضعیت هدف از قبل برقرار است، باید قرارداد را مشخص کنی: success بدون تغییر، یا InvalidState؟
در CLI فعلی Start دوباره و Close دوباره رد می‌شوند.
Rename با عنوان برابر روی Ticket باز یک تغییر معتبر بی‌اثر محسوب می‌شود و می‌تواند save شود.
idempotency در HTTP فصل بعد بررسی می‌شود؛ result type خودش idempotency نمی‌سازد.

## ۱۳. اجرا

[Program](../../../samples/19-ticket-outcomes/Program.cs)

```powershell
dotnet run --project samples/19-ticket-outcomes/SupportDesk.csproj
```

خروجی اصلی:

```text
Error InvalidInput: Title must contain 3-120 characters.
Success #1: Network issue
Error NotFound: Ticket not found.
Error InvalidInput: ID must be positive.
```

مسیر failure زیرساخت:

```powershell
dotnet run --project samples/19-ticket-outcomes/SupportDesk.csproj -- --failure
```

ثبت معتبر باید با خطای persistence و کد خروج ۱ تمام شود و Success چاپ نشود.

## ۱۴. محدودیت این نمونه

writer حافظه‌ای ظرفیت Id را با exception گزارش می‌کند، چون port آن نتیجهٔ capacity مدل نکرده است.
CLI نهایی با state پایدار و Outcome، CapacityReached را پیش از ثبت گزارش می‌کند.
Result نباید برای پوشاندن قرارداد ناقص dependency استفاده شود.
در هر مرز مشخص کن کدام failure جزو semantics عادی آن است.

## تمرین مستقل

برای Rename یک Outcome<TicketSummary> طراحی کن.
ورودی کوتاه، Id ناموجود و Ticket بسته را با code متفاوت گزارش کن.
storage خراب باید مسیر exception داشته باشد.
فقط در success ذخیره انجام شود و title قبلی در شکست ثابت بماند.

## تمرین تکمیلی

یک تابع mapping بنویس که ErrorCode را به exit code CLI تبدیل کند.
برای InvalidInput و InvalidState کد ۲، NotFound کد ۳ و failure storage کد ۱ استفاده کن.
معنی codeها را در README ثبت کن؛ در تست روی code تکیه کن، نه ترجمهٔ Message.

## سناریوهای پذیرش

| حالت | انتظار |
| --- | --- |
| ورودی کوتاه | InvalidInput؛ writer صدا زده نشود |
| Id ناموجود | NotFound |
| writer خراب | exception؛ Success چاپ نشود |
| Value در failure | استفادهٔ غلط رد شود |
| Success(null) | ساخت نتیجه رد شود |
| ErrorCode ناشناخته | ساخت failure رد شود |

## معیار عبور

- [ ] expected failure و exception را تفکیک می‌کنم.
- [ ] نتیجهٔ success بدون payload معتبر نمی‌سازم.
- [ ] caller متن message را parse نمی‌کند.
- [ ] لغو را خطای ورودی نمی‌کنم.
- [ ] ! محدود به قرارداد اثبات‌شده است.
- [ ] ظرفیت و محدودیت port را مستند می‌کنم.

## منابع رسمی

- [Exception best practices](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)
- [Nullable analysis attributes](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/nullable-analysis)
- [Task cancellation](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-cancellation)

## قدم بعد

مسیر parse، dispatch و render را با حفظ رفتار از هم جدا می‌کنیم.
