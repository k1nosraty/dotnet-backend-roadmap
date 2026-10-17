# درس 049 — تشخیص مسئولیت کلاس‌ها

## خروجی قابل مشاهده

قابلیت «بستن تیکت» را با چهار مسئولیت مشخص اجرا می‌کنی:
ترجمهٔ ورودی و خروجی، هماهنگی عملیات، قانون دامنه و ذخیره‌سازی.
برای هر فایل می‌توانی یک دلیل مستقلِ تغییر نام ببری.
نام کلاس یا تعداد خطوط، جای این استدلال را نمی‌گیرد.

پیش‌نیاز: درس‌های 013، 017، 026، 046 و [تحویل پایدار 048](../../04-data/048-data-delivery/README.md).
نمونهٔ این درس عمداً کنسولی و حافظه‌ای است تا بررسی مسئولیت به وب و PostgreSQL وابسته نباشد.
API پایدار درس ۴۸ سر جای خود باقی می‌ماند؛ این نمونه جایگزین persistence آن نیست.

## مسئله: کلاس همه‌کاره

این کد را فقط برای تشخیص مشکل بخوان؛ implementation مرجع نیست:

```csharp
// ضدنمونه؛ به پروژه اضافه نکن.
// Close(request, context):
//   خواندن شناسه از HTTP
//   خواندن سطر از EF
//   بررسی InProgress بودن و وجود یادداشت
//   تغییر Status در سطر
//   SaveChanges
//   ساخت JSON و انتخاب کد HTTP
```

تغییر قرارداد HTTP، قانون بستن و provider ذخیره، همگی این متد را تغییر می‌دهند.
اگر همان قابلیت در CLI یا worker لازم شود، یا قوانین تکرار می‌شوند یا HTTP به آن محیط نشت می‌کند.
راه‌حل صرفاً انتقال متد به فایلی با نام Service نیست.

## دلیل تغییر را پیدا کن

| بخش | سؤال اصلی | دلیل تغییر نمونه |
| --- | --- | --- |
| Presentation / Program | خروجی چگونه نمایش داده شود؟ | پیام کنسول یا قرارداد HTTP |
| Application / CloseTicket | عملیات با چه ترتیبی اجرا شود؟ | هماهنگی load، rule و save |
| Domain / Ticket | چه تغییری مجاز است؟ | قانون وضعیت یا یادداشت |
| Infrastructure / InMemoryTicketStore | تغییر چگونه ذخیره شود؟ | الگوریتم هماهنگی یا provider |

SRP یعنی یک مسئولیت منسجم نسبت به دلایل تغییر، نه «هر کلاس فقط یک متد».
Ticket چند رفتار دارد، اما همه به حفظ قواعد همان تیکت مربوط‌اند.
Store هم load و save دارد؛ هر دو بخشی از قرارداد نگهداری تیکت هستند.

## واژگان

**Cohesion** یعنی اجزای مرتبط کنار هم باشند.
**Coupling** یعنی یک جزء برای کار کردن چه چیزهایی از اجزای دیگر می‌داند.
هدف حذف تمام coupling نیست؛ وابستگی به قرارداد کوچک و لازم، قابل دفاع است.
**Composition root** جایی است که implementationها انتخاب و به هم متصل می‌شوند.
در این مثال Program.cs این نقش را دارد.

## مسیر فایل‌ها

| فایل نسبت به پوشهٔ نمونه | نقش |
| --- | --- |
| Program.cs | ساخت seed، اتصال اجزا، چاپ نتیجه |
| Application/CloseTicket.cs | use case بستن تیکت |
| Application/ITicketStore.cs | قرارداد کوچک ذخیره و snapshot |
| Application/OperationResult.cs | نتیجهٔ مستقل از HTTP و DTO خروجی |
| Domain/Ticket.cs | قواعد وضعیت و یادداشت، مدل immutable |
| Domain/TicketTitle.cs | عنوان معتبر و نرمال‌شده |
| Infrastructure/InMemoryTicketStore.cs | dictionary و compare-and-swap |
| Infrastructure/StoreFactory.cs | ساخت adapter بدون افشای کلاس داخلی |
| Verification.cs | سناریوهای بررسی مستقل از نمایش |

## اجرای مرجع

از ریشهٔ مخزن:

```powershell
dotnet run --project samples/48-responsibilities/SupportDesk.csproj
dotnet run --project samples/48-responsibilities/SupportDesk.csproj -- --check
```

اجرای عادی `Success: 1 Closed notes=1` می‌دهد.
اجرای بررسی در انتها `PASS: architecture behavior` چاپ می‌کند.
هر اجرای جدید از seed تازه شروع می‌شود؛ این adapter دوام بعد از restart ندارد.
SDK 10 کافی است؛ package خارجی و دیتابیس لازم نیست.

## هماهنگی با Application، تصمیم با Domain

بخش اصلی CloseTicket چنین است:

```csharp
var original = await store.LoadAsync(id, ct);
if (original is null) return OperationResult.Missing();

var changed = original.Value.ChangeStatus("Closed");
if (changed is null) return OperationResult.Conflict();
var saved = await store.SaveAsync(original, changed, ct);
```

Application نمی‌نویسد «اگر وضعیت InProgress و Notes.Count مثبت بود».
این قانون از بخش‌های قبلی داخل Ticket مانده است؛ تغییر UI آن را تغییر نمی‌دهد.
Open مستقیم بسته نمی‌شود؛ InProgress بدون یادداشت هم بسته نمی‌شود.
برای تیکت بسته، ChangeStatus با همان وضعیت خود مدل را می‌دهد؛ نتیجهٔ بستن تکراری موفق است.
در adapter این درس فراخوانی موفق تکراری نسخهٔ داخلی را هم افزایش می‌دهد؛ بدون تغییر business state.

`OperationResult` کدهای Success، Invalid، Missing و Conflict دارد.
HTTP status در این نوع وجود ندارد؛ presentation آینده می‌تواند آن‌ها را به 200، 400، 404 و 409 ترجمه کند.
متد Success مقدار null نمی‌پذیرد تا نتیجهٔ موفق بدون پاسخ ساخته نشود.
TicketResponse یک تصویر کوچک برای caller است و storage graph را بیرون نمی‌دهد.

## چرا قرارداد store اینجاست؟

Application برای load و ذخیرهٔ یک تغییر به قرارداد نیاز دارد؛ بنابراین مالک این نیاز است.
Infrastructure آن قرارداد را پیاده می‌کند.
interface برای تک‌تک کلاس‌ها اضافه نکرده‌ایم؛ CloseTicket و TicketTitle interface ندارند.
یک GenericRepository با ده‌ها متدِ بدون مصرف، مسئولیت را روشن‌تر نمی‌کند.

## حافظه و هم‌زمانی

Snapshot شامل Ticket immutable و Version داخلی است.
Save فقط اگر نسخه هنوز برابر باشد، تحت lock یک نسخهٔ جدید ذخیره می‌کند.
دو caller که یک snapshot را خوانده‌اند نمی‌توانند هر دو آن را بازنویسی کنند.
این lock فقط همین process را هماهنگ می‌کند؛ جای concurrency token دیتابیس در درس ۴۵ را نمی‌گیرد.
این نسخه از کاربر HTTP دریافت نمی‌شود و راه‌حل تشخیص صفحهٔ قدیمی مشتری هم نیست.
مدل و مجموعهٔ Notes setter قابل تغییر ندارند؛ caller نمی‌تواند ذخیره را دور بزند.

## بررسی و عیب‌یابی

| سناریو | انتظار |
| --- | --- |
| شناسهٔ صفر | Invalid بدون خواندن و ذخیره |
| شناسهٔ ناموجود | Missing |
| Open یا InProgress بدون note | Conflict و snapshot قبلی |
| InProgress با note | Closed و افزایش نسخه |
| دو تغییر با snapshot یکسان | دقیقاً یک Saved و یک Conflict |
| لغو پیش از save | OperationCanceledException و حفظ داده |
| تغییر شناسه در save | ArgumentException و حفظ داده |

Cancellation بعد از نقطهٔ commit تضمین rollback ندارد.
سرویس خطای غیرمنتظرهٔ adapter را به Missing تبدیل نمی‌کند؛ مدیریت خطای فنی متعلق به مرز host است.
تست حافظه‌ای، SQL و rollback دیتابیس را ثابت نمی‌کند؛ برای آن‌ها بررسی‌های بخش چهارم لازم‌اند.

## تمرین مستقل

قابلیت «تغییر عنوان» را به این نمونه اضافه کن.
ورودی عنوان و شناسه را بگیر؛ TicketTitle و Ticket.Rename را دوباره استفاده کن.
تیکت بسته نباید تغییر کند و تعارض نسخه نباید بازنویسی شود.
پیش از نوشتن کد برای هر فایل تازه، نقش و dependency مجازش را بنویس.

### راهنمای مرحله‌ای

1. ابتدا مسیر تغییر را روی کاغذ بنویس: ورودی، load، ساخت عنوان معتبر، Rename، save، خروجی.
2. تصمیم بگیر خطای عنوان در قرارداد برنامه چگونه نمایش داده شود.
3. به‌جای اضافه کردن if وضعیت در Program، رفتار مدل موجود را فراخوانی کن.
4. نسخهٔ قبلی را هنگام save همراه تغییر بفرست؛ load دوباره پیش از save، تعارض را پنهان می‌کند.

## معیار عبور

- بتوانی بدون رجوع به نام پوشه، مسئولیت هر فایل را توضیح بدهی.
- تغییر پیام نمایش، Ticket و store را تغییر ندهد.
- تغییر قانون دامنه در CLI و use case یکسان دیده شود.
- تمرین با عنوان خالی، تیکت بسته و snapshot قدیمی بررسی شود.
- بگویی چرا این نمونه اثبات persistence یا امنیت نیست.

## منابع رسمی

مفهوم و ابزارها در 2026-10-17 با مستندات رسمی تطبیق داده شده‌اند:

- [معماری‌های رایج برنامه‌های .NET](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- [دسترسی internal در C#](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/internal)

[بعدی: لایه یا قابلیت](../050-folders/README.md) · [فهرست بخش](../README.md)
