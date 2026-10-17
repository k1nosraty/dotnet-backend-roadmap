# درس 053 — قواعد Domain و aggregate

## خروجی قابل مشاهده

مدل Ticket را به‌عنوان مرز تغییراتِ یک تیکت و noteهایش تحلیل می‌کنی.
دادهٔ ذخیره‌شده تنها پس از بررسی تمام قواعد به مدل معتبر تبدیل می‌شود.
تغییر آرایهٔ ورودی یا دستکاری مجموعهٔ خروجی نمی‌تواند وضعیت مدل ساخته‌شده را عوض کند.

پیش‌نیاز: [درس 052](../052-application-services/README.md)، کپسوله‌سازی، value object و optimistic concurrency.
این snapshot چهار پروژه دارد و هنوز کنسولی و حافظه‌ای است.
قواعد قبلی تیکت را حفظ می‌کنیم؛ قابلیت تازه برای بازگشایی تیکت اضافه نشده است.

## aggregate را از نیاز پیدا کن

در این پروژه سه قانون به کل تیکت مربوط‌اند:

- note تکراری و بیشتر از پنج note مجاز نیست.
- فقط InProgress با حداقل یک note می‌تواند Closed شود.
- تیکت بسته note یا عنوان تازه نمی‌پذیرد.

اگر هر note را مستقل ذخیره کنیم و بستن را جدا انجام دهیم، بررسی این قواعد میان چند مسیر پراکنده می‌شود.
مرز این نمونه Ticket به‌همراه مجموعهٔ noteهای آن است.
ورودی تغییر از رفتارهای Ticket می‌گذرد و خروجی تغییر به‌صورت یک snapshot ذخیره می‌شود.
این مرز یک تصمیم business است، نه نتیجهٔ خودکار رابطهٔ FK یا نام جدول.

## واژگان و مدل فعلی

| مفهوم | نمونهٔ این درس | نکته |
| --- | --- | --- |
| Entity | Ticket با Id | هویت در طول تغییر ثابت می‌ماند |
| Value object | TicketTitle | برابری بر اساس مقدار معتبر |
| Aggregate root | Ticket | نقطهٔ ورود رفتارهای مجموعه |
| مقدار در مجموعه | رشتهٔ note | فعلاً هویت مستقل ندارد |
| Invariant | سقف note و وضعیت معتبر | در مدل ساخته‌شده برقرار است |

note در این درس entity مستقلی با شناسه نیست.
اگر نیاز به ویرایش یک note مشخص، تاریخچه یا نویسنده داشته باشیم، مدل آن را دوباره بررسی می‌کنیم.
هر مجموعه‌ای الزاماً aggregate نیست و هر FK الزاماً عضو یک aggregate مشترک نیست.
کل سازمان و همهٔ تیکت‌هایش را فقط به‌دلیل داشتن رابطه در یک aggregate عظیم قرار نده.

## رفتار مدل با validation فرم فرق دارد

فرم می‌تواند قبل از ارسال، طول متن را بررسی کند.
Application می‌تواند input خراب را به Invalid تبدیل کند.
اما تغییر معتبر مدل باید حتی از CLI، job یا adapter هم از قواعد Domain عبور کند.
attribute فرم به‌تنهایی جلوی ساخت وضعیت نامعتبر در کد دیگر را نمی‌گیرد.
قواعد وضعیت در Ticket باقی می‌مانند و به handler منتقل نمی‌شوند.

## خطر بازسازی از storage

constructor عمومی با setter برای همهٔ فیلدها، دادهٔ خراب را به شیء ظاهراً معتبر تبدیل می‌کند.
در این مرحله متد صریح Restore اضافه شده است:

```csharp
public static Ticket Restore(
    int id, string title, string status, IEnumerable<string> notes)
```

این متد برای بازسازی یک وضعیت کامل است؛ تاریخچهٔ transitionها را بازپخش نمی‌کند.
پس لازم نیست برای یک تیکت Closed واقعی، هنگام load دوباره Open و InProgress را اجرا کنیم.
در عوض وضعیت نهایی باید معتبر باشد.
Restore حق تغییر خودسرانهٔ وضعیت در endpoint نیست؛ ورودی مشتری مستقیماً به آن map نمی‌شود.

## قواعد Restore

| ورودی | رفتار |
| --- | --- |
| Id غیرمثبت | ArgumentOutOfRangeException |
| عنوان نامعتبر | خطای TicketTitle.Create |
| status خارج از Open، InProgress، Closed | ArgumentException |
| note null، کوتاه، بلند یا دارای فاصلهٔ اضافی | رد داده |
| بیش از پنج note یا note تکراری | رد داده |
| Closed بدون note | رد داده |
| دادهٔ معتبر | Ticket مستقل از آرایهٔ ورودی |

عنوان با TicketTitle.Create نرمال می‌شود؛ note ذخیره‌شده باید از قبل canonical باشد.
این تفاوت یک قرارداد صریحِ همین نمونه است؛ adapter نباید بی‌صدا دادهٔ خراب را با حذف noteها «ترمیم» کند.
طول string بر اساس واحدهای UTF-16 است و الزاماً تعداد حرف دیداری فارسی یا emoji نیست.
مقایسهٔ duplicate از نوع Ordinal و پس از نرمال‌سازی هنگام افزودن note است.

## دفاع از مجموعه

Restore ابتدا notes را به آرایهٔ مستقل تبدیل می‌کند.
constructor خصوصی نیز کپی را در ReadOnlyCollection قرار می‌دهد.
پس نگهداری آرایهٔ ورودی و تغییر بعدی آن، مدل را تغییر نمی‌دهد.
نوع مجموعهٔ عمومی هم امکان Add یا setter در اختیار caller نمی‌گذارد.
فقط IReadOnlyList بودن، اگر پشت آن همان List قابل دستکاری برگردانده شود، چنین تضمینی نمی‌دهد.
برای این نمونه، copy و wrapper بخشی از تصمیم حفاظت از state هستند.

## immutability جای concurrency نیست

AddNote و ChangeStatus مدل تازه می‌سازند و مدل قبلی را تغییر نمی‌دهند.
این کمک می‌کند شکست مرحلهٔ دوم، تغییر نیمه‌کاره در store ایجاد نکند.
اما دو مدل immutable که از نسخهٔ یکسان ساخته شده‌اند همچنان می‌توانند یکدیگر را بازنویسی کنند.
نسخه و compare-and-swap ذخیره برای آن مسئله لازم است.
در PostgreSQL، transaction و concurrency token پیاده‌سازی این مرز را برعهده دارند.
یک aggregate تعریف‌شده روی کاغذ، commit اتمی دیتابیس را خودکار ایجاد نمی‌کند.

## record و هویت

Ticket برای copy و مدل immutable به شکل record نوشته شده است.
این به معنی مناسب بودن برابری تولیدشدهٔ record برای تشخیص هویت business نیست.
در این برنامه هویت با Id بررسی می‌شود؛ Notes هم یک collection است و equality آن مقایسهٔ محتوایی عمومی نیست.
برای تشخیص «همان تیکت»، به مقایسهٔ خودکار تمام اعضا تکیه نکن.
TicketTitle نمونهٔ ساده‌تری از برابری مقداری است.

## فایل‌ها و اجرا

| فایل نسبت به samples/52-domain-aggregate | نقش |
| --- | --- |
| src/Domain/Ticket.cs | root، رفتارها و Restore |
| src/Domain/TicketTitle.cs | عنوان معتبر |
| AggregateChecks.cs | ورودی خراب، دفاع مجموعه و بازسازی |
| Verification.cs | قواعد و orchestration و تعارض قبلی |

```powershell
dotnet run --project samples/52-domain-aggregate/SupportDesk.csproj
dotnet run --project samples/52-domain-aggregate/SupportDesk.csproj -- --check
python scripts/architecture_next_smoke.py
```

خروجی عادی `Success: 1 Closed notes=1` است.
بررسی، علاوه بر سناریوهای قبلی، `PASS: aggregate invariants and rehydration` می‌دهد.
SDK 10 کافی است؛ این snapshot package خارجی یا PostgreSQL نمی‌خواهد.

## تمرین مستقل

برای note یک value object معتبر با factory مشخص طراحی کن.
نیازی به شناسهٔ note یا repository جدا نیست مگر در نیازمندی‌ات دلیل داشته باشد.
نرمال‌سازی و طول در یک نقطهٔ Domain قرار بگیرند؛ سقف و duplicate همچنان مسئولیت Ticket باشند.
سپس Restore را طوری تغییر بده که ورودی ذخیره‌شدهٔ نامعتبر را رد کند.

### راهنمای مرحله‌ای

1. تفاوت قانون «یک note معتبر» با «مجموعهٔ معتبر noteها» را بنویس.
2. ابتدا ساخت یک note را آزمایش کن؛ بعد collection را به مدل اضافه کن.
3. برای دادهٔ خراب، سیاست رد کردن را پیش از mapping تعیین کن.
4. array ورودی را پس از ساخت مدل تغییر بده و نتیجه را مشاهده کن.

## معیار عبور

- تغییر آرایهٔ ورودی، مدل قبلی را عوض نکند.
- note ششم و duplicate رد شوند و مدل قبلی ثابت بماند.
- Closed بدون note از Restore ساخته نشود.
- بتوانی بگویی چرا entity، record و aggregate مترادف نیستند.
- توضیح بدهی کدام تضمین مربوط به Domain و کدام مربوط به storage است.

## منابع رسمی

تطبیق مفاهیم در 2026-10-17، مبنا SDK 10:

- [اعتبارسنجی در مدل دامنه](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations)
- [مرزهای معماری برنامه](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)

[قبلی](../052-application-services/README.md) · [بعدی](../054-ports/README.md)
