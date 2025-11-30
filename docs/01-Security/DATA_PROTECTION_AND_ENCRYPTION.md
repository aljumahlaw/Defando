# حماية البيانات والتشفير
## Data Protection and Encryption

**التاريخ:** 2025  
**الإصدار:** 2.0  
**الحالة:** ✅ **مكتمل**

---

## 📋 الملخص التنفيذي

هذا الملف يغطي جميع جوانب حماية البيانات والتشفير في مشروع LegalDocSystem، بما في ذلك:

- Password Hashing (BCrypt)
- Data Encryption (DPAPI/AES)
- Email Security
- Connection String Security
- Sensitive Data Storage

**الحالة:** ✅ جميع الآليات مطبقة بشكل صحيح وآمن

---

## 1. Password Hashing (BCrypt)

### 1.1 التطبيق

**الموقع:** `src/Services/UserService.cs`

#### Hashing

```csharp
user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
```

#### Verification

```csharp
return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
```

**النتيجة:** ✅ **محمي** - Passwords مشفرة باستخدام BCrypt

---

## 2. Data Encryption (DPAPI/AES)

### 2.1 EncryptionService

**الموقع:** `src/Services/EncryptionService.cs`

#### Windows (DPAPI)

```csharp
if (OperatingSystem.IsWindows())
{
    var scope = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production"
        ? DataProtectionScope.LocalMachine  // Shared across users (production)
        : DataProtectionScope.CurrentUser;  // User-specific (development)
    
    encryptedBytes = ProtectedData.Protect(
        plainBytes,
        null,
        scope);
}
```

#### Non-Windows (AES)

```csharp
// Get encryption key from environment variable or configuration
var keyString = configuration["Encryption:Key"] 
    ?? Environment.GetEnvironmentVariable("LEGALDOC_ENCRYPTION_KEY");

if (!string.IsNullOrEmpty(keyString))
{
    using (var sha256 = SHA256.Create())
    {
        _aesKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
    }
}
```

### 2.2 AES Key Management

**الاستخدام:**

```bash
# Development
# في appsettings.Development.json
{
  "Encryption": {
    "Key": "YourDevelopmentKey32CharactersLong!"
  }
}

# Production
export LEGALDOC_ENCRYPTION_KEY="YourSecureKey32CharactersLong!"
```

**النتيجة:** ✅ **محمي** - AES Key آمن (Environment Variables)

---

## 3. Email Security

### 3.1 MailKit Integration

**الموقع:** `src/Services/EmailService.cs`

```csharp
using MailKit.Net.Smtp;
using MimeKit;

var message = new MimeMessage();
message.From.Add(new MailboxAddress("LegalDocSystem", "noreply@legaldocsystem.com"));
message.To.Add(new MailboxAddress("User", "user@example.com"));
message.Subject = "Test Email";

var body = new TextPart("html")
{
    Text = "<h1>Test Email</h1><p>This is a test email."
};
message.Body = body;

using (var client = new SmtpClient())
{
    await client.ConnectAsync("smtp.example.com", 587, SecureSocketOptions.StartTls);
    await client.AuthenticateAsync("username", decryptedPassword);
    await client.SendAsync(message);
    await client.DisconnectAsync(true);
}
```

### 3.2 SMTP Password Encryption

**الموقع:** `src/Services/EmailService.cs`

```csharp
// Encrypt password
var encrypted = _encryptionService.Encrypt(smtpPassword);
// Store encrypted password in database

// Decrypt password
var decrypted = _encryptionService.Decrypt(encryptedPassword);
// Use decrypted password for SMTP connection
```

### 3.3 Retry Logic

```csharp
const int maxRetries = 3;
const int delaySeconds = 5;

for (int attempt = 1; attempt <= maxRetries; attempt++)
{
    try
    {
        await client.SendAsync(message);
        return; // Success
    }
    catch (Exception ex)
    {
        if (attempt == maxRetries)
            throw; // Last attempt failed
        
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
    }
}
```

**النتيجة:** ✅ **محمي** - Email Security مطبق بشكل صحيح

---

## 4. Connection String Security

### 4.1 المشكلة

❌ كلمة مرور قاعدة البيانات موجودة في `appsettings.json` (مكشوفة في Git)

### 4.2 الحل المنفذ

**الموقع:** `src/Program.cs`

```csharp
// Get connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Override password from environment variable or User Secrets
var dbPassword = builder.Configuration["Database:Password"]
    ?? Environment.GetEnvironmentVariable("LEGALDOC_DB_PASSWORD")
    ?? throw new InvalidOperationException(
        "Database password not configured. Set LEGALDOC_DB_PASSWORD environment variable or use User Secrets.");

// Replace password placeholder in connection string
if (connectionString.Contains("Password=;") || connectionString.Contains("Password=YOUR_PASSWORD_HERE"))
{
    connectionString = connectionString.Replace("Password=;", $"Password={dbPassword};")
                                      .Replace("Password=YOUR_PASSWORD_HERE;", $"Password={dbPassword};");
}
```

### 4.3 الاستخدام

**Development (User Secrets):**

```bash
cd src
dotnet user-secrets set "Database:Password" "YourPassword"
```

**Production (Environment Variables):**

```bash
export LEGALDOC_DB_PASSWORD="YourPassword"
```

**النتيجة:** ✅ **محمي** - Connection String Password آمنة

---

## 5. Sensitive Data Storage

### 5.1 Passwords

- ✅ **User Passwords:** BCrypt hashed
- ✅ **Shared Link Passwords:** BCrypt hashed
- ✅ **SMTP Passwords:** Encrypted (DPAPI/AES)

### 5.2 Encryption Keys

- ✅ **AES Key:** Environment Variables
- ✅ **DPAPI:** Windows Data Protection API

**النتيجة:** ✅ **محمي** - جميع البيانات الحساسة مشفرة

---

## 📋 Checklist

### Password Security

- [x] ✅ BCrypt Hashing مطبق
- [x] ✅ Password Verification صحيح
- [x] ✅ No Plain Text Passwords

### Data Encryption

- [x] ✅ DPAPI (Windows) مطبق
- [x] ✅ AES (Non-Windows) مطبق
- [x] ✅ AES Key Management آمن
- [x] ✅ DPAPI Scope محسّن (LocalMachine للإنتاج)

### Email Security

- [x] ✅ MailKit مستخدم
- [x] ✅ SMTP Password Encrypted
- [x] ✅ Retry Logic موجود

### Connection String Security

- [x] ✅ Password محذوف من appsettings.json
- [x] ✅ User Secrets (Development)
- [x] ✅ Environment Variables (Production)

---

## 📚 المراجع

- [SECURITY_OVERVIEW.md](./SECURITY_OVERVIEW.md) - نظرة عامة على الأمان
- [SECURE_CONFIGURATION_AND_INFRASTRUCTURE.md](./SECURE_CONFIGURATION_AND_INFRASTRUCTURE.md) - الإعدادات الآمنة

---

**آخر تحديث:** 2025  
**الحالة:** ✅ **مكتمل وآمن**

