# درس ۰۳۲ — طراحی REST و pagination

[قبلی](../031-di-scopes/README.md) · [خانه](../../../README.md) · [بعدی](../033-openapi/README.md)

## مسئله و خروجی

برگرداندن تمام داده‌ها برای هر GET با رشد حجم مناسب نیست.
قرارداد فهرست را با filter، ترتیب قطعی، page و pageSize مشخص می‌کنیم.
در این درس اندازهٔ صفحه از Options می‌آید و envelope پاسخ تغییر می‌کند.
این یک API حافظه‌ای برای فهم قرارداد است؛ کارایی query دیتابیس را ادعا نمی‌کند.

## ۱. نام‌گذاری منبع و عملیات

/api/tickets مجموعه و /api/tickets/{id} یک عضو است.
زیرمنبع /title نمایش قابل جایگزینی عنوان را دارد.
در URL نام همهٔ متدهای C# مانند GetAllTickets را وارد نمی‌کنیم.
فقط جمع بستن اسم‌ها یک API را به تمام معنا RESTful نمی‌کند؛ قرارداد method، representation و failure هم مهم است.
این بخش یک قرارداد HTTP منظم می‌سازد و همهٔ قیود REST یا hypermedia را پیاده نکرده است.

## ۲. قرارداد query

| کلید | default | محدوده / رفتار |
| --- | --- | --- |
| q | غایب | فیلتر عنوان، حداکثر raw ۱۲۰ |
| page | ۱ | عدد صحیح مثبت |
| pageSize | DefaultPageSize | ۱ تا MaxPageSize |

filter بدون حساسیت به بزرگی حروف و با OrdinalIgnoreCase است.
whitespace به معنی بدون فیلتر است.
pageSize نامعتبر را در این قرارداد رد می‌کنیم، نه اینکه بی‌خبر به max کاهش دهیم.
سیاست clamp هم ممکن است، ولی باید مستند و قابل تست باشد.

## ۳. پاسخ envelope

```csharp
public sealed record PageResponse<T>(
    IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
```

```json
{
  "items":[{"id":1,"title":"Network issue"}],
  "page":1,"pageSize":2,"totalCount":1
}
```

TotalCount تعداد کل نتیجه‌های filter قبل از page است.
Items فقط دادهٔ همان صفحه است.
این تغییر نسبت به array خالیِ درس قبل تغییر قرارداد است و client قدیمی را می‌تواند بشکند.
در snapshot آموزشی نسخه‌ها جدا هستند؛ در محصول به برنامهٔ migration یا versioning نیاز داریم.

## ۴. ترتیب عملیات

ابتدا snapshot داده با ترتیب Id گرفته می‌شود.
بعد filter روی آن اعمال می‌شود.
TotalCount از همان snapshot فیلترشده حساب می‌شود.
در پایان Skip و Take صفحه را انتخاب می‌کنند.
اگر filter را بعد از Take انجام دهی، صفحه‌ها و totalCount معنی اشتباه خواهند داشت.

## ۵. ترتیب قطعی

store در All بر اساس Id صعودی مرتب می‌کند.
pagination بدون ترتیب قطعی می‌تواند بین اجراها جابه‌جایی تصادفی بدهد.
مرتب‌سازی بر اساس فیلد غیر یکتا به tie-breaker مثل Id نیاز دارد.
این قرارداد فعلاً sort دلخواه نمی‌پذیرد.
در دیتابیس باید همین ترتیب به query واقعی منتقل شود.

## ۶. overflow و صفحهٔ خیلی بزرگ

```csharp
long skip = ((long)page - 1) * size;
TicketResponse[] items = skip >= filtered.Length
    ? []
    : filtered.Skip((int)skip).Take(size).Select(t => t.ToResponse()).ToArray();
```

تبدیل به long پیش از ضرب جلوی overflow محاسبهٔ offset برای intهای بزرگ را می‌گیرد.
اگر offset از تعداد داده بیشتر باشد، پاسخ ۲۰۰ با Items خالی است.
فقط وقتی offset از طول آرایه کمتر است آن را به int برمی‌گردانیم.
page منفی یا صفر پیش‌تر ۴۰۰ شده است.
اینجا cap مصنوعی برای شمارهٔ صفحه نداریم؛ هزینهٔ snapshot مستقل از بزرگی شمارهٔ صفحه است.

## ۷. مرز validation

page=abc خطای تبدیل است و ApiController آن را ۴۰۰ می‌کند.
page=0 تبدیل موفق دارد ولی policy نمونه آن را رد می‌کند.
pageSize بیشتر از MaxPageSize خطای ورودی است.
q خیلی بلند هم ۴۰۰ است و داده را تغییر نمی‌دهد.
صفحهٔ معتبرِ بدون نتیجه ۴۰۴ نیست.

## ۸. snapshot یک درخواست

Count و Items از یک آرایهٔ snapshot ساخته می‌شوند.
درخواست create هم‌زمان نمی‌تواند وسط enumeration dictionary را خراب کند.
اما درخواست صفحهٔ اول و صفحهٔ دوم snapshot مشترک تاریخی ندارند.
با حذف یا اضافه شدن داده بین دو درخواست، offset pagination می‌تواند رکورد را تکرار یا جا بیندازد.
این محدودیت را با «مرتب‌سازی قطعی همهٔ مسائل را حل کرد» اشتباه نگیر.

## ۹. offset در برابر cursor

| روش | مزیت | محدودیت |
| --- | --- | --- |
| page / offset | ساده و مناسب پرش صفحه | هزینهٔ offset بزرگ در دیتابیس و جابه‌جایی با mutation |
| cursor / keyset | ادامه دادن بر اساس کلید ترتیب | قرارداد cursor و ترتیب پیچیده‌تر، پرش دلخواه دشوارتر |

در این درس فقط offset پیاده شده است.
cursor باید اطلاعات ترتیب و filter را با قرارداد روشن نگه دارد.
pagination permission filtering را حذف نمی‌کند؛ بعداً فقط دادهٔ مجاز باید وارد count و page شود.

## ۱۰. چرا فعلاً query در controller است؟

برای دیدن قرارداد HTTP، query کوچک روی snapshot در action آمده است.
این به معنی توصیهٔ بارگیری کل جدول PostgreSQL در controller نیست.
در بخش data، filter و projection و limit باید به query قابل اجرا در دیتابیس منتقل شوند.
اگر شکل خروجی یا قواعد query پیچیده شد، application query service مرز مناسب‌تری می‌شود.
abstraction فعلی نباید مانع query کارآمد بعدی شود.

## ۱۱. سناریوی عددی

چهار Ticket با عنوان‌های Network alpha، Printer beta، Network gamma و Network delta بساز.
با q=network و pageSize=2 باید totalCount=3 باشد.
page اول Idهای ۱ و ۳ و page دوم Id=4 را می‌گیرد.
page خیلی بزرگ Items خالی و همان totalCount می‌دهد.
با q=absent totalCount=0 است.

## ۱۲. مصرف پاسخ

client نباید length صفحه را همیشه تعداد کل داده بداند.
pageSize واقعی پاسخ را بخوان؛ default محیط ممکن است متفاوت باشد.
برای نمایش «صفحهٔ بعد» می‌توان از page، pageSize و totalCount استفاده کرد، با توجه به تغییر هم‌زمان داده.
برای product با دادهٔ پرنوسان، قرارداد cursor را جدا ارزیابی کن.


## نمونهٔ قابل اجرا و ابزارهای لازم

پیش‌نیاز: درس‌های قبلی بخش سوم، SDK 10 و یک ترمینال دوم برای requestها.
از ریشهٔ repository:

```powershell
dotnet run --project samples/31-rest-pagination/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5320
```

ترمینال server را باز نگه دار؛ در ترمینال دوم:

```powershell
curl.exe -i http://127.0.0.1:5320/health
$body = @{ title = "Network issue" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:5320/api/tickets -ContentType application/json -Body $body
curl.exe -i http://127.0.0.1:5320/api/tickets/1
```

اگر process تازه باشد Id نخست ۱ است.
دادهٔ هر نمونه مستقل و در حافظهٔ همان process است.
[فایل‌ها و تغییر مرحلهٔ نمونه](../../../samples/31-rest-pagination/README.md) را کنار این درس باز کن.
فرمان `python scripts/controller_smoke.py` همهٔ snapshotهای controller این بخش را بررسی می‌کند.
برای توقف server از Ctrl+C استفاده کن.

## تمرین مستقل

یک query اختیاری برای sort محدود به id و title طراحی کن.
نام fieldهای دلخواه client را مستقیم به SQL آینده نچسبان؛ allowlist لازم است.
برای title برابر tie-breaker مشخص کن و چند صفحه را با دادهٔ تکراری تست کن.
پاسخ خطای sort نامعتبر را مستند کن.

## تمرین تکمیلی

نسخهٔ cursor بر اساس آخرین Id را روی کاغذ طراحی کن.
حذف یک رکورد بین دو درخواست را با offset و cursor مقایسه کن.
مشخص کن totalCount همچنان لازم است یا می‌تواند هزینهٔ اضافه باشد.

## معیار عبور

- [ ] filter پیش از count و page اعمال می‌شود.
- [ ] ترتیب قطعی و tie-breaker را می‌شناسم.
- [ ] صفحهٔ خالی با پارامتر نامعتبر فرق دارد.
- [ ] page خیلی بزرگ overflow نمی‌کند.
- [ ] محدودیت snapshot بین دو request را توضیح می‌دهم.

## منابع رسمی

- [HTTP Semantics — RFC 9110](https://www.rfc-editor.org/rfc/rfc9110.html)
- [Pagination and keyset comparison](https://learn.microsoft.com/en-us/ef/core/querying/pagination)
