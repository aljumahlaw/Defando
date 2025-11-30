# خطة Unit Testing الشاملة
## Comprehensive Unit Testing Plan

**التاريخ:** 2025  
**الإصدار:** 1.0

---

## 📋 نظرة عامة

هذه الخطة الشاملة لإنشاء Unit Tests لمشروع LegalDocSystem باستخدام xUnit و Moq.

---

## 🎯 الخدمات الحرجة المطلوب تغطيتها

### 1. **AuthService** (الأولوية: 🔴 عالية)
- ✅ LoginAsync (نجاح، فشل، حساب مقفل)
- ✅ LogoutAsync
- ✅ LoginWithResultAsync (مع Account Lockout)
- ✅ IsAuthenticatedAsync
- ✅ GetCurrentUserAsync

### 2. **DocumentService** (الأولوية: 🔴 عالية)
- ✅ CreateDocumentAsync
- ✅ UpdateDocumentAsync
- ✅ DeleteDocumentAsync
- ✅ GetDocumentByIdAsync
- ✅ SearchDocumentsAsync
- ✅ AdvancedSearchAsync
- ✅ CheckOutDocumentAsync
- ✅ CheckInDocumentAsync

### 3. **AuditService** (الأولوية: 🟡 متوسطة)
- ✅ LogEventAsync
- ✅ LogLoginAsync
- ✅ LogLogoutAsync
- ✅ LogCreateAsync
- ✅ LogUpdateAsync
- ✅ LogDeleteAsync
- ✅ GetLogsAsync

### 4. **UserService** (الأولوية: 🔴 عالية)
- ✅ CreateUserAsync
- ✅ GetUserByIdAsync
- ✅ GetUserByUsernameAsync
- ✅ ValidatePasswordAsync
- ✅ RecordFailedLoginAttemptAsync
- ✅ ResetFailedLoginAttemptsAsync
- ✅ IsAccountLockedAsync
- ✅ GetLockoutExpirationAsync

### 5. **EmailService** (الأولوية: 🟡 متوسطة)
- ✅ SendEmailAsync
- ✅ SendEmailWithRetryAsync
- ✅ SendTestEmailAsync
- ✅ ValidateSmtpSettingsAsync
- ✅ GetSmtpSettingsAsync
- ✅ SaveSmtpSettingsAsync

### 6. **EncryptionService** (الأولوية: 🟡 متوسطة)
- ✅ Encrypt
- ✅ Decrypt
- ✅ IsEncrypted

### 7. **SharedLinkService** (الأولوية: 🟢 منخفضة)
- ✅ CreateSharedLinkAsync
- ✅ ValidateSharedLinkAsync
- ✅ RecordAccessAsync
- ✅ DeleteLinkAsync

---

## 📦 المتطلبات والمكتبات

### Packages المطلوبة:

```xml
<ItemGroup>
  <!-- xUnit Testing Framework -->
  <PackageReference Include="xunit" Version="2.6.2" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
  
  <!-- Moq for Mocking -->
  <PackageReference Include="Moq" Version="4.20.70" />
  
  <!-- EF Core In-Memory Database for Testing -->
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
  
  <!-- FluentAssertions (Optional but recommended) -->
  <PackageReference Include="FluentAssertions" Version="6.12.0" />
</ItemGroup>
```

---

## 🏗️ هيكل مشروع الاختبارات

```
LegalDocSystem.Tests/
├── LegalDocSystem.Tests.csproj
├── Services/
│   ├── AuthServiceTests.cs
│   ├── DocumentServiceTests.cs
│   ├── AuditServiceTests.cs
│   ├── UserServiceTests.cs
│   ├── EmailServiceTests.cs
│   ├── EncryptionServiceTests.cs
│   └── SharedLinkServiceTests.cs
├── Helpers/
│   ├── TestDbContextFactory.cs
│   ├── TestDataBuilder.cs
│   └── MockHttpContextAccessor.cs
└── Integration/
    └── (Integration tests - optional)
```

---

## 📝 برومبتات إنشاء الاختبارات

### برومبت 1: إنشاء مشروع الاختبارات

```
أنشئ مشروع Unit Tests جديد باسم LegalDocSystem.Tests باستخدام xUnit.

المتطلبات:
1. إنشاء ملف .csproj جديد
2. إضافة Packages: xunit, Moq, Microsoft.EntityFrameworkCore.InMemory, FluentAssertions
3. إضافة Reference إلى مشروع LegalDocSystem الرئيسي
4. إنشاء هيكل المجلدات: Services/, Helpers/, Integration/

الملفات المطلوبة:
- LegalDocSystem.Tests.csproj
- README.md (شرح كيفية تشغيل الاختبارات)
```

---

### برومبت 2: إنشاء Helper Classes

```
أنشئ Helper Classes للاختبارات:

1. TestDbContextFactory.cs:
   - إنشاء In-Memory DbContext للاختبارات
   - Seed بيانات تجريبية
   - Cleanup بعد كل اختبار

2. TestDataBuilder.cs:
   - Builder pattern لإنشاء بيانات تجريبية
   - Methods: CreateUser(), CreateDocument(), CreateFolder(), etc.

3. MockHttpContextAccessor.cs:
   - Mock لـ IHttpContextAccessor
   - دعم Session و User Claims

الكود يجب أن يكون:
- Reusable عبر جميع الاختبارات
- Thread-safe
- Easy to use
```

---

### برومبت 3: اختبارات AuthService

```
أنشئ ملف AuthServiceTests.cs مع الاختبارات التالية:

1. LoginAsync_WithValidCredentials_ReturnsUser:
   - Mock IUserService لإرجاع user صحيح
   - Mock IHttpContextAccessor
   - Mock IAuditService
   - التحقق من نجاح LoginAsync
   - التحقق من استدعاء SignInAsync

2. LoginAsync_WithInvalidCredentials_ReturnsNull:
   - Mock IUserService لإرجاع null أو password خاطئ
   - التحقق من إرجاع null
   - التحقق من تسجيل failed login في AuditService

3. LoginAsync_WithLockedAccount_ReturnsNull:
   - Mock IUserService لإرجاع user مقفل
   - التحقق من إرجاع null
   - التحقق من تسجيل account lockout في AuditService

4. LoginWithResultAsync_WithLockedAccount_ReturnsLockoutInfo:
   - Mock IUserService لإرجاع user مقفل
   - التحقق من IsAccountLocked = true
   - التحقق من LockoutExpiration

5. LogoutAsync_ClearsSessionAndSignsOut:
   - Mock IHttpContextAccessor
   - Mock IAuditService
   - التحقق من استدعاء SignOutAsync
   - التحقق من تسجيل logout في AuditService

6. IsAuthenticatedAsync_WithAuthenticatedUser_ReturnsTrue:
   - Mock IHttpContextAccessor مع user authenticated
   - التحقق من إرجاع true

7. GetCurrentUserAsync_WithValidUserId_ReturnsUser:
   - Mock IHttpContextAccessor مع userId
   - Mock IUserService لإرجاع user
   - التحقق من إرجاع user صحيح

استخدم:
- xUnit [Fact] و [Theory]
- Moq للـ Mocking
- FluentAssertions للـ Assertions
```

---

### برومبت 4: اختبارات DocumentService

```
أنشئ ملف DocumentServiceTests.cs مع الاختبارات التالية:

1. CreateDocumentAsync_WithValidDocument_CreatesAndReturnsDocument:
   - استخدام In-Memory DbContext
   - Mock IOcrService, IFileStorageService, IAuditService
   - إنشاء document جديد
   - التحقق من حفظه في DbContext
   - التحقق من تسجيل Audit Log

2. CreateDocumentAsync_WithNullDocument_ThrowsException:
   - التحقق من throw ArgumentNullException

3. UpdateDocumentAsync_WithValidDocument_UpdatesDocument:
   - إنشاء document موجود
   - تحديث بياناته
   - التحقق من التحديث في DbContext
   - التحقق من تسجيل Audit Log

4. DeleteDocumentAsync_WithValidId_DeletesDocument:
   - إنشاء document موجود
   - حذفه
   - التحقق من عدم وجوده في DbContext
   - التحقق من تسجيل Audit Log

5. GetDocumentByIdAsync_WithValidId_ReturnsDocument:
   - إنشاء document موجود
   - جلب document بالـ ID
   - التحقق من إرجاع document صحيح

6. GetDocumentByIdAsync_WithInvalidId_ReturnsNull:
   - جلب document بـ ID غير موجود
   - التحقق من إرجاع null

7. SearchDocumentsAsync_WithQuery_ReturnsMatchingDocuments:
   - إنشاء documents متعددة
   - البحث بكلمة مفتاحية
   - التحقق من إرجاع documents مطابقة

8. CheckOutDocumentAsync_WithValidDocument_LocksDocument:
   - إنشاء document غير مقفل
   - Check-out
   - التحقق من LockedBy و LockedAt
   - التحقق من تسجيل Audit Log

9. CheckInDocumentAsync_WithValidDocument_UnlocksDocument:
   - إنشاء document مقفل
   - Check-in مع change description
   - التحقق من إزالة Lock
   - التحقق من إنشاء DocumentVersion
   - التحقق من تسجيل Audit Log

استخدم:
- In-Memory Database للـ DbContext
- Moq للـ Dependencies
- TestDataBuilder لإنشاء بيانات تجريبية
```

---

### برومبت 5: اختبارات AuditService

```
أنشئ ملف AuditServiceTests.cs مع الاختبارات التالية:

1. LogEventAsync_WithValidEntry_LogsToDatabase:
   - استخدام In-Memory DbContext
   - Mock IHttpContextAccessor
   - إنشاء AuditLogEntry
   - استدعاء LogEventAsync
   - التحقق من حفظ AuditLog في DbContext

2. LogEventAsync_WithHttpContext_ExtractsUserInfo:
   - Mock IHttpContextAccessor مع authenticated user
   - التحقق من استخراج UserId و Username تلقائياً

3. LogEventAsync_WithException_DoesNotThrow:
   - Mock DbContext لإلقاء exception
   - التحقق من عدم throw exception
   - التحقق من تسجيل error في Console (يمكن mock ILogger)

4. LogLoginAsync_WithSuccess_LogsSuccessEvent:
   - استدعاء LogLoginAsync مع success = true
   - التحقق من Action = "login_success"

5. LogLoginAsync_WithFailure_LogsFailureEvent:
   - استدعاء LogLoginAsync مع success = false
   - التحقق من Action = "login_failed"

6. LogLogoutAsync_LogsLogoutEvent:
   - استدعاء LogLogoutAsync
   - التحقق من Action = "logout"

7. LogCreateAsync_LogsCreateEvent:
   - استدعاء LogCreateAsync
   - التحقق من Action = "create_document"
   - التحقق من EntityType و EntityId

8. GetLogsAsync_WithFilters_ReturnsFilteredLogs:
   - إنشاء audit logs متعددة
   - Filter بـ category, action, userId, date range
   - التحقق من إرجاع logs مطابقة فقط

9. GetLogsAsync_WithPagination_ReturnsPaginatedResults:
   - إنشاء audit logs متعددة
   - Pagination (page, pageSize)
   - التحقق من إرجاع العدد الصحيح

ملاحظات:
- AuditService يستخدم async/await
- يجب اختبار معالجة الأخطاء (لا يجب أن تكسر التطبيق)
- يجب اختبار استخراج User Info من HttpContext
```

---

### برومبت 6: اختبارات UserService

```
أنشئ ملف UserServiceTests.cs مع الاختبارات التالية:

1. CreateUserAsync_WithValidUser_CreatesUser:
   - استخدام In-Memory DbContext
   - إنشاء user جديد
   - التحقق من حفظه في DbContext
   - التحقق من hash password (BCrypt)

2. CreateUserAsync_WithDuplicateUsername_ThrowsException:
   - إنشاء user موجود
   - التحقق من throw exception

3. GetUserByIdAsync_WithValidId_ReturnsUser:
   - إنشاء user موجود
   - جلب user بالـ ID
   - التحقق من إرجاع user صحيح

4. GetUserByUsernameAsync_WithValidUsername_ReturnsUser:
   - إنشاء user موجود
   - جلب user بالـ username
   - التحقق من إرجاع user صحيح

5. ValidatePasswordAsync_WithCorrectPassword_ReturnsTrue:
   - إنشاء user مع password معروف
   - التحقق من ValidatePasswordAsync = true

6. ValidatePasswordAsync_WithIncorrectPassword_ReturnsFalse:
   - إنشاء user مع password معروف
   - التحقق من ValidatePasswordAsync = false مع password خاطئ

7. RecordFailedLoginAttemptAsync_WithThreshold_LocksAccount:
   - إنشاء user مع FailedLoginAttempts = 4
   - استدعاء RecordFailedLoginAttemptAsync
   - التحقق من LockedUntil != null
   - التحقق من FailedLoginAttempts = 5

8. ResetFailedLoginAttemptsAsync_ResetsAttempts:
   - إنشاء user مع FailedLoginAttempts > 0
   - استدعاء ResetFailedLoginAttemptsAsync
   - التحقق من FailedLoginAttempts = 0
   - التحقق من LockedUntil = null

9. IsAccountLockedAsync_WithLockedAccount_ReturnsTrue:
   - إنشاء user مع LockedUntil في المستقبل
   - التحقق من IsAccountLockedAsync = true

10. IsAccountLockedAsync_WithExpiredLock_ReturnsFalse:
    - إنشاء user مع LockedUntil في الماضي
    - التحقق من IsAccountLockedAsync = false
    - التحقق من إزالة Lock تلقائياً

استخدم:
- BCrypt.Net-Next للتحقق من password hashing
- In-Memory Database
```

---

### برومبت 7: اختبارات EmailService

```
أنشئ ملف EmailServiceTests.cs مع الاختبارات التالية:

1. SendEmailAsync_WithValidSettings_SendsEmail:
   - Mock IEncryptionService
   - Mock ApplicationDbContext (لـ GetSmtpSettingsAsync)
   - Mock SmtpClient (صعب - قد نحتاج wrapper)
   - التحقق من استدعاء SendAsync

2. SendEmailAsync_WithRetryLogic_RetriesOnFailure:
   - Mock SendEmailAsync لإلقاء exception في المحاولة الأولى
   - Mock SendEmailAsync للنجاح في المحاولة الثانية
   - التحقق من Retry
   - التحقق من Logging

3. SendEmailWithRetryAsync_WithMaxRetries_FailsAfterMaxRetries:
   - Mock SendEmailAsync لإلقاء exception دائماً
   - maxRetries = 3
   - التحقق من 3 محاولات
   - التحقق من return false

4. ValidateSmtpSettingsAsync_WithValidSettings_ReturnsTrue:
   - Mock GetSmtpSettingsAsync لإرجاع settings صحيحة
   - Mock SmtpClient للنجاح في ConnectAsync و AuthenticateAsync
   - التحقق من return true

5. ValidateSmtpSettingsAsync_WithInvalidSettings_ReturnsFalse:
   - Mock GetSmtpSettingsAsync لإرجاع settings غير صحيحة
   - Mock SmtpClient للفشل في ConnectAsync
   - التحقق من return false

6. GetSmtpSettingsAsync_ReturnsCachedSettings:
   - Mock ApplicationDbContext
   - استدعاء GetSmtpSettingsAsync مرتين
   - التحقق من استدعاء DbContext مرة واحدة فقط (caching)

7. SaveSmtpSettingsAsync_EncryptsPassword:
   - Mock IEncryptionService
   - استدعاء SaveSmtpSettingsAsync
   - التحقق من استدعاء Encrypt على password

ملاحظات:
- MailKit SmtpClient صعب Mock - قد نحتاج wrapper interface
- يمكن استخدام Integration Tests بدلاً من Unit Tests للإيميلات
```

---

### برومبت 8: اختبارات EncryptionService

```
أنشئ ملف EncryptionServiceTests.cs مع الاختبارات التالية:

1. Encrypt_WithPlainText_ReturnsEncryptedString:
   - استدعاء Encrypt مع plain text
   - التحقق من return string غير فارغ
   - التحقق من string يبدأ بـ "DPAPI:"

2. Decrypt_WithEncryptedString_ReturnsPlainText:
   - Encrypt plain text
   - Decrypt النتيجة
   - التحقق من return نفس plain text

3. Encrypt_Decrypt_RoundTrip_ReturnsOriginalText:
   - Encrypt → Decrypt
   - التحقق من النتيجة = النص الأصلي

4. Decrypt_WithLegacyBase64_ReturnsPlainText:
   - Decrypt string مشفر بـ Base64 (legacy)
   - التحقق من return plain text صحيح

5. IsEncrypted_WithEncryptedString_ReturnsTrue:
   - Encrypt plain text
   - التحقق من IsEncrypted = true

6. IsEncrypted_WithPlainText_ReturnsFalse:
   - التحقق من IsEncrypted = false مع plain text

7. Encrypt_WithNull_ThrowsArgumentNullException:
   - التحقق من throw ArgumentNullException

8. Decrypt_WithInvalidFormat_ThrowsCryptographicException:
   - Decrypt string غير صحيح
   - التحقق من throw CryptographicException

ملاحظات:
- على Windows: يستخدم DPAPI
- على Linux/macOS: يستخدم AES
- يجب اختبار كلا الحالتين
```

---

## 🧪 أمثلة عملية على اختبارات الوحدة

### مثال كامل: AuthServiceTests.cs

هذا المثال يوضح كيفية كتابة Unit Tests كاملة باستخدام xUnit و Moq و FluentAssertions:

```csharp
using LegalDocSystem.Data;
using LegalDocSystem.Models;
using LegalDocSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;
using FluentAssertions;

namespace LegalDocSystem.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly AuthService _authService;
    private readonly HttpContext _httpContext;

    public AuthServiceTests()
    {
        // Setup In-Memory Database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup Mocks
        _mockUserService = new Mock<IUserService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockAuditService = new Mock<IAuditService>();

        // Setup HttpContext
        _httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContext);

        // Create AuthService instance
        _authService = new AuthService(
            _mockUserService.Object,
            _mockHttpContextAccessor.Object,
            _mockAuditService.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsUser()
    {
        // Arrange
        var username = "testuser";
        var password = "TestPassword123";
        var user = new User
        {
            UserId = 1,
            Username = username,
            FullName = "Test User",
            Email = "test@example.com",
            Role = "user",
            IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        _mockUserService
            .Setup(x => x.GetUserByUsernameAsync(username))
            .ReturnsAsync(user);

        _mockUserService
            .Setup(x => x.IsAccountLockedAsync(username))
            .ReturnsAsync(false);

        _mockUserService
            .Setup(x => x.ValidatePasswordAsync(username, password))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be(username);
        result.UserId.Should().Be(user.UserId);

        // Verify Audit Log was called
        _mockAuditService.Verify(
            x => x.LogLoginAsync(
                user.UserId,
                username,
                true,
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var username = "testuser";
        var password = "WrongPassword";
        var user = new User
        {
            UserId = 1,
            Username = username,
            IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword")
        };

        _mockUserService
            .Setup(x => x.GetUserByUsernameAsync(username))
            .ReturnsAsync(user);

        _mockUserService
            .Setup(x => x.ValidatePasswordAsync(username, password))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        result.Should().BeNull();

        // Verify Audit Log was called for failed login
        _mockAuditService.Verify(
            x => x.LogLoginAsync(
                user.UserId,
                username,
                false,
                It.Is<string>(s => s.Contains("Invalid password"))),
            Times.Once);
    }
}
```

### نمط Arrange-Act-Assert (AAA)

كل اختبار يجب أن يتبع هذا النمط:

1. **Arrange (الإعداد):** إعداد البيانات والـ Mocks
2. **Act (التنفيذ):** تنفيذ الوظيفة المطلوب اختبارها
3. **Assert (التحقق):** التحقق من النتائج

### استخدام FluentAssertions

```csharp
// بدلاً من:
Assert.NotNull(result);
Assert.Equal(username, result.Username);

// استخدم:
result.Should().NotBeNull();
result.Username.Should().Be(username);
```

### استخدام Moq للـ Mocking

```csharp
// Setup Mock
_mockUserService
    .Setup(x => x.GetUserByUsernameAsync(username))
    .ReturnsAsync(user);

// Verify Mock was called
_mockUserService.Verify(
    x => x.GetUserByUsernameAsync(username),
    Times.Once);
```

### Helper Methods (اختياري)

```csharp
private User CreateTestUser(int userId = 1, string username = "testuser")
{
    return new User
    {
        UserId = userId,
        Username = username,
        FullName = "Test User",
        Email = "test@example.com",
        Role = "user",
        IsActive = true,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123")
    };
}
```

---

## 📊 خطة تنفيذية

### المرحلة 1: الإعداد (أسبوع 1)
- [ ] إنشاء مشروع LegalDocSystem.Tests
- [ ] إضافة Packages المطلوبة
- [ ] إنشاء Helper Classes (TestDbContextFactory, TestDataBuilder, MockHttpContextAccessor)
- [ ] إعداد CI/CD pipeline

### المرحلة 2: الاختبارات الحرجة (أسبوع 2-3)
- [ ] AuthServiceTests (أولوية عالية)
- [ ] UserServiceTests (أولوية عالية)
- [ ] DocumentServiceTests (أولوية عالية)

### المرحلة 3: الاختبارات المتوسطة (أسبوع 4)
- [ ] AuditServiceTests
- [ ] EmailServiceTests
- [ ] EncryptionServiceTests

### المرحلة 4: الاختبارات الإضافية (أسبوع 5)
- [ ] SharedLinkServiceTests
- [ ] FolderServiceTests
- [ ] TaskServiceTests

### المرحلة 5: التغطية والتحسين (أسبوع 6)
- [ ] قياس التغطية
- [ ] إضافة اختبارات للفجوات
- [ ] تحسين الاختبارات الموجودة

---

## 🔄 دمج CI/CD

### GitHub Actions Example:

```yaml
name: Unit Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
    
    - name: Upload coverage
      uses: codecov/codecov-action@v3
      with:
        files: '**/coverage.cobertura.xml'
```

---

## 📈 قياس التغطية (Code Coverage) وأدواتها

### معايير التغطية

#### الحد الأدنى:
- **الخدمات الحرجة:** 90%+
- **الخدمات المتوسطة:** 80%+
- **الخدمات الأخرى:** 70%+
- **المشروع ككل:** 75%+

#### الخدمات الحرجة:
1. **AuthService** - 90%+
2. **UserService** - 90%+
3. **DocumentService** - 90%+
4. **AuditService** - 85%+

#### الخدمات المتوسطة:
1. **EmailService** - 80%+
2. **EncryptionService** - 80%+
3. **SharedLinkService** - 80%+

---

### أدوات قياس التغطية

#### 1. Coverlet (مدمج)

**التثبيت:**
```bash
dotnet add package coverlet.msbuild
```

**الاستخدام:**
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

#### 2. ReportGenerator

**التثبيت:**
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

**الاستخدام:**
```bash
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage" -reporttypes:Html
```

#### 3. Codecov (للتحليل المستمر)

- إضافة إلى GitHub Actions
- أو استخدام Codecov CLI

---

### إعداد قياس التغطية

#### 1. تحديث .csproj

```xml
<ItemGroup>
  <PackageReference Include="coverlet.msbuild" Version="6.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

#### 2. تشغيل الاختبارات مع التغطية

```bash
# تشغيل الاختبارات مع قياس التغطية
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./coverage/

# تشغيل مع تفاصيل أكثر
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./coverage/ /p:Threshold=75 /p:ThresholdType=line
```

#### 3. إنشاء تقرير HTML

```bash
# تثبيت ReportGenerator
dotnet tool install -g dotnet-reportgenerator-globaltool

# إنشاء تقرير
reportgenerator -reports:"coverage/coverage.cobertura.xml" -targetdir:"coverage/report" -reporttypes:Html
```

---

### قراءة تقارير التغطية

#### تقرير HTML

افتح `coverage/report/index.html` في المتصفح.

**المعلومات المتوفرة:**
- تغطية المشروع ككل
- تغطية كل ملف
- تغطية كل method
- الأسطر المغطاة وغير المغطاة

---

### استراتيجية تحسين التغطية

#### 1. تحديد الفجوات

**الأدوات:**
- تقرير HTML
- Visual Studio Code Coverage
- JetBrains dotCover

**الخطوات:**
1. تشغيل الاختبارات مع التغطية
2. فحص التقارير
3. تحديد الأسطر/الـ Methods غير المغطاة
4. إضافة اختبارات للفجوات

#### 2. إضافة اختبارات للفجوات

**أمثلة:**
- Edge cases
- Error handling
- Null checks
- Boundary conditions

#### 3. إزالة الكود غير المستخدم

**الأدوات:**
- Visual Studio Code Analysis
- SonarQube

---

### دمج CI/CD

#### GitHub Actions Example:

```yaml
name: Unit Tests with Coverage

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test with Coverage
      run: |
        dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./coverage/
    
    - name: Generate Coverage Report
      run: |
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator -reports:"coverage/coverage.cobertura.xml" -targetdir:"coverage/report" -reporttypes:Html
    
    - name: Upload Coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        files: 'coverage/coverage.cobertura.xml'
        flags: unittests
        name: codecov-umbrella
```

---

### تتبع التغطية

#### 1. Codecov Dashboard

**الميزات:**
- تتبع التغطية عبر الوقت
- Pull Request comments
- Coverage badges

#### 2. SonarQube

**الميزات:**
- تحليل شامل للكود
- تتبع التغطية
- Code smells detection

---

### ⚠️ ملاحظات مهمة

#### 1. التغطية ليست كل شيء

**تذكر:**
- ✅ التغطية العالية لا تعني جودة عالية
- ✅ اختبارات جيدة > تغطية عالية
- ✅ ركز على اختبار الحالات الحرجة

#### 2. اختبار الحالات الحرجة

**أولويات:**
1. **Authentication/Authorization** - حرج جداً
2. **Data Validation** - حرج
3. **Error Handling** - مهم
4. **Business Logic** - مهم

#### 3. تجنب Over-Testing

**لا تختبر:**
- Framework code (EF Core, ASP.NET Core)
- Third-party libraries
- Simple getters/setters

---

### 📋 Checklist

#### للتطوير:

- [ ] ✅ إضافة coverlet.msbuild إلى .csproj
- [ ] ✅ تشغيل الاختبارات مع التغطية
- [ ] ✅ إنشاء تقرير HTML
- [ ] ✅ تحديد الفجوات
- [ ] ✅ إضافة اختبارات للفجوات

#### للإنتاج:

- [ ] ⚠️ إعداد CI/CD pipeline
- [ ] ⚠️ ربط Codecov أو SonarQube
- [ ] ⚠️ تعيين معايير التغطية
- [ ] ⚠️ مراقبة التغطية عبر الوقت

---

## 📚 المراجع

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [EF Core In-Memory](https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database)
- [FluentAssertions](https://fluentassertions.com/)

---

**آخر تحديث:** 2025  
**الحالة:** ✅ جاهز للتنفيذ

