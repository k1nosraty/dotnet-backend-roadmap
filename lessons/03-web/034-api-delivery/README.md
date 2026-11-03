# درس ۰۳۴ — تحویل SupportDesk API در حافظه

[قبلی](../033-openapi/README.md) · [خانه](../../../README.md) · [بعدی](../../04-data/035-relational/README.md)

## خروجی تحویل

در پایان این بخش یک SupportDesk API در حافظه تحویل می‌دهی.
قابلیت‌های پایه، تغییر عنوان، note، گردش وضعیت، جست‌وجو و صفحه‌بندی کنار هم کار می‌کنند.
failureها قرارداد دارند و ساختار فایل‌ها باید برای تو قابل دفاع باشد.
این تحویل پایان بخش وب است؛ دیتابیس پایدار PostgreSQL از بخش بعد شروع می‌شود.

## ۱. محدودهٔ دقیق نسخه

| قابلیت | قرارداد |
| --- | --- |
| ساخت | POST /api/tickets، ۲۰۱ و Location |
| فهرست | GET /api/tickets، PageResponse |
| جزئیات | GET /api/tickets/{id}، ۲۰۰ یا ۴۰۴ |
| عنوان | PUT /api/tickets/{id}/title، ۲۰۰ یا ۴۰۹ |
| یادداشت | POST /api/tickets/{id}/notes، ۲۰۰ با Ticket به‌روزشده؛ ۴۰۹ در تعارض |
| وضعیت | PUT /api/tickets/{id}/status، ۲۰۰ یا ۴۰۹ |
| حذف | DELETE /api/tickets/{id}، ۲۰۴ یا ۴۰۴ |
| زنده بودن | GET /health، ۲۰۰ |
| سند توسعه | GET /openapi/v1.json در Development |

ثبت note زیر همین Ticket انجام می‌شود و در این نسخه منبع مستقل با Id و Location ندارد.
به همین دلیل پاسخ note، Ticket به‌روزشده با ۲۰۰ است و ادعای ایجاد منبع مستقل با ۲۰۱ نمی‌کند.
هیچ authentication، ownership یا persistence در این snapshot پیاده نشده است.
این محدوده برای تمرین محلی است و مراحل امنیت و استقرار بخش‌های جدا دارند.

## ۲. قرارداد خروجی جدید

```json
{
  "id":1,
  "title":"Network issue",
  "status":"Open",
  "notes":[]
}
```

نوع CreateTicketRequest همچنان فقط title را می‌پذیرد.
اولویت و گزارش گروهی CLI هنوز به این API منتقل نشده‌اند؛ ماتریس قابلیت‌ها در [فهرست بخش](../README.md) محدوده را روشن می‌کند.
status اولیه را server تعیین می‌کند.
فیلد notes آرایهٔ متن است، نه جدول تاریخچه با نویسنده و زمان.
اگر به audit واقعی نیاز داریم، باید مدل مستقل و نیازمندی آن را در بخش بعد طراحی کنیم.

## ۳. قواعد گردش وضعیت

| وضعیت فعلی | وضعیت درخواستی | نتیجه |
| --- | --- | --- |
| Open | InProgress | موفق |
| Open | Closed | ۴۰۹ |
| InProgress | Closed با حداقل یک note | موفق |
| InProgress | Closed بدون note | ۴۰۹ |
| هر وضعیت | همان وضعیت | موفق بدون تغییر |
| Closed | Open یا InProgress | ۴۰۹ |
| هر وضعیت | نام ناشناخته | ۴۰۰ |

نام‌ها در این قرارداد به بزرگی و کوچکی حروف حساس‌اند.
نامی مثل `Waiting` در این قرارداد اصلاً وضعیت شناخته‌شده نیست و ۴۰۰ می‌گیرد. اما `Closed` نام معتبر است؛ اگر درخواست هنوز `Open` باشد، رفتن به آن مجاز نیست و ۴۰۹ می‌گیرد. تغییر از یک وضعیت به وضعیت دیگر، `transition` نام دارد.
همان قواعد بخش دوم حفظ شده‌اند: Ticket بسته دیگر rename یا note تازه نمی‌پذیرد و این تلاش‌ها ۴۰۹ می‌دهند.
هر note پس از Trim بین ۳ و ۵۰۰ واحد UTF-16 است؛ حداکثر پنج note و بدون تکرار با Ordinal داریم.
syntax نامعتبر note پاسخ ۴۰۰ و تعارض با وضعیت، ظرفیت یا تکرار پاسخ ۴۰۹ دارد.
تکرار PUT با همان وضعیت فعلی در این API موفق است، اما چیزی را تغییر نمی‌دهد؛ این رفتار `no-op` نام دارد. در مقابل، CLI تکرار فرمان start یا close را رد می‌کرد. پس این تفاوت، انتخاب صریح قرارداد HTTP است و نباید در پیاده‌سازی یکی از آن‌ها را تصادفی جای دیگری گذاشت.

## ۴. قانون در Domain

```csharp
public Ticket? ChangeStatus(string next)
{
    if (next == Status) return this;
    if (Status == "Open" && next == "InProgress") return new(Id, Title, next, Notes);
    if (Status == "InProgress" && next == "Closed" && Notes.Count > 0)
        return new(Id, Title, next, Notes);
    return null;
}
```

لایهٔ Domain نام HTTP و status code را نمی‌شناسد.
null این متد داخلی یعنی transition مجاز نیست و store آن را به MutationKind.Conflict تبدیل می‌کند.
در API بزرگ‌تر می‌توان نتیجهٔ domain با علت دقیق‌تری ساخت؛ اینجا یک نیاز محدود داریم.
constructor خصوصی و propertyهای get-only اجازهٔ setter مستقیم روی Status نمی‌دهند.
Create همچنین Id غیرمثبت و TicketTitle برابر null را رد می‌کند؛ caller داخلی نیز باید قرارداد را رعایت کند.
مدل جدید ساخته می‌شود و مدل قبلی تا جایگزینی تغییر نمی‌کند.

## ۵. MutationResult و نتیجهٔ HTTP

لایهٔ Application نتیجهٔ Success، Missing یا Conflict دارد.
MutationResult یک کلاس با constructor خصوصی و factoryهای Success، Missing و Conflict است.
Success(null) رد می‌شود و خواندن Value در failure خطای استفادهٔ caller است؛ مثل قرارداد Outcome درس ۰۲۰.
record آزاد با Kind=Success و Value=null نمی‌سازیم و branch ناشناختهٔ enum را به موفقیت تبدیل نمی‌کنیم.
controller Missing را ۴۰۴ و Conflict را ۴۰۹ می‌کند.
نتیجهٔ Success حاوی Ticket است و به TicketResponse map می‌شود.
failure متداول برای تغییر وضعیت exception نیست و از handler ۵۰۰ عبور نمی‌کند.
جایگاه HTTP status در controller است و Domain همچنان مستقل مانده است.

## ۶. چرا mutation زیر یک lock است؟

فرض کن دو request هم‌زمان یک درخواست با صفر یادداشت را بخوانند و هرکدام یک یادداشت به نسخهٔ خود اضافه کنند. اگر جداگانه ذخیره کنند، جایگزینی آخر ممکن است فقط یک یادداشت را نگه دارد. متد `Mutate` خواندن، اجرای تابع کوتاه domain و جایگزینی را داخل یک `lock` انجام می‌دهد؛ عملیات دوم بعد از اول، وضعیت به‌روز را می‌بیند و یادداشت آن را از دست نمی‌دهد.
این تابع نباید I/O طولانی یا await داشته باشد.
قفل process جای transaction یا optimistic concurrency دیتابیس چندسرور نیست.

## ۷. collection بیرون از state مشترک

عضو Notes در Domain با ReadOnlyCollection و کپی داده ساخته می‌شود.
AddNote مدل تازه با collection تازه می‌سازد.
mapping نیز برای response کپی آرایه می‌گیرد.
این کار اجازه نمی‌دهد caller با cast کردن یک لیست مشترک، state store را بی‌قفل عوض کند.
ردیابی note با متن صرف محدودیت دارد؛ دادهٔ واقعی باید policy اندازه و تاریخچهٔ مشخص داشته باشد.
در این تمرین سقف پنج note هم زیر همان lock بررسی می‌شود؛ این تست، بار production را ارزیابی نمی‌کند.

## ۸. مسیر کامل یک عملیات

برای افزودن یادداشت، client متن را در JSON می‌فرستد. ابتدا MVC آن را به `AddNoteRequest` تبدیل و annotationها را بررسی می‌کند؛ سپس controller شناسه و طول متن پس از Trim را می‌سنجد. اگر ورودی پذیرفته شد، `TicketService` از طریق `ITicketStore` تغییر را درخواست می‌کند. متد `Mutate` با دادهٔ جاری، قانون domain را اجرا می‌کند و فقط در موفقیت نسخهٔ تازه را نگه می‌دارد. در پایان، controller نتیجه را به DTO پاسخ تبدیل می‌کند و framework آن را به JSON می‌نویسد.
در هیچ مرحله‌ای Infrastructure به Controller وابسته نشده است.

## ۹. اجرای جریان کامل در PowerShell

پس از اجرای server، در ترمینال دوم:

```powershell
$base = "http://127.0.0.1:5340"
$t = Invoke-RestMethod -Method Post -Uri "$base/api/tickets" -ContentType application/json -Body (@{title="Network issue"} | ConvertTo-Json)
$id = $t.id
Invoke-RestMethod -Method Put -Uri "$base/api/tickets/$id/status" -ContentType application/json -Body (@{status="InProgress"} | ConvertTo-Json)
Invoke-RestMethod -Method Post -Uri "$base/api/tickets/$id/notes" -ContentType application/json -Body (@{text="Fixed network"} | ConvertTo-Json)
Invoke-RestMethod -Method Put -Uri "$base/api/tickets/$id/status" -ContentType application/json -Body (@{status="Closed"} | ConvertTo-Json)
Invoke-RestMethod -Uri "$base/api/tickets/$id"
```

پاسخ نهایی باید Closed و دارای note ثبت‌شده باشد.
PUT همان Closed را تکرار کن؛ note تازه یا Ticket تازه نباید ساخته شود.
POST دوبارهٔ همان note روی Ticket قابل ویرایش، طبق قاعدهٔ duplicate پاسخ ۴۰۹ می‌دهد و append نمی‌کند.
این قاعده به معنی پیاده‌سازی idempotency key عمومی نیست؛ POST ساخت Ticket هنوز می‌تواند Ticket دوم بسازد.
در Windows PowerShell قدیمی برای پاسخ failure از try/catch استفاده کن یا status را با curl.exe -i ببین.

## ۱۰. سناریوهای پذیرش

| سناریو | انتظار |
| --- | --- |
| create با body دارای Id جعلی | Id را server می‌سازد |
| title نامعتبر | ۴۰۰ و بدون mutation |
| resource مثبت ناموجود | ۴۰۴ |
| Open به Closed | ۴۰۹ و Status همچنان Open |
| InProgress بدون note به Closed | ۴۰۹ |
| note معتبر سپس close | Closed و حفظ note |
| status ناشناخته | ۴۰۰ |
| تکرار PUT وضعیت فعلی | همان وضعیت، بدون append |
| چند create parallel | Idهای یکتا |
| note کوتاه‌تر از ۳ یا بلندتر از ۵۰۰ | ۴۰۰ و بدون mutation |
| note تکراری پس از Trim | ۴۰۹ و noteهای قبلی ثابت |
| چند note parallel | حداکثر پنج note؛ درخواست‌های بیش از ظرفیت ۴۰۹ |
| rename یا note پس از Closed | ۴۰۹ و state ثابت |
| restart | همهٔ دادهٔ حافظه‌ای پاک می‌شود |

هم status و هم state پس از failure باید بررسی شوند.
قبول کردن یک پاسخ ۴۰۹ بدون اطمینان از تغییر نکردن داده کافی نیست.
تست‌های خودکار این سناریوها را روی HTTP واقعی loopback اجرا می‌کنند.

## ۱۱. ساختار تحویل

| پوشه | نمونهٔ فایل | دلیل حضور |
| --- | --- | --- |
| Controllers | TicketsController | HTTP و انتخاب status |
| Contracts | AddNoteRequest، PageResponse | قرارداد ورودی و خروجی |
| Mapping | TicketMapping | جداسازی پاسخ از entity |
| Application | TicketService، ITicketStore، MutationResult | use case و قرارداد ذخیره |
| Domain | Ticket، TicketTitle | invariant و transition |
| Infrastructure | InMemoryTicketStore | نگهداری state و هماهنگی process |
| Configuration | TicketOptions | تنظیمات typed و validated |
| Hosting | ApiExceptionHandler، RequestIdMiddleware | رفتار مشترک pipeline |
| Program.cs | composition root | اتصال اجزای برنامه |

برای دیدن فهرست دقیق فایل‌ها README نمونهٔ نهایی را بخوان.
همهٔ این پوشه‌ها هنوز یک assembly دارند.
نام‌گذاری پوشه به تنهایی جلوی استفادهٔ Controller از dictionary داخلی را با compiler نمی‌گیرد.
بخش معماری به تفکیک پروژه‌ها و دفاع از abstraction می‌پردازد.

## ۱۲. build، check و publish

```powershell
python scripts/check_content.py
python scripts/controller_smoke.py
python scripts/domain_smoke.py
dotnet publish samples/33-supportdesk-api/SupportDesk.csproj -c Release -o exercises/published-api
```

ابزار بررسی controller_smoke هر نه snapshot را build و قرارداد هر مرحله را بررسی می‌کند.
تست‌ها options خراب، scope درخواست، pagination، سند OpenAPI و failure ۵۰۰ را هم پوشش می‌دهند.
یک consumer مستقل قواعد Domain را بدون controller بررسی می‌کند، از جمله Id غیرمثبت، null و ممنوعیت ویرایش پس از Closed.
این جداسازی نشان می‌دهد محافظت مدل فقط ناشی از guard HTTP نیست.
نمونهٔ نهایی publish و از پوشهٔ خروجی اجرا می‌شود.
خروجی framework-dependent است و runtime .NET 10 روی مقصد لازم دارد.
publish دادهٔ حافظه را پایدار نمی‌کند.

## ۱۳. اجرای خروجی منتشرشده

در PowerShell از ریشهٔ دوره:

```powershell
Push-Location exercises/published-api
try {
    dotnet SupportDesk.dll --urls http://127.0.0.1:5340
} finally {
    Pop-Location
}
```

برنامهٔ server قبلی روی همین پورت را پیش از این فرمان متوقف کن.
از پوشهٔ خروجی اجرا می‌کنیم تا content root شامل appsettings کپی‌شده باشد.
برای توقف Ctrl+C بزن.
این فرمان listener محلی ایجاد می‌کند و راهنمای TLS و reverse proxy نیست.


## نمونهٔ قابل اجرا و ابزارهای لازم

پیش‌نیاز: درس‌های قبلی بخش سوم، SDK 10 و یک ترمینال دوم برای requestها.
از ریشهٔ repository:

```powershell
dotnet run --project samples/33-supportdesk-api/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5340
```

ترمینال server را باز نگه دار؛ در ترمینال دوم:

```powershell
curl.exe -i http://127.0.0.1:5340/health
$body = @{ title = "Network issue" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:5340/api/tickets -ContentType application/json -Body $body
curl.exe -i http://127.0.0.1:5340/api/tickets/1
```

اگر process تازه باشد Id نخست ۱ است.
دادهٔ هر نمونه مستقل و در حافظهٔ همان process است.
[فایل‌ها و تغییر مرحلهٔ نمونه](../../../samples/33-supportdesk-api/README.md) را کنار این درس باز کن.
فرمان `python scripts/controller_smoke.py` همهٔ snapshotهای controller این بخش را بررسی می‌کند.
برای توقف server از Ctrl+C استفاده کن.

## تمرین مستقل تحویل

یک پروژهٔ جدا در exercises بساز و با مراجعه به قراردادها قابلیت‌ها را پیاده کن.
بعد از هر مرحله تست دستی و توضیح تغییرات را در README خودت ثبت کن.
حداقل دو transition نامعتبر و یک failure زیرساخت ساختگی را آزمایش کن.
اگر از نمونه کپی کردی، یک قابلیت کوچک مستقل اضافه کن و مسیر آن را با اسم کلاس‌ها توضیح بده.

## دفاع از طراحی

- چرا CreateTicketRequest شامل Status و Id نیست؟
- چرا note و status در controller روی dictionary تغییر نمی‌کنند؟
- چرا Domain به ProblemDetails وابسته نیست؟
- چرا count و page یک snapshot لازم دارند؟
- چه چیزی در تست parallel ثابت شده و چه چیزی دربارهٔ چندسرور ثابت نشده است؟
- کدام dependency scoped است و کدام singleton؟
- کدام نیازها هنوز برای انتشار عمومی باقی‌اند؟

## معیار قبول پایان بخش

همهٔ سناریوهای پذیرش باید موفق و مسیر هر عملیات قابل توضیح باشد.
README تحویل باید فرمان اجرا، قرارداد، ساختار، failureها و محدودیت‌ها را داشته باشد.
تست‌ها باید نمونهٔ publishشده را هم اجرا کنند.
ادعای persistence، احراز هویت یا آماده بودن برای production برای این snapshot نمی‌کنیم.
پس از این بخش آماده‌ای قرارداد ذخیره‌سازی را به مدل رابطه‌ای و PostgreSQL پیوند بدهی.

## منابع رسمی

- [dotnet publish](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish)
- [C# lock](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock)
- [ReadOnlyCollection](https://learn.microsoft.com/en-us/dotnet/api/system.collections.objectmodel.readonlycollection-1)

## پایان بخش سوم

هر ۱۲ درس ۰۲۳ تا ۰۳۴ آماده‌اند.
[درس ۰۳۵: مدل رابطه‌ای و PostgreSQL](../../04-data/035-relational/README.md) آغاز بخش بعد است؛ تمام درس‌های ۳۵ تا ۴۸ و تحویل API پایدار در فهرست بخش چهارم آماده‌اند.
تحویل و دفاع این بخش را پیش از ورود به ذخیره‌سازی پایدار انجام بده.
