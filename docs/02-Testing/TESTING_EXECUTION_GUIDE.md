# دليل تنفيذ الاختبارات الشاملة
## Comprehensive Testing Execution Guide

**التاريخ:** 2025  
**الإصدار:** 1.0

---

## 📋 نظرة عامة

هذا الدليل يشرح كيفية تنفيذ جميع اختبارات LegalDocSystem خطوة بخطوة.

---

## 🛠️ إعداد البيئة

### 1. تثبيت الأدوات

#### Selenium WebDriver:

```bash
cd tests/Integration
dotnet add package Selenium.WebDriver
dotnet add package Selenium.WebDriver.ChromeDriver
```

#### Postman:

- تحميل من [postman.com](https://www.postman.com/downloads/)
- أو استخدام Postman CLI

#### Apache JMeter (اختياري):

- تحميل من [jmeter.apache.org](https://jmeter.apache.org/download_jmeter.cgi)

---

## 📝 خطوات التنفيذ

### المرحلة 1: إعداد البيئة (30 دقيقة)

#### 1.1 تشغيل التطبيق:

```bash
cd src

# تعيين User Secrets
dotnet user-secrets set "Database:Password" "YourPassword"

# تشغيل التطبيق
dotnet run
```

#### 1.2 التحقق من التطبيق:

- افتح المتصفح: `http://localhost:5001`
- تحقق من أن التطبيق يعمل

#### 1.3 إعداد قاعدة البيانات:

- تأكد من أن PostgreSQL يعمل
- تأكد من أن Database موجودة
- تأكد من وجود بيانات اختبار

---

### المرحلة 2: اختبار UI/UX (2-3 ساعات)

#### 2.1 تشغيل UI Tests:

```bash
cd tests/Integration
dotnet test --filter "FullyQualifiedName~UITests"
```

#### 2.2 الاختبارات اليدوية:

**اختبار تسجيل الدخول:**

1. افتح `http://localhost:5001/login`
2. أدخل بيانات صحيحة
3. تحقق من إعادة التوجيه
4. أدخل بيانات خاطئة
5. تحقق من رسالة الخطأ

**اختبار Responsive Design:**

1. افتح Developer Tools (F12)
2. اضغط Ctrl+Shift+M (Toggle Device Toolbar)
3. اختر أجهزة مختلفة (Mobile, Tablet, Desktop)
4. تحقق من التصميم

---

### المرحلة 3: اختبار الأداء (2-3 ساعات)

#### 3.1 Postman Collection:

**إنشاء Collection:**

1. افتح Postman
2. أنشئ Collection جديد: "LegalDocSystem API Tests"
3. أضف Environment: "Local Development"
   - `baseUrl`: `http://localhost:5001`
   - `token`: (سيتم ملؤه بعد Login)

**إضافة Tests:**

```javascript
// في Tests tab لكل Request
pm.test("Response time is less than 500ms", function () {
    pm.expect(pm.response.responseTime).to.be.below(500);
});

pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});
```

**تشغيل Collection:**

1. Run Collection
2. مراجعة النتائج
3. تصدير التقرير

#### 3.2 Load Testing (JMeter):

**إنشاء Test Plan:**

1. افتح JMeter
2. أنشئ Thread Group:
   - Number of Threads: 100
   - Ramp-up Period: 10
   - Loop Count: Forever
   - Duration: 60 seconds

3. أضف HTTP Request:
   - Server Name: `localhost`
   - Port: `5001`
   - Path: `/api/documents`
   - Method: `GET`

4. أضف Listeners:
   - View Results Tree
   - Summary Report
   - Graph Results

**تشغيل Test:**

1. Run Test Plan
2. مراقبة النتائج
3. تصدير التقرير

---

### المرحلة 4: اختبار الأخطاء (1 ساعة)

#### 4.1 اختبار Error Messages:

**Postman Tests:**

```javascript
// Test Create with invalid data
pm.test("Error message is generic", function () {
    pm.expect(pm.response.text()).to.include("An error occurred");
    pm.expect(pm.response.text()).to.not.include("Stack Trace");
});
```

#### 4.2 اختبار Error Logging:

1. إحداث خطأ متعمد
2. فحص Application Logs
3. فحص Database Audit Logs
4. التحقق من تسجيل الخطأ

---

### المرحلة 5: اختبار Validation (1-2 ساعة)

#### 5.1 Client-side Validation:

**Selenium Test:**

```csharp
[Test]
public void TestClientSideValidation()
{
    driver.Navigate().GoToUrl("http://localhost:5001/login");
    var loginButton = driver.FindElement(By.CssSelector("button[type='submit']"));
    loginButton.Click();
    
    // Should stay on login page
    Assert.Contains("/login", driver.Url);
}
```

#### 5.2 Server-side Validation:

**Postman Test:**

```javascript
// Test empty input
pm.test("Server-side validation works", function () {
    pm.response.to.have.status(400);
});
```

---

## 📊 قوالب التقارير

### تقرير UI Test:

```markdown
## تقرير اختبار UI/UX

**التاريخ:** [تاريخ]
**المختبر:** [اسم]

### النتائج:

| الاختبار | الحالة | الوقت (ms) | الملاحظات |
|---|---|---|---|
| Login Success | ✅ | 1200 | - |
| Login Failure | ✅ | 800 | - |
| Responsive Design | ✅ | - | يعمل على جميع الأحجام |

### المشاكل:

لا توجد مشاكل

### التوصيات:

- تحسين وقت تحميل صفحة Documents
```

### تقرير Performance Test:

```markdown
## تقرير اختبار الأداء

**التاريخ:** [تاريخ]
**الأداة:** Postman/JMeter

### النتائج:

| Endpoint | Avg Response Time (ms) | 95th Percentile (ms) | Status |
|---|---|---|---|
| GET /api/documents | 250 | 450 | ✅ |
| POST /api/documents | 800 | 1200 | ✅ |

### التوصيات:

- تحسين استعلامات قاعدة البيانات
- إضافة Caching
```

---

## ✅ Checklist التنفيذ

### قبل البدء:

- [ ] ✅ تثبيت جميع الأدوات
- [ ] ✅ تشغيل التطبيق
- [ ] ✅ إعداد قاعدة البيانات
- [ ] ✅ إنشاء Test Data

### أثناء الاختبار:

- [ ] ✅ تنفيذ جميع الاختبارات
- [ ] ✅ تسجيل النتائج
- [ ] ✅ توثيق المشاكل

### بعد الاختبار:

- [ ] ✅ مراجعة النتائج
- [ ] ✅ إصلاح المشاكل
- [ ] ✅ إعادة الاختبار
- [ ] ✅ إنشاء التقرير النهائي

---

## 🎯 النتيجة المتوقعة

بعد تنفيذ جميع الاختبارات:

- ✅ **UI/UX:** جميع الواجهات تعمل بشكل صحيح
- ✅ **الأداء:** أوقات الاستجابة ضمن المعايير
- ✅ **الأخطاء:** رسائل واضحة وآمنة
- ✅ **Validation:** يعمل على Client و Server

---

**آخر تحديث:** 2025  
**الحالة:** ✅ **جاهزة للتنفيذ**

