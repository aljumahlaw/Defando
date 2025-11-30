# Integration Tests for LegalDocSystem

## نظرة عامة

هذا المجلد يحتوي على اختبارات التكامل (Integration Tests) لمشروع LegalDocSystem.

---

## المتطلبات

### الأدوات المطلوبة:

1. **Selenium WebDriver**
   ```bash
   dotnet add package Selenium.WebDriver
   dotnet add package Selenium.WebDriver.ChromeDriver
   ```

2. **xUnit**
   ```bash
   dotnet add package xunit
   dotnet add package xunit.runner.visualstudio
   ```

3. **ChromeDriver**
   - يجب تثبيت Chrome Browser
   - ChromeDriver سيتم تحميله تلقائياً

---

## تشغيل الاختبارات

### تشغيل جميع الاختبارات:

```bash
cd tests/Integration
dotnet test
```

### تشغيل اختبارات محددة:

```bash
# UI Tests only
dotnet test --filter "FullyQualifiedName~UITests"

# API Tests only
dotnet test --filter "FullyQualifiedName~APITests"
```

---

## إعداد البيئة

### 1. تشغيل التطبيق:

```bash
cd src
dotnet run
```

### 2. التأكد من الاتصال بقاعدة البيانات:

- يجب أن تكون قاعدة البيانات قيد التشغيل
- يجب تعيين Database Password (User Secrets أو Env Var)

---

## الاختبارات المتوفرة

### UI Tests:
- `LoginUITests.cs` - اختبارات صفحة تسجيل الدخول

### API Tests:
- `PerformanceTests.cs` - اختبارات الأداء
- `ErrorHandlingTests.cs` - اختبارات التعامل مع الأخطاء
- `ValidationTests.cs` - اختبارات التحقق من الإدخال

---

## ملاحظات

- ⚠️ **يجب تشغيل التطبيق قبل تشغيل الاختبارات**
- ⚠️ **يجب تثبيت Chrome Browser**
- ⚠️ **يجب تعيين Database Password**

---

**آخر تحديث:** 2025

