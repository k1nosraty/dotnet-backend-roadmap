# SupportDesk CLI — تحویل بخش دوم

برنامهٔ آموزشی .NET 10 برای درخواست‌های پشتیبانی با منو، فرمان مستقیم و JSON پایدار.

## اجرا

از ریشهٔ repository:

```powershell
New-Item -ItemType Directory -Force exercises | Out-Null
dotnet run --project samples/21-supportdesk-cli/SupportDesk.csproj -- exercises/supportdesk.json
```

بدون command منو باز می‌شود. برای command مستقیم، پس از مسیر فایل فرمان را بده.

| فرمان | مثال آرگومان پس از مسیر |
| --- | --- |
| ثبت | `add "Network issue" High` |
| فهرست | `list` یا `list --open` |
| جست‌وجو | `find 1` |
| تغییر عنوان | `rename 1 "Router issue"` |
| یادداشت | `note 1 "Configuration corrected"` |
| شروع | `start 1` |
| بستن | `close 1` |
| حذف | `delete 1` |
| گزارش | `report` |
| راهنما | `help` |

## قواعد

عنوان ۳ تا ۱۲۰، یادداشت ۳ تا ۵۰۰ واحد UTF-16 پس از Trim است.
هر Ticket حداکثر پنج یادداشت بدون تکرار Ordinal دارد.
Start از Open و Close از InProgress با یادداشت مجاز است.
Ticket بسته قابل rename یا note نیست.
اولویت Low، Normal یا High است؛ پیش‌فرض Normal.

## داده

Schema: supportdesk-v1. NextId پایدار است و پس از حذف کاهش نمی‌یابد.
فایل‌های نمونه‌های قبلی بدون migration قبول نمی‌شوند.
قرارداد تک‌process و فایل کوچک است؛ اجرای هم‌زمان دو نویسنده پشتیبانی نمی‌شود.
فایل موقت ریسک بازنویسی مستقیم را کم می‌کند؛ تضمین transaction و قطع برق نیست.
مسیر نسبی نسبت به current working directory است.

## exit code

۰ موفق، ۱ زیرساخت/دادهٔ خراب، ۲ ورودی/وضعیت/ظرفیت، ۳ ناموجود، ۱۳۰ لغو.
در منو expected failure جلسه را نمی‌بندد؛ خطای storage آن را متوقف می‌کند.

## publish

```powershell
dotnet publish samples/21-supportdesk-cli/SupportDesk.csproj -c Release -p:UseAppHost=false -o exercises/published-supportdesk
dotnet exercises/published-supportdesk/SupportDesk.dll exercises/published-data.json report
```

runtime .NET 10 روی مقصد لازم است.
داده را بیرون پوشهٔ publish نگه دار.

## بررسی و یادگیری

[درس ۰۲۲](../../lessons/02-design/022-cli-delivery/README.md) توضیح ساختار، محدودیت‌ها و ارزیابی مستقل را دارد.
`python scripts/smoke.py` از ریشهٔ دوره بررسی‌های ساخت و رفتار را اجرا می‌کند.
این برنامه audit، login یا authorization ندارد؛ برای تمرین backend و تک‌کاربره است.
