# إعداد BCrypt.Net-Next

## نظرة عامة

تم تحديث `SharedLinkService.cs` لاستخدام BCrypt.Net-Next لتشفير كلمات مرور الروابط المشتركة بدلاً من SHA256. BCrypt يوفر تشفيراً أكثر أماناً مع دعم Salt تلقائي.

## تثبيت الحزمة

### باستخدام .NET CLI:

```bash
dotnet add package BCrypt.Net-Next
```

### باستخدام Package Manager Console في Visual Studio:

```powershell
Install-Package BCrypt.Net-Next
```

### باستخدام NuGet Package Manager:

1. افتح NuGet Package Manager
2. ابحث عن `BCrypt.Net-Next`
3. اضغط Install

## التحقق من التثبيت

بعد تثبيت الحزمة، تأكد من أن الحزمة موجودة في ملف `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
</ItemGroup>
```

## التغييرات المطبقة

### 1. تحديث using statements

تم استبدال:
```csharp
using System.Security.Cryptography;
using System.Text;
```

بـ:
```csharp
using BCrypt.Net;
```

### 2. تحديث HashPassword method

**قبل:**
```csharp
private string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(password);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}
```

**بعد:**
```csharp
private string HashPassword(string password)
{
    // Use BCrypt.Net-Next for secure password hashing
    // BCrypt automatically generates a salt and includes it in the hash
    return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
}
```

### 3. تحديث VerifyPassword method

**قبل:**
```csharp
private bool VerifyPassword(string password, string hash)
{
    var passwordHash = HashPassword(password);
    return passwordHash == hash;
}
```

**بعد:**
```csharp
private bool VerifyPassword(string password, string hash)
{
    try
    {
        // Use BCrypt.Net-Next to verify the password
        // BCrypt automatically extracts the salt from the hash and verifies the password
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
    catch (Exception ex)
    {
        // Log error and return false if verification fails
        _logger.LogWarning(ex, "Error verifying password hash. This might indicate an old hash format.");
        return false;
    }
}
```

## المميزات

1. **أمان أفضل**: BCrypt يستخدم خوارزمية Blowfish مع salt تلقائي
2. **Work Factor قابل للتعديل**: تم ضبط workFactor على 12 (يمكن زيادته للأمان الأكبر)
3. **معالجة الأخطاء**: تمت إضافة try-catch للتعامل مع الأخطاء بشكل صحيح
4. **توافق مع الهاشات القديمة**: في حالة وجود هاشات قديمة (SHA256)، سيتم إرجاع false مع تسجيل تحذير

## ملاحظات مهمة

### الهاشات القديمة

إذا كان لديك روابط مشتركة موجودة بكلمات مرور مشفرة باستخدام SHA256، فإن التحقق منها سيفشل. ستحتاج إلى:

1. إعادة إنشاء الروابط المشتركة بكلمات مرور جديدة
2. أو إضافة منطق للتحقق من كلا التنسيقين (SHA256 و BCrypt) أثناء فترة الانتقال

### Work Factor

- **القيمة الحالية**: 12
- **المعنى**: عدد التكرارات (2^12 = 4096)
- **التوصية**: 
  - للإنتاج: 12-14
  - للأمان العالي: 14-16
  - ملاحظة: زيادة workFactor تزيد من وقت التشفير والتحقق

## الاختبار

بعد تثبيت الحزمة، اختبر الكود:

```csharp
// Test hashing
var password = "test123";
var hash = BCrypt.Net.BCrypt.HashPassword(password);
Console.WriteLine($"Hash: {hash}");

// Test verification
var isValid = BCrypt.Net.BCrypt.Verify(password, hash);
Console.WriteLine($"Is Valid: {isValid}"); // Should be True
```

## الدعم

إذا واجهت أي مشاكل:

1. تأكد من تثبيت الحزمة بشكل صحيح
2. تحقق من أن using statement موجود
3. تأكد من أن المشروع يعيد البناء بدون أخطاء
4. راجع سجلات الأخطاء (Logs) للتحذيرات

---

**آخر تحديث**: 2025

