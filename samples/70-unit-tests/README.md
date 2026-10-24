# نمونهٔ درس 71

[متن درس و تمرین‌ها](../../lessons/07-testing/071-unit/README.md) را بخوان.
نمونهٔ مستقل و کوچک است؛ محصول کامل یا host آمادهٔ انتشار نیست.

```powershell
dotnet build samples/70-unit-tests/SupportDesk.csproj
dotnet test tests/071-unit-tests/SupportDesk.Tests.csproj
```

پروژهٔ test بیرون از sample قرار دارد و فقط از test به app reference دارد.
runner کلی: python scripts/testing_smoke.py؛ اجرای PostgreSQL واقعی: python scripts/testing_smoke.py --with-docker.
درس ۷۴ به Docker Linux engine نیاز دارد؛ build آن به معنی اجرای دیتابیس نیست.
هویت X-Test-Actor فقط در test assembly ساخته می‌شود و host عادی آن را نمی‌پذیرد.
