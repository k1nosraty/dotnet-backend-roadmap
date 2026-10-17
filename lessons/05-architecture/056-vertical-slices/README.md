# درس 056 — vertical slice و مقایسهٔ عملی

## خروجی قابل مشاهده

سه قابلیت Rename، Resolve و Close را از endpoint تا handler دنبال می‌کنی.
Application بر اساس قابلیت چیده می‌شود و هر endpoint در فایل همان قابلیت قرار می‌گیرد.
قواعد مشترک Domain و adapterها حفظ می‌شوند؛ تغییر چیدمان، بازنویسی منطق business نیست.
سناریوها با درخواست واقعی HTTP و خروجی publish بررسی می‌شوند.

پیش‌نیاز: [درس 055](../055-mapping/README.md)، Minimal API درس‌های ۲۳ تا ۲۵ و lifetime سرویس‌ها.
نمونهٔ این درس یک Web host است؛ اجرای عادی آن مثل درس‌های قبل چاپ نتیجه و خروج فوری ندارد.
با --check همچنان بررسی‌های کنسولی مستقل اجرا می‌شوند.

## vertical slice چه مسئله‌ای را حل می‌کند؟

در سازمان‌دهی صرفاً لایه‌ای، برای تغییر یک رفتار باید میان endpointها، serviceها و قراردادهای عمومی جابه‌جا شوی.
نگاه slice مسیر یک قابلیت را برجسته می‌کند: ورودی، handler و خروجی آن.
این نگاه الزام نمی‌کند Domain مشترک را در هر قابلیت کپی کنی یا همهٔ SQL را داخل endpoint بنویسی.
تعداد packageها و وجود mediator هم تعریف آن نیست.

## انتخاب این نمونه: ساختار ترکیبی

برای حفظ مرز compile، Domain، Application و Infrastructure همچنان assemblyهای جدا هستند.
درون Application، handlerها زیر Features بر اساس قابلیت گروه‌بندی شده‌اند.
در host نیز endpoint متناظر هر قابلیت زیر Features همان نام قرار دارد.
پس یک slice منطقی هنوز از دو project عبور می‌کند؛ ادعای «همهٔ فایل‌ها در یک پوشه» نداریم.
این یک انتخاب آموزشی ترکیبی است و تنها شکل ممکن vertical slice نیست.

| قابلیت | فایل host | فایل Application |
| --- | --- | --- |
| ResolveTicket | Features/ResolveTicket/Endpoint.cs | src/Application/Features/ResolveTicket/ResolveTicket.cs |
| RenameTicket | Features/RenameTicket/Endpoint.cs | src/Application/Features/RenameTicket/RenameTicket.cs |
| CloseTicket | Features/CloseTicket/Endpoint.cs | src/Application/Features/CloseTicket/CloseTicket.cs |

Shared در Application فقط شامل port، نتیجه و projection مشترک فعلی است.
Domain و Infrastructure به endpointهای feature وابسته نمی‌شوند.
namespaceهای Application در این مقایسه ثابت مانده‌اند؛ محل فایل با namespace الزاماً یکسان نیست.

## چه چیزی واقعاً تغییر کرده است؟

handlerها و Domain و mapping ذخیره با درس ۵۵ یکسان‌اند؛ فقط مسیر handlerها و قراردادهای مشترک عوض شده است.
اسکریپت این برابری سورس را برای همین refactor کنترل می‌کند.
در کنار آن یک host واقعی با سه endpoint افزوده شده است.
پس این درس هم مقایسهٔ چیدمان دارد و هم نشان می‌دهد slice تا مرز HTTP چگونه دنبال می‌شود.
برابری متنِ handler جای آزمون endpoint جدید را نمی‌گیرد.

## یک endpoint نمونه

در Features/ResolveTicket/Endpoint.cs مسیر زیر تعریف می‌شود:

```csharp
app.MapPost("/api/tickets/{id:int}/resolve",
    async (int id, ResolveRequest request, ResolveTicket handler, CancellationToken ct) =>
        OutcomeHttp.ToHttp(await handler.ExecuteAsync(
            RequestMapping.ToCommand(id, request), ct)));
```

routeId از مسیر می‌آید، body شکل محدود دارد و handler از DI گرفته می‌شود.
CancellationToken درخواست تا store منتقل می‌شود.
کد endpoint قانون تعداد noteها یا state transition را تکرار نمی‌کند.
OutcomeHttp تنها ترجمهٔ نتیجهٔ مشترک به HTTP است؛ اگر یک قابلیت قرارداد متفاوت داشت، مجبور نیست از همین ترجمه استفاده کند.

## lifetime و composition

Store حافظه‌ای singleton است تا درخواست‌های مختلف یک state مشترک ببینند.
handlerها scoped ثبت شده‌اند و state ذخیره‌شدهٔ مشترک داخل خودشان ندارند.
اتصال implementation به port در Program انجام می‌شود.
DI container جای تعیین مسئولیت یا ذخیره‌سازی اتمی را نمی‌گیرد.
در نسخهٔ دارای DbContext باید lifetime آن adapter متناسب با context باشد؛ singleton کردن این store به تمام providerها قابل تعمیم نیست.

## قرارداد HTTP همین آزمایش

| متد و مسیر | body | موفقیت | شکست‌های اصلی |
| --- | --- | --- | --- |
| PUT /api/tickets/{id}/title | title | 200 و خلاصه | 400، 404، 409 |
| POST /api/tickets/{id}/resolve | resolution | 200 و خلاصه | 400، 404، 409 |
| POST /api/tickets/{id}/close | ندارد | 200 و خلاصه | 404، 409 |

close با شناسهٔ عددی غیرمثبت به 400 هم می‌رسد.
route constraint عدد نامعتبر را match نمی‌کند و 404 می‌دهد.
ورودی JSON نامعتبر یا body لازمِ مفقود ممکن است پیش از handler به 400 برسد.
این نمونه برای نمایش سه قابلیت است؛ API کامل CRUD بخش چهارم را جایگزین نمی‌کند.
احراز هویت و مالکیت کاربر در فصل امنیت می‌آیند؛ این آزمایش را به‌عنوان سرویس عمومی محصول منتشر نکن.

## اجرا

```powershell
dotnet run --project samples/55-vertical-slices/SupportDesk.csproj -- --check
dotnet run --project samples/55-vertical-slices/SupportDesk.csproj -- --urls http://127.0.0.1:5056
```

host تا Ctrl+C روشن می‌ماند و یک تیکت Id=1 با وضعیت InProgress و بدون note دارد.
هر restart همان seed تازه را می‌سازد؛ حافظهٔ این host دوام ندارد.
در PowerShell جداگانه:

```powershell
Invoke-RestMethod -Method Put -Uri 'http://127.0.0.1:5056/api/tickets/1/title' -ContentType 'application/json' -Body '{"title":"Network restored"}'
Invoke-RestMethod -Method Post -Uri 'http://127.0.0.1:5056/api/tickets/1/resolve' -ContentType 'application/json' -Body '{"resolution":"Cable replaced"}'
```

پاسخ اول status برابر InProgress و پاسخ دوم Closed با noteCount=1 دارد.
تکرار Resolve یا Rename پس از بسته شدن، 409 می‌دهد.
Close تکراری موفق است چون state جدیدی ایجاد نمی‌کند؛ این رفتار با Resolve متفاوت است.
برای فارسی در shell، نکات encoding بخش اول را رعایت کن.

## هم‌زمانی را در مسیر کامل بررسی کن

اسکریپت هشت درخواست Resolve هم‌زمان می‌فرستد.
دقیقاً یکی باید موفق و هفت مورد Conflict شوند؛ noteCount نهایی باید یک باشد.
این مشاهده فقط وضعیت نهایی نیست: اجازه نمی‌دهد عملیات ناموفق note اضافه جا بگذارد.
تضمین رقابت در این host از CAS adapter و قواعد Domain می‌آید، نه از پوشهٔ Features.
قرار دادن همان handler در پوشهٔ دیگر، هیچ قفل تازه‌ای ایجاد نمی‌کند.

```powershell
python scripts/architecture_next_smoke.py
```

اسکریپت build، بررسی‌های Domain و port و mapping، restart فایل و publish را اجرا می‌کند.
API از خروجی publish و working directory دیگری روشن می‌شود.
ساختار response، ورودی اضافی، خطاهای 400/404/409 و رقابت HTTP بررسی می‌شوند.
SDK 10 لازم است؛ dependency خارجی تازه‌ای اضافه نشده است.

## مقایسهٔ هزینهٔ تغییر

برای نیاز «افزودن دلیل اجباری برای بستن»، دو طرح بنویس.
در چیدمان لایه‌ای کدام endpoint و DTO و use case و Domain تغییر می‌کنند؟
در چیدمان featureمحور همان فایل‌ها کجا هستند؟
اگر تغییر به یک قانون مشترک Domain مربوط باشد، featureها را کپی‌برداری نکن.
اگر دو قابلیت پاسخ متفاوتی نیاز داشته باشند، از DTO مشترک صرفاً برای کوتاه شدن کد دفاع نکن.
این مقایسه با تعداد پوشه‌ها یا مدرن بودن نام معماری تمام نمی‌شود.

## تمرین مستقل

قابلیت AddNote مستقل را به‌شکل یک slice اضافه کن.
body فقط text بگیرد و از همان Ticket.AddNote استفاده کند.
تیکت بسته، note تکراری و note ششم باید 409 بدهند و نسخهٔ قبلی حفظ شود.
یک تمرین تکمیلی: همین قابلیت را در طرح لایه‌ای درس ۵۵ پیاده کن و تعداد محل‌های تغییر را مقایسه کن.

### سرنخ‌ها

- ابتدا قرارداد HTTP و نتیجهٔ برنامه را روی کاغذ مشخص کن.
- port مورد نیاز را قبل از ساخت interface جدید بررسی کن.
- قانون مشترک را در Domain نگه دار.
- تست endpoint و تست handler دو شاهد متفاوت‌اند؛ هر دو را توضیح بده.

## معیار عبور

- مسیر یک request را با نام فایل‌ها تا commit توضیح بدهی.
- Domain به endpoint یا ASP.NET reference ندهد.
- همهٔ هشت درخواست رقابتی فقط یک note ثبت کنند.
- handlerهای یکسان در دو چیدمان بررسی شوند.
- بتوانی بگویی این ساختار چه چیزی را آسان کرده و چه تضمینی ایجاد نکرده است.

## منابع رسمی

تطبیق APIهای framework در 2026-10-17، ASP.NET Core 10:

- [Route handler و endpoint خارج از Program](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/route-handlers?view=aspnetcore-10.0)
- [مرزهای معماری و نقش composition](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- [فیلدهای ناشناختهٔ JSON](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members)

[قبلی](../055-mapping/README.md) · [فهرست بخش](../README.md)
درس 057 دربارهٔ command و query مرحلهٔ بعد است و هنوز نوشته نشده است.
