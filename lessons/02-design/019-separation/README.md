# درس ۰۱۹ — جداسازی presentation، application و persistence

[قبلی](../018-value-objects/README.md) · [خانه](../../../README.md)

## خروجی درس

یک برنامهٔ ثبت و جست‌وجوی درخواست با JSON می‌سازی.
برای هر کلاس محل و مسئولیتش را توضیح می‌دهی.
مسیر یک عملیات را از command تا domain و storage و خروجی دنبال می‌کنی.
می‌توانی presentation را بدون دست‌کاری قانون عنوان عوض کنی.

## پیش‌نیاز

value object، قرارداد reader، constructor injection، JSON و مدیریت failure.
این نمونه برای تمرکز بر مرزها synchronous است و عملیات ثبت و جست‌وجو دارد.
منوی کامل، وضعیت و یادداشت‌های نسخهٔ قبلی را بی‌بررسی داخل آن کپی نمی‌کنیم.
در تمرین می‌توان قابلیت‌های بعدی را با قرارداد مشخص اضافه کرد.

## ۱. سه مسئولیت و یک مدل

presentation با روش تعامل کاربر سروکار دارد.
application عملیات مورد درخواست کاربر را هماهنگ می‌کند.
domain مفهوم عنوان معتبر و موجودیت را تعریف می‌کند.
persistence دادهٔ فایل و روش خواندن و نوشتن را می‌فهمد.
Program نقطهٔ ساخت و اتصال این اجزاست.

این تقسیم به معنی چهار process یا چهار deployment نیست.
نمونه در یک executable و یک csproj اجرا می‌شود.

## ۲. ساختار واقعی پوشه‌ها

| مسیر | فایل‌ها | مسئولیت |
| --- | --- | --- |
| Domain | Ticket.cs، TicketTitle.cs | داده و invariantهای مدل |
| Application | Contracts.cs، TicketService.cs | portها، use case و خروجی |
| Infrastructure | StoreDocument.cs، JsonTicketStore.cs | فرمت JSON و I/O |
| Presentation | ConsoleCommands.cs | parsing فرمان و پیام کاربر |
| ریشه | Program.cs | composition root |

هر فایل واقعاً کد مربوط دارد؛ controller یا service خالی برای ظاهر ساختار ایجاد نشده است.
در وب، endpoint یا controller نقش دیگری در presentation خواهد داشت.
classهای این برنامه هنوز controller HTTP نیستند.

## ۳. جهت وابستگی‌ها

| مصرف‌کننده | چه چیزی را می‌شناسد؟ |
| --- | --- |
| Domain | امکانات پایهٔ .NET |
| Application | Domain و قراردادهای خودش |
| Infrastructure | Application و Domain |
| Presentation | Application و قرارداد خروجی |
| Program | concreteها برای اتصال |

این جدول جهت design را مشخص می‌کند.
چون یک assembly داریم، کامپایلر همهٔ namespaceها را می‌بیند و این مرز را خودکار enforce نمی‌کند.
در فصل معماری، project reference و تست وابستگی ساختاری را اضافه می‌کنیم.
پوشه تنها مانع ارجاع اشتباه نیست.

## ۴. domain ثبت کوچک

```csharp
public sealed class Ticket
{
    public int Id { get; }
    public TicketTitle Title { get; }
    public Ticket(int id, TicketTitle title)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        ArgumentNullException.ThrowIfNull(title);
        Id = id;
        Title = title;
    }
}
```

Id مثبت و Title معتبر لازم است.
مدل نام مسیر فایل یا متن پیام خطای کنسول را نمی‌شناسد.
این مدل فقط برای ثبت است و Status یا Notes ندارد.
اختلاف با مدل درس ۰۱۳ یک کاهش صریح scope آموزشی است، نه حذف خاموش یک نیازمندی در محصول.

## ۵. port ساخت و خواندن

```csharp
public interface ITicketCreator
{
    Ticket Create(TicketTitle title);
}
public interface ITicketReader
{
    Ticket? FindById(int id);
}
```

Create فقط بعد از موفقیت persistence نتیجهٔ موفق می‌دهد.
Id در storage تولید می‌شود، چون آن جزء دادهٔ موجود و generator را می‌بیند.
reader برای Id ناموجود null می‌دهد؛ failure همچنان exception است.
این مرزهای خاص قابلیت‌اند، نه generic repository اجباری برای همهٔ نوع‌ها.

## ۶. use case ثبت

```csharp
public TicketSummary Register(string rawTitle)
{
    TicketTitle title = TicketTitle.Create(rawTitle);
    Ticket ticket = creator.Create(title);
    return new TicketSummary(ticket.Id, ticket.Title.Value);
}
```

application string خام را به ارزش domain تبدیل می‌کند.
persistence را صدا می‌زند و فقط پس از موفقیت خروجی می‌سازد.
Console.WriteLine و File.WriteAllText در این متد نداریم.
اگر storage خطا بدهد، Saved در presentation نباید نمایش داده شود.

## ۷. DTO خروجی

TicketSummary یک record شامل Id و Title string است.
این خروجی انتقال داده است و به FilePath یا قالب JSON ذخیره‌شده وابسته نیست.
caller یک Ticket mutable از storage دریافت نمی‌کند؛ summary از دادهٔ مدل ساخته می‌شود.
در API آینده خروجی عمومی هم باید آگاهانه تعریف شود تا جزئیات entity افشا نشود.
DTO و storage document لزوماً یکی نیستند، حتی اگر امروز fieldهای شبیه داشته باشند.

## ۸. use case جست‌وجو

application ابتدا Id مثبت را بررسی می‌کند.
reader را صدا می‌زند و Ticket را به TicketSummary تبدیل می‌کند.
برای null، خود result هم null است و presentation تصمیم نمایش می‌گیرد.
application پیام Ticket not found را به عنوان Title یک رکورد جعلی نمی‌سازد.

## ۹. presentation command

ConsoleCommands فرمان add یا find را parse می‌کند.
Id متنی با NumberStyles.None و CultureInfo.InvariantCulture به عدد تبدیل می‌شود.
اگر syntax خراب باشد، usage و کد خروج ۲ می‌دهد.
اگر use case خطا بدهد، پیام معنی‌دار و کد خروج ۱ می‌دهد.
موفقیت و ناموجود بودن داده طبق قرارداد فعلی کد خروج صفر دارند.
این policy CLI است؛ status code HTTP در فصل وب جدا تعریف می‌شود.

## ۱۰. مدیریت خطا در مرز

| failure | پیام CLI |
| --- | --- |
| JSON نحوی خراب | Invalid JSON؛ فایل حفظ شود |
| schema یا دادهٔ نامعتبر | Invalid stored data |
| permission | Storage access denied |
| I/O دیگر | Storage failed؛ موفقیت گزارش نشود |
| عنوان نامعتبر | Invalid title or input |

exceptionهای storage در service به null تبدیل نشده‌اند.
UI نیز متن stack trace را در خروجی عمومی چاپ نمی‌کند.
برای عیب‌یابی واقعی بعداً logging ساختاریافته و error code اضافه می‌کنیم.

## ۱۱. storage document مستقل

قرارداد فایل این نمونه Schema برابر registration-v1 دارد.
Items شامل رکورد Id و Title است.
این schema با Version/Tickets فایل درس ۰۱۱ متفاوت است.
از مسیر فایل تازه استفاده کن؛ فایل قبلی باید به عنوان schema ناشناخته رد شود.
اگر migration می‌خواهی، باید mapping و قواعدش را جدا بنویسی.
بازنویسی خودکار فایل قدیمی بدون migration نداریم.

## ۱۲. Load با validation

JsonTicketStore فایل را می‌خواند، schema را بررسی می‌کند و rowها را به مدل معتبر تبدیل می‌کند.
Title از TicketTitle.Create عبور می‌کند.
Id تکراری و row null رد می‌شوند.
فقط FileNotFoundException به فهرست خالی تبدیل می‌شود؛ خرابی یا access denied خالی نیست.

اگر فایل خراب باشد، Create پیش از Save شکست می‌خورد.
بنابراین فایل اصلی با دادهٔ تازه یا خالی overwrite نمی‌شود.
این مسیر یکی از سناریوهای اصلی بررسی است.

## ۱۳. Create و مرز موفقیت

Create دادهٔ فعلی را Load می‌کند.
بیشترین Id به علاوهٔ یک را انتخاب می‌کند و ظرفیت int را بررسی می‌کند.
Ticket معتبر می‌سازد و Save می‌کند.
فقط پس از Save، Ticket به caller بازمی‌گردد.
application از روی موفق شدن validation به تنهایی Saved اعلام نمی‌کند.

این generator قرارداد تک‌نویسنده دارد و حذف در این نمونه وجود ندارد.
برای concurrent process یا شناسهٔ هرگز تکرارنشونده پس از حذف، دیتابیس یا generator پایدار مناسب لازم است.

## ۱۴. Save مرحله‌ای

ابتدا فایل موقت هم‌پوشه نوشته می‌شود و سپس File.Move آن را جایگزین می‌کند.
cleanup تلاش می‌کند فایل موقت را حذف کند بدون پنهان کردن خطای اصلی.
این روش خطر truncate مستقیم مقصد را کم می‌کند.
transaction دیتابیس، جلوگیری از lost update یا تضمین قطع برق نیست.
دادهٔ کوچک و یک process فرض این نمونه است.

## ۱۵. composition root

```csharp
JsonTicketStore store = new(args[0]);
TicketService service = new(store, store);
ConsoleCommands commands = new(service);
return commands.Execute(args.Skip(1).ToArray());
```

همان store دو port را پیاده می‌کند؛ دو مخزن مستقل ساخته نشده‌اند.
Program انتخاب JSON و مسیر را می‌داند.
TicketService فقط portها را می‌بیند.
برای این گراف کوچک manual DI کافی است؛ container درس ۰۱۶ اختیاری است.

## ۱۶. اجرا از ریشه

```powershell
New-Item -ItemType Directory -Force exercises | Out-Null
dotnet run --project samples/18-ticket-layers/SupportDesk.csproj -- exercises/registration.json add "Network issue"
dotnet run --project samples/18-ticket-layers/SupportDesk.csproj -- exercises/registration.json find 1
```

خروجی در فایل تازه:

```text
Saved #1: Network issue
Found #1: Network issue
```

هر فرمان process تازه است؛ جست‌وجو باید دادهٔ ذخیره‌شدهٔ فرمان قبلی را ببیند.
مسیر نسبی نسبت به پوشهٔ کاری است، نه محل فایل project.

## ۱۷. مسیر یک درخواست روی کد

1. Program concreteها را می‌سازد.
2. ConsoleCommands دستور و syntax را تشخیص می‌دهد.
3. TicketService عنوان معتبر می‌سازد.
4. JsonTicketStore داده را Load و شناسه را انتخاب می‌کند.
5. domain شیء Ticket معتبر می‌سازد.
6. store داده را ذخیره می‌کند و نتیجه می‌دهد.
7. application summary می‌سازد.
8. presentation Saved را چاپ می‌کند.

برای find مسیر کوتاه‌تر است و Save انجام نمی‌شود.
جهت اجرای runtime با جهت وابستگی سورس یکسان فرض نمی‌شود؛ application implementation زیرساخت را از طریق port صدا می‌زند.

## تمرین مستقل

یک presentation دوم بساز که فرمان‌ها را از منوی تعاملی دریافت کند.
TicketService و JsonTicketStore را تغییر نده.
input خراب، EOF، add، find و failure فایل را با همان policy مشخص بررسی کن.
این تمرین نشان می‌دهد جدا شدن presentation فقط تغییر نام پوشه نبوده است.

## تمرین تکمیلی

یک InMemoryTicketStore با همان portها بنویس.
برای بررسی use case، آن را جای JSON store به Program بده.
failure واقعی JSON را با fake پوشش‌داده‌شده حساب نکن؛ هر implementation بررسی مستقل می‌خواهد.

## سناریوهای پذیرش

| حالت | انتظار |
| --- | --- |
| ثبت و process تازه | داده بازیابی شود |
| عنوان خراب | فایل قبلی ثابت؛ Saved چاپ نشود |
| find ناموجود | پیام ناموجود؛ فایل ساخته نشود |
| JSON یا schema خراب | خروج ۱؛ بایت‌ها ثابت |
| path پوشهٔ ناموجود | storage failure؛ Saved چاپ نشود |
| find صفر، منفی یا abc | usage و خروج ۲ |

## معیار عبور

- [ ] برای هر فایل یک مسئولیت مشخص دارم.
- [ ] domain و application از File و Console استفاده نمی‌کنند.
- [ ] Saved فقط پس از persistence موفق است.
- [ ] DTO خروجی و قرارداد فایل را جدا می‌دانم.
- [ ] مرز پوشه با enforcement کامپایلر یکی نیست.
- [ ] تفاوت runtime call و source dependency را توضیح می‌دهم.
- [ ] presentation دوم بدون تغییر use case کار می‌کند.

## منابع رسمی

- [Common application architectures](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- [Architectural principles](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)
- [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview)

## قدم بعد

[درس ۰۲۰](../020-errors/README.md) نتیجهٔ عملیات، exception و طراحی outcome را بررسی می‌کند.
پیش از آن presentation و سیاست failure پروژهٔ خودت را کامل کن.
