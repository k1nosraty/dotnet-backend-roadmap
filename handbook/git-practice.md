# ثبت تغییرات تمرین با Git

برای بک‌اند حرفه‌ای فقط اجرای برنامه کافی نیست؛ باید بتوانی تغییر کوچک، تاریخچه و توضیح قابل بررسی داشته باشی.
این راهنما را پس از درس ۰۰۱ بخوان و از اولین پروژهٔ تمرینی استفاده کن.
GitHub محل remote repository است؛ Git ابزار مدیریت نسخهٔ محلی است.

## دریافت مرجع دوره

از صفحهٔ [repository](https://github.com/k1nosraty/dotnet-backend-roadmap) گزینهٔ Code و Download ZIP را انتخاب کن، یا با Git نصب‌شده:

```powershell
git clone https://github.com/k1nosraty/dotnet-backend-roadmap.git
Set-Location dotnet-backend-roadmap
```

دستورهای درس‌ها فرض می‌کنند terminal در همین ریشه است.
اگر پروژه را ZIP گرفته‌ای، برای اجرای آن نیاز به init کردن مرجع دوره نداری.
نمونهٔ مرجع را با تمرین مستقل خودت یکی نکن.

## repository جدا برای تمرین

پوشهٔ exercises در مرجع دوره ignored است تا کد شخصی وارد تغییرات مرجع نشود.
می‌توانی داخل هر پروژهٔ تمرین repository مستقل بسازی؛ nested بودن آن به معنی ثبت خودکار در remote دوره نیست.
مثال پس از نصب Git و ساخت project:

```powershell
dotnet new console -o exercises/MyPractice --framework net10.0
dotnet new gitignore -o exercises/MyPractice
git -C exercises/MyPractice init
```

نام و ایمیل commit را برای repository تمرین خودت با مقادیر انتخابی خودت تنظیم کن:

```powershell
git -C exercises/MyPractice config user.name "Your Name"
git -C exercises/MyPractice config user.email "your-address@example.com"
```

این دو مقدار نمونه‌اند و باید خودت جایگزین کنی؛ credential یا رمز GitHub نیستند.
اگر ایمیل خصوصی می‌خواهی، تنظیمات ایمیل GitHub و آدرس noreply حساب خودت را بررسی کن.

## چرخهٔ تغییر کوچک

```powershell
git -C exercises/MyPractice status --short
git -C exercises/MyPractice add Program.cs MyPractice.csproj .gitignore
git -C exercises/MyPractice diff --cached
git -C exercises/MyPractice commit -m "Add first runnable exercise"
```

status فایل تازه و تغییرکرده را نشان می‌دهد.
add تغییر را stage می‌کند؛ diff --cached چیزی را نشان می‌دهد که commit خواهد شد.
commit یک نقطهٔ تاریخچهٔ محلی می‌سازد و خودکار push نمی‌کند.
قبل از commit فرمان build و سناریوهای پذیرش مربوط را اجرا کن.
یک commit با یک تغییر قابل توضیح از چند قابلیت مخلوط ساده‌تر بررسی می‌شود.

## چه چیزی ثبت شود؟

| فایل | تصمیم |
| --- | --- |
| کد، csproj، README و تست | معمولاً ثبت شود |
| bin و obj | ثبت نشود؛ خروجی قابل بازسازی است |
| فایل JSON شخصی، secret، token و .env | در تمرین عمومی ثبت نشود |
| دادهٔ آزمایشی بی‌خطر و کوچک | با تصمیم و توضیح ثبت شود |

.gitignore جلوی ثبت فایل از قبل tracked را خودکار نمی‌گیرد.
اگر چیزی در commit اشتباه دیدی، پیش از ادامه علت را برطرف کن؛ این راهنما فرمان پاک کردن تاریخچه یا force push ارائه نمی‌کند.

## تمرین branch

```powershell
git -C exercises/MyPractice switch -c lesson-003-validation
```

در این branch فقط validation تمرین را اضافه و build کن.
تفاوت کد را با git diff و تاریخچه را با git log --oneline بخوان.
git diff بدون --cached فقط تغییر tracked و unstaged را نشان می‌دهد؛ فایل untracked را در status پیدا کن.
نام branch به تنهایی تست یا انتشار انجام نمی‌دهد.
کار تیمی با pull request، review و CI در مراحل بعدی توسعه تکمیل می‌شود.

## معیار عبور

- [ ] مرجع دوره و repository تمرین من جدا هستند.
- [ ] پیش از commit فایل‌های staged را دیده‌ام.
- [ ] build موفق و نتیجهٔ سناریوها را در README تمرین ثبت کرده‌ام.
- [ ] bin، obj و دادهٔ حساس در commit نیست.
- [ ] توضیح می‌دهم commit محلی، push و انتشار سه کار متفاوت‌اند.

## منابع رسمی

- [Install Git](https://git-scm.com/downloads)
- [Git basics](https://git-scm.com/book/en/v2/Git-Basics-Getting-a-Git-Repository)
- [Recording changes](https://git-scm.com/book/en/v2/Git-Basics-Recording-Changes-to-the-Repository)
