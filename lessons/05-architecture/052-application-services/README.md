# درس 052 — Application Service و orchestration

## خروجی قابل مشاهده

عملیات «ثبت یادداشت حل مسئله و بستن تیکت» را هماهنگ می‌کنی.
اگر بستن مجاز نباشد، یادداشت جدید هم ذخیره نمی‌شود.
کل تغییر مدل با یک save و یک کنترل نسخه ذخیره می‌شود.
هم‌زمانی و failure از قرارداد نتیجهٔ برنامه جداگانه قابل مشاهده‌اند.

پیش‌نیاز: [درس 051](../051-projects/README.md)، نتیجهٔ عملیات، immutable model و optimistic concurrency بخش چهارم.
این snapshot چهارپروژه‌ای است و use case CloseTicket قبلی را هم حفظ می‌کند.
نام قابلیت جدید ResolveTicket است و معنایش با CloseTicket یکسان نیست.

## قرارداد مسئله

کارشناس روی یک تیکت InProgress کار کرده و اکنون توضیح حل مسئله را ثبت می‌کند.
سیستم باید note معتبر را اضافه کند و سپس تیکت را ببندد.
قوانین قبلی مدل حفظ می‌شوند:

- عنوان معتبر در مدل حفظ می‌شود.
- note پس از Trim باید ۳ تا ۵۰۰ واحد طول string باشد؛ این معیار تعداد حروف دیداری نیست.
- حداکثر پنج note و عدم تکرار دقیق پس از Trim برقرار است.
- Open مستقیم به Closed نمی‌رود.
- تیکت بسته note تازه نمی‌پذیرد.

Resolve یک قابلیت مشخص است؛ قرار نیست هر تغییر کوچک Ticket به یک «سرویس عمومی همه‌کاره» سپرده شود.

## واژگان و مرزها

**Orchestration** یعنی ترتیب دادن مراحل یک use case و هماهنگ کردن وابستگی‌های آن.
**Invariant** قاعده‌ای است که وضعیت معتبر مدل باید حفظ کند.
Application تعیین می‌کند چه زمانی load و commit انجام شود.
Domain تعیین می‌کند AddNote یا ChangeStatus مجاز هست یا نیست.
Infrastructure تضمین قرارداد ذخیره را در محیط خودش پیاده می‌کند.
Presentation نتیجه را به پیام کنسول یا HTTP تبدیل می‌کند.

## فایل‌های تازه

| مسیر در نمونه | مسئولیت |
| --- | --- |
| src/Application/ResolveTicket.cs | command و orchestration ثبت resolution و بستن |
| src/Domain/Ticket.cs | همان قواعد قبلی؛ بدون آگاهی از use case جدید |
| src/Application/ITicketStore.cs | load و save اتمی یک TicketSnapshot |
| src/Infrastructure/InMemoryTicketStore.cs | compare-and-swap کل Ticket با lock |
| Program.cs | نمایش اجرای ResolveTicket |
| Verification.cs | شکست مرحلهٔ دوم، conflict، IO و لغو |

command فقط Id و Resolution دارد؛ connection string، HttpContext و DbContext ندارد.
OperationResult هم response مستقل از framework می‌دهد.
ترکیب command و service در یک فایل کوتاه انتخاب محلی است، نه الزام «همیشه همهٔ typeها در یک فایل».

## پیاده‌سازی مرحله‌ای

مرحلهٔ اول: شکل ورودی را پیش از I/O بررسی کن.
شناسهٔ مثبت و متن با طول مجاز لازم‌اند.
چون متن از بیرون می‌آید، string? صریح است؛ null را با ! پنهان نمی‌کنیم.
Domain هم guard خودش را دارد؛ validation لبه جای قاعدهٔ داخلی مدل نیست.

مرحلهٔ دوم: snapshot را یک بار load کن.
اگر موجود نیست Missing برگردان.
همین snapshot و نسخه باید تا commit همراه تغییر باقی بماند.

مرحلهٔ سوم: رفتارهای Domain را روی مقدارهای جدید اجرا کن:

```csharp
var withNote = original.Value.AddNote(text);
if (withNote is null) return OperationResult.Conflict();
var closed = withNote.ChangeStatus("Closed");
if (closed is null) return OperationResult.Conflict();
```

متغیر withNote هنوز ذخیره نشده است.
اگر تیکت Open باشد، note در یک مدل موقت ساخته می‌شود ولی بستن رد می‌شود.
مدل اصلی immutable است، پس نه حافظهٔ store و نه snapshot قبلی تغییر نکرده‌اند.
این ویژگی برای جلوگیری از تغییر نیمه‌کاره مهم است.
اگر مدل mutable یا tracked بود، باید rollback و پاک کردن state را جدا مدیریت می‌کردیم.

مرحلهٔ چهارم: فقط نتیجهٔ نهایی را ذخیره کن:

```csharp
var saved = await store.SaveAsync(original, closed, ct);
```

adapter یک بار زیر lock نسخه را مقایسه و کل مدل را جایگزین می‌کند.
در adapter واقعی PostgreSQL این قرارداد باید با concurrency token و commit اتمی اجرا شود؛
نمونهٔ پایدار درس ۴۸ مبنای آن آزمایش‌هاست.
فقط نوشتن نام SaveAsync، اتمی بودن یک implementation دیگر را تضمین نمی‌کند.

## خطای رایج: دو save

این ترتیب مشکل دارد:

```csharp
// ضدنمونه؛ اجرا نکن:
// AddNote -> Save -> ChangeStatus -> Save
```

اگر تغییر وضعیت رد شود یا save دوم شکست بخورد، note قبلاً ذخیره شده است.
در این use case، ثبت یادداشت و بستن باید یک تغییر اتمی باشند.
یک transaction واقعی هم فقط وقتی مفید است که تمام مراحل لازم داخل مرز آن باشند.
Application Service نباید بدون شناخت تضمین adapter ادعای rollback بدهد.

## نتیجهٔ عملیات

| وضعیت | OperationCode | آیا چیزی ذخیره می‌شود؟ |
| --- | --- | --- |
| Id نامعتبر، متن null یا طول نامعتبر | Invalid | خیر |
| تیکت هنگام load نیست | Missing | خیر |
| note تکراری، سقف note، وضعیت نامجاز | Conflict | خیر |
| نسخه هنگام save تغییر کرده | Conflict | تغییر این caller ذخیره نمی‌شود |
| تیکت میان load و save حذف شده | Missing | خیر |
| اجرای کامل | Success | note و Closed با یک افزایش نسخه |
| خطای فنی غیرمنتظره | exception برای host | مطابق قرارداد failure adapter |

Catch عمومی که همهٔ exceptionها را Missing کند، قطعی ذخیره را با نبودن تیکت اشتباه می‌گیرد.
CancellationToken از ورودی تا store منتقل می‌شود.
لغو قبل از commit در این نمونه داده را حفظ می‌کند؛ لغو پس از commit الزاماً تغییر را برنمی‌گرداند.

## تکرار درخواست و تعارض

CloseTicket تکراری روی Closed موفق است؛ business state دوباره تغییر نمی‌کند.
ResolveTicket تکراری موفق نیست: این عملیات note تازه می‌افزاید و تیکت بسته note نمی‌پذیرد.
بنابراین درخواست دوباره Conflict می‌دهد.
برای retry پس از گم شدن پاسخ شبکه، قرارداد idempotency مستقل لازم است؛ در این درس جعل آن را با «اگر بسته بود موفق بده» جایگزین نمی‌کنیم.
token داخلی سرور هم ETag مشتری یا idempotency key نیست.

## اجرای مرجع

```powershell
dotnet run --project samples/51-application-services/SupportDesk.csproj
dotnet run --project samples/51-application-services/SupportDesk.csproj -- --check
python scripts/architecture_smoke.py
```

خروجی عادی `Success: 1 Closed notes=1` و بررسی `PASS: architecture behavior` است.
store حافظه‌ای است و اجرای تازه seed تازه دارد.
برای اجرا SDK 10 کافی است؛ تست SQL به این درس نسبت داده نمی‌شود.
اسکریپت خروجی publish چندپروژه‌ای را نیز از یک working directory دیگر اجرا می‌کند.

## بررسی failure با test double

Verification یک RejectingStore کوچک دارد.
Load را به store حافظه‌ای واقعی واگذار می‌کند ولی Save را با Conflict، Missing یا IOException رد می‌کند.
انتظار این است که Application نتیجه را درست تفسیر کند و note نیمه‌کاره ذخیره نشود.
این double فقط orchestration را بررسی می‌کند؛ خرابی واقعی شبکه یا transaction PostgreSQL را بازتولید نمی‌کند.
تست double عمدی exception را قبل از هر write می‌دهد؛ تضمین آن را به تمام providerها تعمیم نده.

## تمرین مستقل

قابلیت «بازگشایی همراه دلیل» را تحلیل و سپس پیاده کن.
ابتدا مشخص کن قانون فعلی مدل Closed را terminal می‌داند؛ پس تغییر این نیازمند تصمیم business و تغییر صریح Domain است.
دلیل بازگشایی باید معتبر باشد، سقف note روشن بماند و وضعیت و دلیل با یک save ثبت شوند.
برای «تیکت از ابتدا بسته نبود»، «سقف note پر بود» و «نسخه هم‌زمان عوض شد» رفتار مورد انتظار را پیش از کد بنویس.

### راهنمای مرحله‌ای

1. اول سیاست بازگشایی و نگهداری دلیل را با جدول transition طراحی کن.
2. به جای دور زدن Domain با setter عمومی، رفتار مجاز به مدل اضافه کن.
3. command، use case و DTO را از جزئیات store مستقل نگه دار.
4. قبل از افزودن worker یا ایمیل، مرز commit را مشخص کن.
5. اگر قرار شد پس از commit پیام بیرونی بفرستی، موفقیت DB و موفقیت ارسال یک transaction مشترک نیستند؛ آن مسئله در مباحث integration و worker بررسی خواهد شد.

## معیار عبور

- شکست ChangeStatus پس از ساخت note، نسخه و noteهای اصلی را ثابت بگذارد.
- اجرای موفق دقیقاً یک افزایش نسخه داشته باشد.
- note تکراری، سقف پنج note و تیکت بسته به Conflict برسند.
- خطای فنی با Missing مخلوط نشود.
- use case به Infrastructure reference ندهد.
- بتوانی تفاوت invariant، orchestration و تضمین commit را با فایل‌های همین نمونه توضیح بدهی.
- بتوانی محدودیت fake، حافظه و cancellation را بیان کنی.

## منابع رسمی

تطبیق در 2026-10-17؛ ابزار همان SDK 10 و بدون dependency تازه است:

- [مرز Application Core و Infrastructure](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- [مرجع تنظیمات projectهای SDK](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props)

[قبلی](../051-projects/README.md) · [فهرست بخش](../README.md)
[درس بعد: قواعد Domain و aggregate](../053-domain/README.md).
