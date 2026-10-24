# آزمایشگاه یک‌بارمصرف عملیات

این راهنما از ریشهٔ مخزن در PowerShell اجرا می‌شود. SDK 10، Python 3، Docker CLI/Engine با Linux containers لازم‌اند.
این containerها فقط دادهٔ مصنوعی دوره دارند. نام‌ها و پورت‌ها را اگر از قبل استفاده شده‌اند تغییر بده؛ container موجودِ دیگری را پاک نکن.

## PostgreSQL برای درس‌های ۸۴ و ۸۶

```powershell
$env:POSTGRES_PASSWORD = [guid]::NewGuid().ToString('N')
docker run --rm -d --name course-ops-pg -p 127.0.0.1:5544:5432 -e POSTGRES_USER=course -e POSTGRES_DB=jobs -e POSTGRES_PASSWORD postgres:18.6
docker exec course-ops-pg pg_isready -h 127.0.0.1 -U course -d jobs
$env:SUPPORTDESK_JOBS = "Host=127.0.0.1;Port=5544;Database=jobs;Username=course;Password=$env:POSTGRES_PASSWORD;Timeout=3;Command Timeout=3"
```

تا پاسخ pg_isready آماده نشده، مرحلهٔ بعد را اجرا نکن. connection string را چاپ یا commit نکن؛ این نقش در container آموزشی superuser است و الگوی مجوز محصول نیست.

```powershell
dotnet run --project samples/83-durable-jobs/SupportDesk.csproj -- init
dotnet run --project samples/83-durable-jobs/SupportDesk.csproj -- seed
```

init فقط schema خالی آزمایش را می‌سازد؛ ابزار migration یا upgrade نیست.

## Redis اختیاری برای درس ۸۳

```powershell
docker run --rm -d --name course-ops-redis -p 127.0.0.1:6383:6379 redis:8.2
$env:SUPPORTDESK_REDIS = '127.0.0.1:6383'
dotnet run --project samples/82-versioned-cache/SupportDesk.csproj
```

پورت‌ها فقط به loopback وصل شده‌اند. این Redis بدون حساب محصول و فقط برای دادهٔ مصنوعی است؛ ACL و TLS و محدودیت حافظهٔ محصول را باید جدا تنظیم کنی.
8.2 یک minor tag است و patch آن می‌تواند تغییر کند؛ بازتولید دقیق image نیاز به ثبت digest دارد.

## پایان آزمایش

اول worker را با Ctrl+C متوقف کن. فقط containerهایی را که با همین راهنما ساختی متوقف کن:

```powershell
docker stop course-ops-pg course-ops-redis
Remove-Item Env:SUPPORTDESK_JOBS -ErrorAction SilentlyContinue
Remove-Item Env:SUPPORTDESK_REDIS -ErrorAction SilentlyContinue
Remove-Item Env:POSTGRES_PASSWORD -ErrorAction SilentlyContinue
```

اگر فقط یکی را ساخته‌ای، فقط همان نام را بده. --rm باعث می‌شود با توقف، دادهٔ همان container حذف شود. اینجا volume دادهٔ واقعی نداریم؛ برای محصول از این دستورهای موقتی استفاده نکن.
اسکریپت خودکار بازیابی، نام تصادفی و رمز خودش را می‌سازد و نیازی به این containerهای دستی ندارد.
