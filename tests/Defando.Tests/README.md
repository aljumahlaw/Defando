# LegalDocSystem.Tests

مشروع Unit Tests لمشروع LegalDocSystem.

## 📋 المتطلبات

- .NET 8.0 SDK
- Visual Studio 2022 أو Visual Studio Code
- Reference إلى مشروع `LegalDocSystem` الرئيسي

---

## 🚀 تشغيل الاختبارات

### من Command Line:

```bash
# تشغيل جميع الاختبارات
dotnet test

# تشغيل مع تفاصيل أكثر
dotnet test --verbosity normal

# تشغيل اختبار محدد
dotnet test --filter "FullyQualifiedName~AuthServiceTests"

# تشغيل مع قياس التغطية
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### من Visual Studio:

1. افتح Test Explorer (Test → Test Explorer)
2. اضغط "Run All Tests" أو اختر اختبارات محددة
3. راجع النتائج في Test Explorer

---

## 📊 قياس التغطية

### تثبيت ReportGenerator:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

### تشغيل الاختبارات مع التغطية:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./coverage/
```

### إنشاء تقرير HTML:

```bash
reportgenerator -reports:"coverage/coverage.cobertura.xml" -targetdir:"coverage/report" -reporttypes:Html
```

---

## 🏗️ الهيكل

```
LegalDocSystem.Tests/
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

## 📝 أمثلة

### مثال على اختبار:

```csharp
[Fact]
public async Task LoginAsync_WithValidCredentials_ReturnsUser()
{
    // Arrange
    var username = "testuser";
    var password = "TestPassword123";
    var user = TestDataBuilder.CreateUser(username: username, password: password);

    _mockUserService
        .Setup(x => x.GetUserByUsernameAsync(username))
        .ReturnsAsync(user);

    // Act
    var result = await _authService.LoginAsync(username, password);

    // Assert
    result.Should().NotBeNull();
    result!.Username.Should().Be(username);
}
```

---

## 🔧 Helper Classes

### TestDbContextFactory:
- إنشاء In-Memory DbContext
- Seed بيانات تجريبية

### TestDataBuilder:
- Builder pattern لإنشاء بيانات تجريبية
- Methods: `CreateUser()`, `CreateDocument()`, etc.

### MockHttpContextAccessor:
- Mock لـ IHttpContextAccessor
- دعم Session و User Claims

---

## 📚 المراجع

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [FluentAssertions](https://fluentassertions.com/)

---

**آخر تحديث:** 2025

