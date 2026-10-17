# درس 055 — mapping و هزینهٔ abstraction

## خروجی قابل مشاهده

مدل ورودی مشتری، command برنامه، Domain، row ذخیره و پاسخ عمومی را جدا می‌کنی.
فقط فیلدهای لازم از هر مرز عبور می‌کنند و mapping صریح قابل خواندن باقی می‌ماند.
با ورودی دارای Status و Version اضافی بررسی می‌کنی که این فیلدها setter دامنه نمی‌شوند.

پیش‌نیاز: [درس 054](../054-ports/README.md)، DTO، nullable reference و JSON.
نمونه یک اجرای کنسولی دارد؛ HTTP featureها در درس ۵۶ اضافه می‌شوند.
package mapper یا mediator تازه‌ای در این درس لازم نیست.

## یک تیکت، چند شکل

| شکل داده | مالک | هدف |
| --- | --- | --- |
| RenameRequest | Presentation | فقط Title دریافتی |
| RenameTicketCommand | Application | Id معتبر از route و عنوان درخواست |
| Ticket | Domain | هویت و قواعد و noteها |
| StoredTicket | Infrastructure | schema فایل، noteها و Version |
| TicketResponse | Application | Id، Title، Status، NoteCount |

چند type برای یک مفهوم، وقتی قراردادهای متفاوت دارند معنا پیدا می‌کند.
اگر دو type واقعاً قرارداد یکسان دارند، ساخت چند نسخه صرفاً برای نام‌های بیشتر ضروری نیست.
در این مثال Version داخلی نباید از request قابل تنظیم باشد و متن noteهای خصوصی هم در پاسخ خلاصه نیست.
پس جداسازی شکل‌ها یک نیاز مشخص دارد.

## input را به entity bind نکن

بدنهٔ زیر ممکن است از caller برسد:

```json
{
  "Title": "  New title  ",
  "Status": "Closed",
  "Version": 999,
  "Id": 99
}
```

RenameRequest فقط Title دارد.
شناسهٔ command از آرگومان route گرفته می‌شود و از body کپی نمی‌شود.
Status و Version در این request عضو معتبری ندارند.
در تنظیم پیش‌فرض System.Text.Json، فیلدهای ناشناخته نادیده گرفته می‌شوند.
می‌توان سیاست strict را با UnmappedMemberHandling انتخاب کرد؛ این نمونه برای مقایسه، سیاست ignore پیش‌فرض را نگه می‌دارد.
این انتخاب باید بخشی از قرارداد API باشد، نه تکیه بر حدس مشتری.

## کد صریح mapping ورودی

Presentation/Requests.cs:

```csharp
public sealed record RenameRequest(string? Title);
public static RenameTicketCommand ToCommand(int routeId, RenameRequest request)
    => new(routeId, request.Title);
```

کد بالا منطق وضعیت یا دسترسی به store ندارد.
عنوان هنوز می‌تواند null یا نامعتبر باشد؛ command معتبرِ شکلی الزاماً مدل معتبر نیست.
RenameTicket ورودی را بررسی می‌کند، TicketTitle می‌سازد و Ticket.Rename را اجرا می‌کند.
اگر تیکت بسته باشد، mapping اجازهٔ دور زدن آن را نمی‌دهد.

## پاسخ خلاصه

src/Application/TicketMapping.cs یک projection کوچک دارد:

```csharp
return new(ticket.Id, ticket.Title, ticket.Status, ticket.Notes.Count);
```

TicketResponse نه Version دارد و نه مجموعهٔ noteها.
مقدار NoteCount از Domain محاسبه می‌شود؛ caller نمی‌تواند با آن مجموعه را تغییر دهد.
CloseTicket، ResolveTicket و RenameTicket از این projection استفاده می‌کنند.
این اشتراک کوچک ارزش دارد چون شکل پاسخ مشترک است، نه صرفاً چون هر سه کلاس متدی به نام Map می‌خواهند.
اگر آیندهٔ پاسخ‌ها متفاوت شد، این اشتراک را دوباره ارزیابی کن.

## mapping ذخیره با mapping پاسخ فرق دارد

StorageMapping باید noteهای کامل و نسخه را برای ذخیره حفظ کند.
جهت خواندن به Ticket.Restore می‌رود تا دادهٔ نامعتبر رد شود.
جهت پاسخ عمومی فقط خلاصه می‌سازد.
ساخت یک GenericMapper که هر دو را با reflection و setterها انجام دهد، این تفاوت‌ها را پنهان می‌کند.
یک تبدیل دوطرفهٔ عمومی ممکن است ناخواسته مسیر bypass قانون بسازد.

## چه چیزی در mapper قرار نمی‌گیرد؟

- تشخیص مجاز بودن بستن تیکت: مسئولیت Domain.
- خواندن و ذخیره و کنترل نسخه: مسئولیت orchestration و adapter.
- تصمیم دربارهٔ 400 و 409: مسئولیت Presentation.
- گرفتن dependency از service locator: نیازی در این mapping کوچک ندارد.

Mapper باید معلوم کند داده از کجا به کجا می‌رود.
اگر برای mapping به HttpContext و DbContext و سرویس مجوز نیاز پیدا کرده‌ای، احتمالاً چند مسئولیت را مخلوط کرده‌ای.

## آیا کتابخانهٔ mapping لازم است؟

در این مثال projection چهار فیلد دارد و دستی بودنش هزینهٔ کمی دارد.
کتابخانه می‌تواند در قراردادهای زیاد و تکراری مفید باشد، اما هزینهٔ dependency، تنظیم، اشکال‌زدایی و یادگیری دارد.
قبل از افزودن آن، سناریوهای null، نام فیلد متفاوت و حذف فیلد خصوصی را بنویس.
نباید موفقیت build را معادل حفظ قرارداد serialization بدانی.
انتخاب mapper باید با مسئله و اندازهٔ تغییر توضیح داده شود.

## abstraction مفید یا تشریفاتی؟

| پیشنهاد | در این نمونه |
| --- | --- |
| ITicketStore | تعهد مستقل برای چند adapter واقعی |
| TicketMapping مشترک | projection ثابت پاسخ سه use case |
| ICloseTicket فقط با یک پیاده‌سازی | فعلاً نیاز اثبات‌شده ندارد |
| GenericRepository برای تمام typeها | تعهد aggregate و نسخه را مبهم می‌کند |
| BaseHandler با pipeline پیچیده | برای سه عملیات کوچک هزینهٔ اضافه دارد |

تعداد interface معیار کیفیت نیست.
تغییر adapter بدون تغییر use case یک شاهد مفید است.
وجود type بیشتر بدون کاهش coupling، شاهد کافی نیست.

## اجرا و شواهد

```powershell
dotnet run --project samples/54-explicit-mapping/SupportDesk.csproj
dotnet run --project samples/54-explicit-mapping/SupportDesk.csproj -- --check
python scripts/architecture_next_smoke.py
```

خروجی عادی `Success: 1 Closed notes=1` است.
بررسی mapping عبارت `PASS: explicit mapping and constrained request shape` می‌دهد.
آزمایش serialize پاسخ، مجموعهٔ دقیق فیلدهای خروجی را مقایسه می‌کند.
آزمایش request اضافی هم نشان می‌دهد Id مسیر، وضعیت و نسخهٔ storage از body نوشته نمی‌شوند.
بررسی‌های aggregate، adapter و orchestration قبلی هم اجرا می‌شوند.

این بررسی، مجوز دسترسی یا جلوگیری از IDOR را ثابت نمی‌کند.
DTO محدود، جای بررسی مالکیت و احراز هویتِ فصل امنیت نیست.
همچنین این نمونه فقط نمایش خلاصه دارد؛ تصمیمِ خصوصی بودن note در محصول واقعی به نیاز و مجوز بستگی دارد.

## تمرین مستقل

برای گزارش عمومی یک TicketSummary بساز که فقط Id، Title و Status داشته باشد.
یک response مخصوص کارشناس هم طراحی کن که noteهای لازم را به‌صورت copy نمایش بدهد.
هیچ‌یک نباید Version داخلی را قابل نوشتن کند.
دربارهٔ اینکه caller حق دریافت response دوم را دارد فقط قرارداد بنویس؛ سازوکار احراز هویت را فعلاً جعل نکن.

### راهنمای مرحله‌ای

1. ابتدا JSON مورد انتظار هر مخاطب را بنویس.
2. نام و nullability هر فیلد را مشخص کن.
3. projection را در سمت مالک پاسخ قرار بده.
4. با assertion روی کلیدهای JSON بررسی کن که فیلد ناخواسته ظاهر نشده باشد.
5. تغییر نام یک فیلد را امتحان کن و اثرش را روی مصرف‌کننده توضیح بده.

## معیار عبور

- درخواست اضافی نتواند Status، Id یا Version را بنویسد.
- شناسهٔ command از مسیر معتبر تعیین شود.
- response فقط چهار فیلد قراردادش را داشته باشد.
- تیکت بسته با RenameRequest هم تغییر نکند.
- mapper به storage و HTTP وابسته نشود.
- یک abstraction پیشنهادی را با هزینه و نیاز مشخص قبول یا رد کنی.

## منابع رسمی

بررسی در 2026-10-17، مبنا .NET 10:

- [سیاست فیلدهای ناشناختهٔ JSON](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members)
- [مرزهای قرارداد و Infrastructure](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)

[قبلی](../054-ports/README.md) · [بعدی](../056-vertical-slices/README.md)
