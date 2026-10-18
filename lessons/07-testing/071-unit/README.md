# درس 071 — unit test و xUnit

## خروجی قابل مشاهده

قواعد Title و وضعیت تیکت را بدون HTTP، PostgreSQL یا Docker با xUnit اجرا می‌کنی.
ورودی دوحرفی رد می‌شود، مرزهای دقیق پذیرفته می‌شوند و resolve نامعتبر تیکت را نمی‌بندد.
پیش‌نیاز: [درس ۷۰](../../06-security/070-security-review/README.md)، کلاس، exception و قرارداد Domain.

## چرا پروژهٔ تست جداست؟

samples/70-unit-tests برنامهٔ تحت آزمون است و tests/071-unit-tests فقط به آن reference دارد.
Domain به xUnit وابسته نیست. packageهای runner و assertion در پروژهٔ test می‌مانند و وارد publish برنامه نمی‌شوند.
این snapshot کوچکِ رفتار تیکت است؛ محصول کامل TeamDesk یا جایگزین پروژه‌های قبلی نیست.
ابتدا مرز یک رفتار را روشن می‌کنیم و در درس‌های بعد همان رفتار را تا HTTP و دیتابیس می‌بریم.

| مسیر | نقش |
| --- | --- |
| samples/70-unit-tests/Domain/TicketTitle.cs | ساخت عنوان معتبر و immutable |
| samples/70-unit-tests/Domain/TextRules.cs | اندازهٔ متن برحسب Unicode scalar، نه تعداد code unit |
| samples/70-unit-tests/Domain/Ticket.cs | گذار Open → InProgress → Closed بدون تغییر object قبلی |
| tests/071-unit-tests/DomainTests.cs | مثال‌های قابل اجرا از قرارداد |
| tests/071-unit-tests/SupportDesk.Tests.csproj | reference به SUT و ابزارهای تست |

## ابزارها و نسخهٔ مرجع

از xUnit v2 با نسخهٔ ثابت 2.9.3، adapter به نام xunit.runner.visualstudio نسخهٔ 3.1.5 و Microsoft.NET.Test.Sdk نسخهٔ 18.0.1 استفاده می‌کنیم.
شمارهٔ adapter، نسخهٔ framework xUnit نیست؛ این درس عمداً API و IAsyncLifetime مربوط به v2 را آموزش می‌دهد.
این‌ها نسخه‌های مرجع بررسی‌شده در 2026-10-18 هستند، نه ادعای جدیدترین نسخهٔ تمام بسته‌ها.
ابزار coverage یا mocking library برای شروع لازم نیست؛ مسئلهٔ واقعی را پیش از اضافه کردن package مشخص کن.

## اجرای اولین تست

```powershell
dotnet run --project samples/70-unit-tests/SupportDesk.csproj
dotnet test tests/071-unit-tests/SupportDesk.Tests.csproj
dotnet test tests/071-unit-tests/SupportDesk.Tests.csproj --filter "FullyQualifiedName~Rejects_too_short_title"
```

برنامهٔ کنسولی نمونه Closed را چاپ می‌کند؛ این خروجی جای assert نیست.
نام تست باید رفتار و نتیجه را بگوید، نه نامی مانند Test1 که بعد از شکست چیزی توضیح نمی‌دهد.
با --filter فقط بخشی از تست‌ها اجرا می‌شوند؛ آن را با اجرای کل suite اشتباه نگیر.

## Fact و Theory

Fact برای مثال مشخصی مانند رد Guid.Empty مناسب است.
Theory همراه InlineData یک قرارداد را با چند ورودی مانند null، متن خالی و whitespace بررسی می‌کند.
از Theory برای چسباندن رفتارهای نامرتبط به یک تست بزرگ استفاده نکن.
در الگوی Arrange/Act/Assert ابتدا داده را بساز، یک رفتار را اجرا کن و نتیجهٔ قابل مشاهده را بسنج.
برای exception از Assert.Throws استفاده کن؛ try/catch بدون assert ممکن است هیچ خطایی رخ ندهد و تست همچنان سبز بماند.

```csharp
[Fact]
public void Rejects_too_short_title()
{
    Assert.Throws<ArgumentException>(() => TicketTitle.Create("ab"));
}
```

expected در این تست از قرارداد «حداقل سه scalar» آمده، نه از خواندن مقدار minimum از خود پیاده‌سازی.
اگر هم test و هم SUT همان فرمول اشتباه را تکرار کنند، سبز شدنشان ارزش کمی دارد.

## قرارداد مرزی دقیق

| رفتار | دادهٔ آزمون | انتظار |
| --- | --- | --- |
| عنوان خالی | null و whitespace | ArgumentException |
| پایین مرز | ۲ scalar | رد |
| خود مرزها | ۳ و ۱۲۰ scalar | قبول |
| بالای مرز | ۱۲۱ scalar | رد |
| متن فارسی با فاصله | «  اختلال شبکه  » | trim با حفظ متن |
| emoji | دو و سه emoji | دو رد و سه قبول |
| resolve قبل از Start | Open | InvalidOperationException |
| resolve کوتاه/بلند | ۲ و ۲۰۱ scalar | رد و حفظ InProgress |

string.Length در .NET با شمارش scalar یکسان نیست؛ بعضی emojiها دو UTF-16 code unit دارند.
TextRules از EnumerateRunes استفاده می‌کند تا با char_length روی متن Unicode معتبر PostgreSQL در درس ۷۴ هم‌معنا باشد.
این شمارش، شمارش grapheme یا تعداد نمادهای بصری نیست؛ ترکیب‌ها و ZWJ می‌توانند متفاوت دیده شوند.
واحد اندازه‌گیری متن را بخشی از قرارداد بدان.

## دادهٔ immutable و assertion مفید

Start و Resolve یک Ticket جدید می‌دهند؛ تست علاوه بر وضعیت خروجی، وضعیت object قبلی را هم بررسی می‌کند.
بررسی حفظ Id و Resolution و عدم تغییر state در شکست، از تست فقط getterها مفیدتر است.
تست‌ها به ترتیب اجرا، زمان واقعی، اینترنت یا فایل مشترک وابسته نیستند.
Guid تصادفی فقط identity غیرخالی می‌سازد؛ نتیجهٔ تست به مقدار تصادفی آن وابسته نیست.

## اثبات اینکه تست می‌تواند خطا پیدا کند

```powershell
python scripts/testing_smoke.py
```

runner ابتدا suiteهای ۷۱ تا ۷۳ را اجرا و نتیجهٔ TRX را برای تعداد تست، اجرا شدن و نبود skip بررسی می‌کند.
سپس در کپی موقت، minimum طول را از ۳ به ۲ ضعیف می‌کند و فقط Rejects_too_short_title را اجرا می‌کند؛ آن تست باید واقعاً Failed شود.
خطای compile یا کشف نشدن تست به‌عنوان mutation موفق پذیرفته نمی‌شود؛ TRX باید یک شکست assertionِ اجراشده داشته باشد.
فایل اصلی repo تغییر نمی‌کند. این تنها یک mutation هدفمند است و mutation coverage کلی ادعا نمی‌شود.

## تمرین مستقل

قانون «resolve بسته‌شده دوباره قابل انجام نیست» را در تست جدا و کوتاه بنویس.
برای Title یک تغییر واقعی در قرارداد پیشنهاد کن و ابتدا مثال‌های مرزی آن را بنویس.
یکی از guardها را در کپی آزمایشی حذف کن و نشان بده کدام تست شکست می‌خورد؛ تستی که فقط خود config را می‌خواند ننویس.

### سرنخ

قبل از Assert، جملهٔ «اگر رفتار اشتباه شود، چه چیزی از بیرون فرق می‌کند؟» را جواب بده.

## معیار عبور

- unit test بدون DB و HTTP اجرا شود.
- مرز دقیق و مورد منفی و عدم تغییر state را بسنجی.
- Fact و Theory و filter را درست استفاده کنی.
- اجرای صفر تست یا skip را موفقیت کامل حساب نکنی.
- یک regression عمدی به شکست تست مورد انتظار منجر شود.

## منابع رسمی

- [شروع xUnit v2](https://xunit.net/docs/getting-started/v2/getting-started)
- [Microsoft: unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [xUnit 2.9.3](https://www.nuget.org/packages/xunit/2.9.3)
- [Test SDK 18.0.1](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.0.1)

[قبلی](../../06-security/070-security-review/README.md) · [فهرست بخش](../README.md) · [بعدی](../072-test-design/README.md)
