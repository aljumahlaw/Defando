# دليل أمان البريد الإلكتروني
## Email Security Guide

**التاريخ:** 2025  
**الإصدار:** 1.0

---

## 📋 نظرة عامة

هذا الدليل يشرح كيفية إعداد واستخدام نظام البريد الإلكتروني الآمن في LegalDocSystem، بما في ذلك:
- تثبيت وإعداد MailKit
- تشفير كلمات مرور SMTP باستخدام DPAPI
- Retry Logic ومعالجة الأخطاء
- اختبار الإيميلات

---

## 📦 1. تثبيت MailKit

### 1.1 إضافة Package

تم إضافة MailKit إلى `src/LegalDocSystem.csproj`:

```xml
<PackageReference Include="MailKit" Version="4.3.0" />
```

### 1.2 تثبيت Package

```bash
cd src
dotnet restore
```

أو:

```bash
dotnet add package MailKit --version 4.3.0
```

---

## 🔐 2. تشفير كلمات المرور

### 2.1 نظرة عامة

تم استبدال التشفير غير الآمن (Base64) بنظام تشفير آمن يستخدم:
- **Windows DPAPI** (Data Protection API) على Windows
- **AES Encryption** على Linux/macOS

### 2.2 خدمة التشفير (EncryptionService)

**الملف:** `src/Services/EncryptionService.cs`

**الميزات:**
- ✅ تشفير آمن باستخدام Windows DPAPI (CurrentUser scope)
- ✅ دعم AES encryption على Linux/macOS
- ✅ دعم Legacy Base64 (للمهاجرة من النظام القديم)
- ✅ معالجة أخطاء شاملة

**الاستخدام:**

```csharp
// في EmailService
private readonly IEncryptionService _encryptionService;

// تشفير كلمة المرور
string encrypted = _encryptionService.Encrypt("MyPassword123");

// فك التشفير
string decrypted = _encryptionService.Decrypt(encrypted);
```

---

### 2.3 كيفية عمل DPAPI

**Windows DPAPI (Data Protection API):**
- يستخدم مفتاح مشفر خاص بالمستخدم الحالي
- لا يحتاج إلى إدارة مفاتيح يدوية
- آمن ومدمج في Windows
- **Scope:** `CurrentUser` (فقط المستخدم الحالي يمكنه فك التشفير)

**مثال:**

```csharp
// التشفير
byte[] encrypted = ProtectedData.Protect(
    plainBytes,
    null, // Optional entropy
    DataProtectionScope.CurrentUser);

// فك التشفير
byte[] decrypted = ProtectedData.Unprotect(
    encrypted,
    null,
    DataProtectionScope.CurrentUser);
```

---

### 2.4 Migration من Base64

**دعم Legacy Base64:**
- النظام يدعم فك تشفير كلمات المرور القديمة (Base64)
- عند حفظ كلمة مرور جديدة، يتم تشفيرها باستخدام DPAPI
- **توصية:** إعادة تشفير جميع كلمات المرور القديمة

**كيفية Migration:**

```csharp
// 1. قراءة كلمة المرور القديمة (Base64)
string oldPassword = GetOldPasswordFromDatabase();

// 2. فك التشفير (يدعم Base64 تلقائياً)
string plainPassword = _encryptionService.Decrypt(oldPassword);

// 3. إعادة التشفير باستخدام DPAPI
string newEncryptedPassword = _encryptionService.Encrypt(plainPassword);

// 4. حفظ في قاعدة البيانات
SavePasswordToDatabase(newEncryptedPassword);
```

---

## 📧 3. إعداد MailKit في EmailService

### 3.1 التعديلات المنفذة

**الملف:** `src/Services/EmailService.cs`

**التغييرات:**
- ✅ إضافة `using MailKit.Net.Smtp;`
- ✅ إضافة `using MailKit.Security;`
- ✅ إضافة `using MimeKit;`
- ✅ إزالة TODO comments
- ✅ تفعيل كود MailKit الكامل

---

### 3.2 إرسال الإيميل

**الكود الكامل:**

```csharp
public async Task<bool> SendEmailAsync(
    string to,
    string subject,
    string body,
    bool isHtml = true,
    List<string>? attachments = null,
    string? cc = null,
    string? bcc = null)
{
    var settings = await GetSmtpSettingsAsync();
    
    // Create MIME message
    using var message = new MimeMessage();
    message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
    message.To.Add(MailboxAddress.Parse(to));
    
    if (!string.IsNullOrEmpty(cc))
        message.Cc.Add(MailboxAddress.Parse(cc));
    
    if (!string.IsNullOrEmpty(bcc))
        message.Bcc.Add(MailboxAddress.Parse(bcc));
    
    message.Subject = subject;

    // Build message body
    var bodyBuilder = new BodyBuilder();
    if (isHtml)
        bodyBuilder.HtmlBody = body;
    else
        bodyBuilder.TextBody = body;

    // Add attachments
    if (attachments != null && attachments.Any())
    {
        foreach (var attachmentPath in attachments)
        {
            if (File.Exists(attachmentPath))
                bodyBuilder.Attachments.Add(attachmentPath);
        }
    }

    message.Body = bodyBuilder.ToMessageBody();

    // Connect and send
    using var client = new SmtpClient();
    var secureSocketOptions = settings.UseSsl 
        ? SecureSocketOptions.StartTls 
        : SecureSocketOptions.None;
    
    await client.ConnectAsync(settings.Host, settings.Port, secureSocketOptions);
    
    // Authenticate with decrypted password
    string decryptedPassword = _encryptionService.Decrypt(settings.Password);
    await client.AuthenticateAsync(settings.Username, decryptedPassword);
    
    await client.SendAsync(message);
    await client.DisconnectAsync(true);
    
    return true;
}
```

---

### 3.3 معالجة الأخطاء

**أنواع الأخطاء:**

1. **SmtpCommandException:**
   - خطأ في أمر SMTP (مثل: authentication failed)
   - يحتوي على `StatusCode` للتفاصيل

2. **SmtpProtocolException:**
   - خطأ في بروتوكول SMTP
   - مشاكل في الاتصال أو البروتوكول

3. **Exception (عام):**
   - أخطاء أخرى (مثل: network errors)

**الكود:**

```csharp
try
{
    // Send email
}
catch (SmtpCommandException ex)
{
    _logger.LogError(ex, $"SMTP command error: {ex.Message} (Status: {ex.StatusCode})");
    return false;
}
catch (SmtpProtocolException ex)
{
    _logger.LogError(ex, $"SMTP protocol error: {ex.Message}");
    return false;
}
catch (Exception ex)
{
    _logger.LogError(ex, $"Failed to send email: {ex.Message}");
    return false;
}
```

---

## 🔄 4. Retry Logic

### 4.1 SendEmailWithRetryAsync

**الوظيفة:** `SendEmailWithRetryAsync`

**الميزات:**
- ✅ إعادة المحاولة تلقائياً عند الفشل
- ✅ تأخير بين المحاولات (configurable)
- ✅ عدد محاولات قابل للتخصيص
- ✅ Logging مفصل لكل محاولة

**الاستخدام:**

```csharp
bool success = await _emailService.SendEmailWithRetryAsync(
    to: "user@example.com",
    subject: "Test Email",
    body: "<h1>Hello</h1>",
    isHtml: true,
    maxRetries: 3,      // 3 محاولات
    delaySeconds: 5);   // 5 ثواني بين المحاولات
```

**الكود:**

```csharp
public async Task<bool> SendEmailWithRetryAsync(
    string to,
    string subject,
    string body,
    bool isHtml = true,
    int maxRetries = 3,
    int delaySeconds = 5)
{
    int attempt = 0;
    Exception? lastException = null;

    while (attempt < maxRetries)
    {
        attempt++;
        _logger.LogInformation($"Attempt {attempt}/{maxRetries} to send email to {to}");

        try
        {
            var result = await SendEmailAsync(to, subject, body, isHtml);
            if (result)
            {
                _logger.LogInformation($"Email sent successfully on attempt {attempt}");
                return true;
            }
        }
        catch (Exception ex)
        {
            lastException = ex;
            _logger.LogWarning(ex, $"Failed on attempt {attempt}/{maxRetries}");
        }

        // Wait before retrying (except on last attempt)
        if (attempt < maxRetries)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }

    _logger.LogError(lastException, $"Failed after {maxRetries} attempts");
    return false;
}
```

---

### 4.2 إعدادات Retry من appsettings.json

**الملف:** `src/appsettings.json`

```json
{
  "EmailNotifications": {
    "RetryAttempts": 3,
    "RetryDelaySeconds": 5
  }
}
```

**الاستخدام:**

```csharp
var retryAttempts = builder.Configuration.GetValue<int>("EmailNotifications:RetryAttempts", 3);
var retryDelay = builder.Configuration.GetValue<int>("EmailNotifications:RetryDelaySeconds", 5);
```

---

## 🧪 5. اختبار الإيميلات

### 5.1 اختبار إرسال إيميل

**الطريقة 1: من Blazor UI**

1. اذهب إلى `/smtp-settings`
2. أدخل إعدادات SMTP
3. اضغط "إرسال إيميل اختبار"

**الطريقة 2: من الكود**

```csharp
var emailService = serviceProvider.GetRequiredService<IEmailService>();

bool success = await emailService.SendTestEmailAsync("your-email@example.com");

if (success)
{
    Console.WriteLine("Test email sent successfully!");
}
else
{
    Console.WriteLine("Test email failed!");
}
```

---

### 5.2 اختبار التحقق من إعدادات SMTP

```csharp
var emailService = serviceProvider.GetRequiredService<IEmailService>();

bool isValid = await emailService.ValidateSmtpSettingsAsync();

if (isValid)
{
    Console.WriteLine("SMTP settings are valid!");
}
else
{
    Console.WriteLine("SMTP settings are invalid!");
}
```

---

### 5.3 اختبار التشفير

```csharp
var encryptionService = serviceProvider.GetRequiredService<IEncryptionService>();

// Test encryption
string plainText = "MyPassword123";
string encrypted = encryptionService.Encrypt(plainText);
Console.WriteLine($"Encrypted: {encrypted}");

// Test decryption
string decrypted = encryptionService.Decrypt(encrypted);
Console.WriteLine($"Decrypted: {decrypted}");

// Verify
bool isMatch = plainText == decrypted;
Console.WriteLine($"Match: {isMatch}"); // Should be true
```

---

## ⚠️ 6. تحذيرات ومتطلبات

### 6.1 Windows DPAPI

**المتطلبات:**
- ✅ يعمل فقط على Windows
- ✅ يحتاج إلى CurrentUser permissions
- ✅ البيانات مشفرة للمستخدم الحالي فقط

**على Linux/macOS:**
- ✅ يستخدم AES encryption تلقائياً
- ⚠️ المفتاح مشتق من MachineName (ليس آمن تماماً)
- **توصية:** استخدام Azure Key Vault في الإنتاج

---

### 6.2 Azure Key Vault (مستقبلاً)

**للإنتاج على Linux/macOS أو Azure:**

```csharp
// Example: Using Azure Key Vault
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;

var client = new SecretClient(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());

// Encrypt
await client.SetSecretAsync("SmtpPassword", plainPassword);

// Decrypt
KeyVaultSecret secret = await client.GetSecretAsync("SmtpPassword");
string decrypted = secret.Value;
```

---

### 6.3 أمان إضافي

**توصيات:**
- ✅ استخدم HTTPS دائماً للاتصال بـ SMTP
- ✅ استخدم `SecureSocketOptions.StartTls`
- ✅ لا تخزن كلمات المرور في Logs
- ✅ استخدم Environment Variables أو User Secrets للإعدادات الحساسة

---

## 📋 7. Checklist

### للتطوير:

- [x] ✅ إضافة MailKit إلى `.csproj`
- [x] ✅ إنشاء `EncryptionService`
- [x] ✅ تعديل `EmailService` لاستخدام MailKit
- [x] ✅ إضافة Retry Logic
- [x] ✅ تحسين معالجة الأخطاء
- [ ] ⚠️ **تنفيذ:** `dotnet restore` (يجب تنفيذه يدوياً)
- [ ] ⚠️ **اختبار:** إرسال إيميل اختبار

---

### للإنتاج:

- [ ] ⚠️ **التحقق:** التأكد من عمل DPAPI على Windows
- [ ] ⚠️ **التحقق:** على Linux/macOS، النظر في Azure Key Vault
- [ ] ⚠️ **الأمان:** التأكد من استخدام HTTPS للاتصال بـ SMTP
- [ ] ⚠️ **المراقبة:** مراقبة EmailLogs للأخطاء

---

## 📚 8. المراجع

- [MailKit Documentation](https://github.com/jstedfast/MailKit)
- [Windows DPAPI](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata)
- [Azure Key Vault](https://learn.microsoft.com/en-us/azure/key-vault/)

---

**آخر تحديث:** 2025  
**الحالة:** ✅ جاهز للاستخدام

