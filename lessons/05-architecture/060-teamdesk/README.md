# درس 060 — بازآرایی TeamDesk و دفاع از معماری

## خروجی قابل مشاهده

یک نسخهٔ یکپارچهٔ معماری TeamDesk تحویل می‌دهی: ثبت، جزئیات، فهرست، تغییر عنوان، note، وضعیت، resolve و حذف.
query و command از portهای مشخص عبور می‌کنند؛ Domain مستقل می‌ماند.
بررسی‌ها نشان می‌دهند restart داده را حفظ می‌کند، حذف باعث استفادهٔ دوباره از شناسه نمی‌شود و تعارض note نیمه‌کاره جا نمی‌گذارد.

پیش‌نیاز: [درس 059](../059-architecture-tests/README.md) و تحویل چهار بخش قبلی.
این خروجی «نسخهٔ معماری» TeamDesk در یک workspace است.
سازمان، حساب کاربر، نقش، مالکیت و مجوز موضوع بخش امنیت ۶۱ تا ۷۰ هستند؛ این نسخه ادعای محصول چندسازمانی امن ندارد.

## نیازمندی تحویل

| قابلیت | انتظار |
| --- | --- |
| ثبت | عنوان معتبر، Id تازه، 201 و Location |
| فهرست | فیلتر و صفحه و ترتیب Id، بدون write |
| جزئیات | response خلاصه، 404 برای منبع ناموجود |
| تغییر عنوان | حفظ قواعد عنوان و ممنوعیت ویرایش Closed |
| note | طول معتبر، عدم تکرار، سقف پنج و ممنوعیت Closed |
| وضعیت | فقط transitionهای مجاز Domain |
| resolve | note و Closed با یک commit |
| حذف | کنترل نسخه و عدم استفادهٔ دوباره از شناسه |

این تحویل ادامهٔ معماری featureمحور قبلی است، نه یک scaffold با متدهای خالی.
نام namespaceهای SupportDesk برای حفظ پیوستگی snapshotها ثابت مانده؛ نام محصول تحویل TeamDesk است.

## ساختار و دلیل فایل‌ها

| مسیر نسبت به samples/59-teamdesk | نقش |
| --- | --- |
| src/Domain | مدل معتبر و رفتارهای تیکت |
| src/Application/Features | commandها و query handlerها |
| src/Application/Shared/ITicketQueries.cs | query و read model |
| src/Application/Shared/ITicketStore.cs | تغییر نسخه‌دار aggregate |
| src/Application/Shared/ITicketLifecycle.cs | تخصیص شناسه و حذف نسخه‌دار |
| src/Infrastructure | adapter حافظه و JSON و projection |
| Features | endpointهای هر قابلیت و ترجمهٔ نتیجه |
| Presentation | request محدود و mapping command |
| Program.cs | composition، انتخاب adapter و مدیریت خطای فنی |
| docs/adr | تصمیم‌های ثبت‌شده |

ITicketLifecycle را به ITicketStore قدیمی تحمیل نکرده‌ایم تا fakeهای use caseهای قبلی مجبور به رفتارهای نامرتبط نشوند.
اما ایجاد port جدا برای هر متد قانون عمومی نیست؛ مالکیت و تعهدهای واقعی را بررسی کن.
ManageTickets فعلاً چند رفتار کوتاه و مرتبط چرخهٔ تیکت دارد؛ اگر بزرگ شد با همان مرزها به فایل‌های feature جدا منتقل کن.

## قرارداد HTTP

| روش و مسیر | ورودی | پاسخ موفق |
| --- | --- | --- |
| POST /api/tickets | title | 201 و Location |
| GET /api/tickets | q، status، page، pageSize | 200 و TicketPage |
| GET /api/tickets/{id} | شناسهٔ مسیر | 200 و خلاصه |
| PUT /api/tickets/{id}/title | title | 200 |
| POST /api/tickets/{id}/notes | text | 200 |
| PUT /api/tickets/{id}/status | status | 200 |
| POST /api/tickets/{id}/resolve | resolution | 200 |
| POST /api/tickets/{id}/close | ندارد | 200 |
| DELETE /api/tickets/{id} | شناسه | 204 |

ورودی نامعتبر 400، منبع ناموجود 404 و rule یا نسخهٔ متعارض 409 می‌دهد.
exception فنی در host به ProblemDetails عمومی 500 تبدیل می‌شود؛ path یا متن فایل خراب در پاسخ نیست.
shape خطای binding framework الزاماً با خطای validator یکی نیست؛ تست‌ها هر مرز را جدا بررسی می‌کنند.
close تکراری روی Closed موفق است، ولی resolve تکراری Conflict می‌دهد.

## storage پیش‌فرض و گزینهٔ فایل

اجرای پیش‌فرض حافظه‌ای است و restart داده را پاک می‌کند.
یک تیکت Id=1 با وضعیت InProgress و بدون note برای شروع وجود دارد.
برای تحویل آزمایشی پایدار، TEAMDESK_DATA را به فایل اختصاصی تازه در پوشهٔ موجود بده.
در نبود فایل، seed ایجاد می‌شود؛ فایل موجود به‌جای seed خوانده و اعتبارسنجی می‌شود.

```powershell
$env:TEAMDESK_DATA = "$env:TEMP/teamdesk-lesson60.json"
dotnet run --project samples/59-teamdesk/SupportDesk.csproj -- --urls http://127.0.0.1:5060
```

بعد از توقف و اجرای دوباره، همان داده باقی می‌ماند.
برای بازگشت به حالت حافظه، متغیر را حذف کن:

```powershell
Remove-Item Env:TEAMDESK_DATA
```

مسیر فایل از configuration قابل اعتماد host است، نه body یا query مشتری.
فایل نسخهٔ ۱ درس‌های قبلی را برای این نمونه استفاده نکن.
نسخهٔ سند این تحویل ۲ و دارای NextId است؛ migration خودکار از سند قبلی نداریم.
استفاده از فایل تازه صریح و عمدی است؛ فایل نامعتبر با seed جایگزین نمی‌شود.

## شناسه پس از حذف

اگر Id تازه را فقط Max(rows)+1 حساب کنیم، حذف آخرین رکورد ممکن است همان شناسه را دوباره تخصیص دهد.
NextId یک counter مستقلِ long است که همراه سند ذخیره می‌شود.
حذف آن را عقب نمی‌برد و reopen آن را از فایل بازیابی می‌کند.
counter باید از همهٔ شناسه‌های موجود بزرگ‌تر و در بازهٔ قابل استفاده باشد.
اگر از int.MaxValue عبور کرد، create به Conflict می‌رسد و چیزی درج نمی‌شود.
این policy نمونه است؛ در PostgreSQL identity/sequence و تراکنش باید جدا بررسی شوند.

## race و commit

در فایل، read و بررسی نسخه و write زیر gate مشترک همان process انجام می‌شوند.
هشت create هم‌زمان باید Idهای متفاوت بگیرند.
هشت resolve روی یک تیکت InProgress باید یک Success و هفت Conflict داشته باشند.
مدل immutable می‌ماند و قبل از موفقیت همهٔ رفتارها save انجام نمی‌شود.
delete نیز snapshot نسخه‌دار دارد؛ حذف با snapshot قدیمی Conflict است.
در حافظه یا فایل تک‌process این تضمین آزمایش شده است؛ تعمیم به چند process یا چند server مجاز نیست.

## اجرای کامل بررسی

```powershell
dotnet run --project samples/59-teamdesk/SupportDesk.csproj -- --check
python scripts/check_content.py
python scripts/architecture_final_smoke.py
```

--check بررسی‌های Domain، adapter، query، validation، assembly و lifecycle را اجرا می‌کند.
اسکریپت Python خروجی publish را از working directory دیگر روشن می‌کند.
پس از ثبت و رقابت و حذف، host را متوقف و با همان فایل restart می‌کند.
بعد صحت داده و بزرگ‌تر بودن Id بعدی را بررسی می‌کند.
همچنین فایل را در آزمایش موقت خراب می‌کند تا 500 عمومی و حفظ bytes خراب بررسی شوند.
این آزمایش هرگز فایل دستی تو را هدف نمی‌گیرد.

## publish قابل اجرا

```powershell
dotnet publish samples/59-teamdesk/SupportDesk.csproj -c Release -o artifacts/teamdesk
dotnet artifacts/teamdesk/SupportDesk.dll --urls http://127.0.0.1:5060
```

تمام پوشهٔ خروجی را منتقل کن؛ DLLهای Domain و Application و Infrastructure و dependency لازم‌اند.
برنامه framework-dependent است و runtime مناسب .NET 10 و ASP.NET Core می‌خواهد.
publish به‌تنهایی نصب‌کننده یا استقرار production نیست.

## محدودیت تحویل و اتصال به بخش‌های دیگر

JSON این درس برای تمرکز روی معماری و اجرای بی‌تنظیم دیتابیس انتخاب شده است.
هماهنگی چند process، تضمین قطع برق، migration تولیدی و حجم بزرگ گزارش را وعده نمی‌دهد.
برای persistence PostgreSQL، [نسخهٔ پایدار درس ۴۸](../../04-data/048-data-delivery/README.md) مرجع مستقل است؛ انتقال آن adapter به portهای جدید نیاز به integration و migration خودش دارد و در این نمونه انجام نشده است.
پیش از سرویس عمومی، فصل امنیت باید حساب، نقش، مجوز و مالکیت را به همین مرزها اضافه کند.

## تمرین مستقل پایان بخش

بدون کپی مستقیم handler مرجع، قابلیت «ارجاع تیکت به صف کاری» را طراحی و بساز.
صف کاری فعلاً فقط یک مقدار معتبر است؛ حساب کاربری یا مجوز جعلی اضافه نکن.
بستن تیکت باید policy ارجاع را رعایت کند؛ آن policy را خودت پیش از کد بنویس.
یک query خلاصهٔ صف و یک command تغییر صف داشته باش.
حداقل یک ADR برای محل invariant، storage و هزینهٔ abstraction بنویس.

### دفاع شفاهی یا نوشتاری

1. مسیر POST را از request تا commit با نام فایل‌ها توضیح بده.
2. نشان بده چرا GET هیچ save ندارد.
3. یک rule را تغییر بده و محل درست تغییر را توضیح بده.
4. adapter فایل را با fake محدود عوض کن و بگو چه چیزی دیگر تست نمی‌شود.
5. یکی از ADRها را با فرض تازه‌ای به چالش بکش و تصمیم جایگزین بنویس.

## معیار عبور بخش پنجم

- تمام قابلیت‌ها و خطاهای 400/404/409 با HTTP واقعی بررسی شوند.
- restart داده را حفظ کند و Id حذف‌شده دوباره استفاده نشود.
- query فایل را بازنویسی نکند.
- شکست rule یا نسخه، note نیمه‌کاره جا نگذارد.
- policy معماری و آزمایش منفی آن عبور کنند.
- ADRها زمینه، جایگزین و پیامد داشته باشند.
- تمرین پایان بخش با تصمیم‌های مستقل تحویل شود.
- تفاوت تحویل آموزشی، persistence تولیدی و امنیت محصول را توضیح بدهی.

## منابع رسمی

تطبیق در 2026-10-17:

- [الگوی CQRS](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [manual validation](https://docs.fluentvalidation.net/en/latest/aspnet.html)
- [مرزهای معماری](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)

[قبلی](../059-architecture-tests/README.md) · [فهرست بخش پنجم](../README.md)
همهٔ درس‌های ۴۹ تا ۶۰ نوشته شده‌اند. [بخش بعد از مدل تهدید و امنیت در درس ۶۱ آغاز می‌شود](../../06-security/061-threat-model/README.md).
