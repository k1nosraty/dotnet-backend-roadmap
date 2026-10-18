# درس 072 — boundary و negative case و test double

## خروجی قابل مشاهده

ResolveTicket را بدون دیتابیس واقعی آزمایش می‌کنی و فرق invalid، missing، conflict و موفقیت commit را می‌بینی.
وقتی ورودی خراب است ذخیره‌سازی صدا زده نمی‌شود؛ وقتی commit خطا می‌دهد موفقیت برنمی‌گردد.
پیش‌نیاز: [درس ۷۱](../071-unit/README.md)، port/adapter و async و CancellationToken.

## از تست کلاس به تست use case

نمونهٔ samples/71-test-design همان Domain مستقل را نگه می‌دارد و Application/ResolveTicket.cs را اضافه می‌کند.
ITicketStore قرارداد Load و Save با version را تعریف می‌کند؛ Application، Npgsql یا HTTP را نمی‌شناسد.
ResolveOutcome یک قرارداد کاربردی است، نه status code HTTP. نگاشت 400/404/409 در درس بعد در endpoint انجام می‌شود.
فایل tests/072-test-design/ResolveTests.cs وابستگی را با RecordingStore کنترل می‌کند.

```powershell
dotnet build samples/71-test-design/SupportDesk.csproj
dotnet test tests/072-test-design/SupportDesk.Tests.csproj
dotnet test tests/072-test-design/SupportDesk.Tests.csproj --filter "FullyQualifiedName~ResolveTests"
```

کنسول نمونه فقط Domain را نمایش می‌دهد؛ suite، خود use case و port را بررسی می‌کند.

## جدول تصمیم پیش از تست

| وضعیت | نتیجه | تعامل مجاز |
| --- | --- | --- |
| token از ابتدا لغوشده | OperationCanceledException | بدون Load/Save |
| id خالی یا resolution نامعتبر | Invalid | بدون Load/Save |
| Load منبعی ندارد | Missing | بدون Save |
| وضعیت غیر InProgress | Conflict | بدون Save |
| Save نتیجهٔ Saved دارد | Resolved | یک تغییر بسته‌شده با version مورد انتظار |
| Save نتیجهٔ Conflict دارد | Conflict | موفقیت اعلام نشود |
| Save منبع را حذف‌شده می‌بیند | Missing | موفقیت اعلام نشود |
| Save دچار IOException می‌شود | exception به caller برسد | منبع immutable قبلی تغییر نکند |

همهٔ خانه‌ها را با یک تست بسیار بزرگ و شرط‌های فراوان اجرا نکن؛ Theory برای نگاشت outcomeها مناسب است و تست خطا مستقل می‌ماند.
مرزهای scalar متن و state از درس قبل نیز در همین snapshot دوباره اجرا می‌شوند.

## test double چه چیزی را ثابت می‌کند؟

در این دوره stub به جایگزینِ داده‌دهنده، spy به ثبت‌کنندهٔ تعامل و fake به پیاده‌سازی سادهٔ کاری می‌گوییم؛ نام‌ها در ادبیات و ابزارها یکسان استفاده نمی‌شوند.
RecordingStore هم پاسخ را کنترل می‌کند و هم اطلاعات Save را ثبت می‌کند؛ اسم آن رفتار را روشن می‌کند.
یک mocking library برای این interface کوچک ضروری نیست؛ اگر ابزار اضافه شد باید خوانایی و هزینهٔ نگهداری را بهتر کند.
این double، isolation دیتابیس یا SQL صحیح یا durability را ثابت نمی‌کند؛ آن‌ها موضوع درس ۷۴ هستند.

## assertion روی تعامل را هدفمند انتخاب کن

برای ورودی نامعتبر، صفر بودن Load یک قرارداد مفید است: درخواست خراب نباید I/O انجام دهد.
برای Save، انتقال همان CancellationToken و version مورد انتظار مهم است چون حذفشان cancellation یا کنترل رقابت را می‌شکند.
برای objectهای immutable، نوع و state خروجی و حفظ state ورودی را بسنج.
روی نام متد private، ترتیب تصادفی helperها یا هر خط داخلی assert نگذار؛ refactor صحیح نباید بی‌دلیل ده‌ها تست را بشکند.

## شکست commit و snapshot قبلی

use case ابتدا از snapshot یک Ticket بسته‌شدهٔ تازه می‌سازد و سپس commit را درخواست می‌کند.
ساخت object تازه به معنی ثبت موفق نیست؛ نتیجهٔ واقعی port مشخص می‌کند که use case موفق است یا conflict.
تستِ FailSave عمداً IOException می‌دهد و ثابت می‌کند caller هنوز خطا می‌بیند و snapshot اصلی InProgress است.
این تست، rollback فیزیکی database را شبیه‌سازی نمی‌کند؛ فقط قرارداد لایهٔ Application و immutable بودن مدل را بررسی می‌کند.
اگر adapter در دنیای واقعی نیمه‌کاره بنویسد، تست double آن را پیدا نخواهد کرد.

## async و cancellation

تست async باید Task برگرداند و عملیات را await کند؛ async void و .Result و .Wait را در این suite استفاده نمی‌کنیم.
لغو قبل از شروع نباید تعامل جدید ایجاد کند؛ token نیز تا port ارسال می‌شود.
این آزمون، قطع query در حال اجرا روی شبکه را ثابت نمی‌کند؛ همان ادعایی را بنویس که تست می‌سنجد.
برای رفتار زمانی از sleep تصادفی استفاده نکن؛ اگر زمان وارد تصمیم شد، یک TimeProvider کنترل‌پذیر به قرارداد بده.

## state مشترک و قابلیت اجرای مستقل

هر تست double خودش را می‌سازد و row مورد نیاز را صریح می‌دهد.
به static mutable store یا تست دیگری که قبلاً داده بسازد وابسته نیستیم.
ورودی دو تست ممکن است شبیه باشد، ولی helper نباید جزئیات مهم سناریو را مخفی کند.
نام Started فقط seed معتبر و تکراری را می‌سازد؛ نتیجهٔ مورد انتظار همچنان در هر تست دیده می‌شود.

## runner و شواهد

```powershell
python scripts/testing_smoke.py
```

در حالت عادی ۷۱ و ۷۲ و تست HTTP درس ۷۳ اجرا می‌شوند؛ پروژهٔ Docker فقط build می‌شود.
runner با TRX نبود شکست و skip و حداقل تعداد کشف‌شده را بررسی می‌کند.
این حداقل تعداد، جای ارزیابی کیفیت تست و سناریوهای واقعی را نمی‌گیرد؛ فقط کشف نشدن suite را پنهان نمی‌کند.

## تمرین مستقل

برای id خالی و resolution بلند یک تست بنویس که هیچ Load/Save رخ ندهد.
یک double بساز که در Load خطا بدهد و نشان بده Save هرگز اتفاق نمی‌افتد.
سپس رفتار حذف شدن منبع بین Load و Save را بدون تبدیل آن به Resolved بررسی کن.

### سرنخ

result و state و side effect را جدا بنویس؛ برای هر سناریو لازم نیست هر سه نوع assertion داشته باشی.

## معیار عبور

- جدول تصمیم را پیش از پیاده‌سازی تست بنویسی.
- invalid و missing و conflict را مخلوط نکنی.
- خطای ذخیره و cancellation به موفقیت تبدیل نشوند.
- هزینه و مرز شواهد test double را توضیح بدهی.
- تست‌ها به ترتیب و زمان و state مشترک وابسته نباشند.

## منابع رسمی

- [Microsoft: طراحی unit test و اصطلاحات double](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [شروع xUnit v2 و async test](https://xunit.net/docs/getting-started/v2/getting-started)

[قبلی](../071-unit/README.md) · [فهرست بخش](../README.md) · [بعدی](../073-integration/README.md)
