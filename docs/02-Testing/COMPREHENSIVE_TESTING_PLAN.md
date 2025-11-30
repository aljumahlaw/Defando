# خطة الاختبار الشاملة لمشروع LegalDocSystem
## Comprehensive Testing Plan for LegalDocSystem

**التاريخ:** 2025  
**الإصدار:** 1.0  
**الحالة:** ✅ **جاهزة للتنفيذ**

---

## 📋 الملخص التنفيذي

هذه خطة اختبار شاملة ومفصلة للتحقق من صحة جميع التحسينات الأمنية والتشغيلية التي تم تنفيذها في مشروع LegalDocSystem.

**الهدف:** ضمان عدم تأثير التحسينات على استقرار النظام ووظائفه الأساسية، والحفاظ على جودة تجربة المستخدم.

---

## 🎯 نطاق الاختبار

### المجالات المغطاة:

1. ✅ **اختبار واجهة المستخدم (UI/UX)**
2. ✅ **اختبار الأداء**
3. ✅ **اختبار التعامل مع الأخطاء**
4. ✅ **اختبار التحقق من الإدخال**
5. ✅ **اختبار الأمان** (تم تنفيذه سابقاً)

---

## 1. اختبار واجهة المستخدم (UI/UX)

### 1.1 نظرة عامة

الهدف من هذا الاختبار هو التأكد من أن واجهة المستخدم تعمل بشكل صحيح بعد جميع التحسينات، وأن تجربة المستخدم لم تتأثر سلباً.

### 1.2 الأدوات المقترحة

- **Selenium WebDriver** - للاختبارات الآلية
- **Playwright** - بديل حديث لـ Selenium
- **Browser DevTools** - للفحص اليدوي
- **Lighthouse** - لقياس الأداء والجودة

### 1.3 سيناريوهات الاختبار

#### السيناريو 1: تسجيل الدخول

**الوصف:** اختبار صفحة تسجيل الدخول والتفاعلات الأساسية.

**الخطوات:**

1. ✅ **فتح صفحة Login:**
   ```
   URL: http://localhost:5001/login
   ```

2. ✅ **التحقق من العناصر:**
   - [ ] حقل اسم المستخدم موجود
   - [ ] حقل كلمة المرور موجود
   - [ ] زر تسجيل الدخول موجود
   - [ ] التصميم متجاوب (Responsive)

3. ✅ **اختبار الإدخال:**
   - [ ] إدخال اسم مستخدم صحيح
   - [ ] إدخال كلمة مرور صحيحة
   - [ ] النقر على زر تسجيل الدخول

4. ✅ **التحقق من النتيجة:**
   - [ ] إعادة التوجيه للصفحة الرئيسية
   - [ ] ظهور رسالة نجاح (إن وجدت)
   - [ ] Session Cookie تم إنشاؤه

5. ✅ **اختبار رسائل الخطأ:**
   - [ ] إدخال بيانات خاطئة
   - [ ] التحقق من ظهور رسالة خطأ واضحة
   - [ ] التحقق من عدم ظهور تفاصيل تقنية

**الكود (Selenium C#):**

```csharp
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

public class LoginTest
{
    private IWebDriver driver;
    
    [SetUp]
    public void Setup()
    {
        driver = new ChromeDriver();
        driver.Navigate().GoToUrl("http://localhost:5001/login");
    }
    
    [Test]
    public void TestLoginSuccess()
    {
        // Find elements
        var usernameField = driver.FindElement(By.Id("username"));
        var passwordField = driver.FindElement(By.Id("password"));
        var loginButton = driver.FindElement(By.CssSelector("button[type='submit']"));
        
        // Enter credentials
        usernameField.SendKeys("admin");
        passwordField.SendKeys("AdminPassword123");
        loginButton.Click();
        
        // Wait for redirect
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.Url.Contains("/") && !d.Url.Contains("/login"));
        
        // Assert
        Assert.IsTrue(driver.Url.Contains("/") && !driver.Url.Contains("/login"));
    }
    
    [Test]
    public void TestLoginFailure()
    {
        var usernameField = driver.FindElement(By.Id("username"));
        var passwordField = driver.FindElement(By.Id("password"));
        var loginButton = driver.FindElement(By.CssSelector("button[type='submit']"));
        
        usernameField.SendKeys("admin");
        passwordField.SendKeys("WrongPassword");
        loginButton.Click();
        
        // Wait for error message
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        var errorMessage = wait.Until(d => 
            d.FindElement(By.CssSelector(".alert-danger")));
        
        // Assert
        Assert.IsTrue(errorMessage.Displayed);
        Assert.IsTrue(errorMessage.Text.Contains("اسم المستخدم أو كلمة المرور"));
        Assert.IsFalse(errorMessage.Text.Contains("Stack Trace"));
        Assert.IsFalse(errorMessage.Text.Contains("Exception"));
    }
    
    [TearDown]
    public void TearDown()
    {
        driver.Quit();
    }
}
```

**النتيجة المتوقعة:**
- ✅ تسجيل الدخول يعمل بشكل صحيح
- ✅ رسائل الخطأ واضحة وآمنة
- ✅ لا توجد تفاصيل تقنية مكشوفة

---

#### السيناريو 2: عرض المستندات

**الوصف:** اختبار صفحة عرض المستندات والتفاعلات.

**الخطوات:**

1. ✅ **تسجيل الدخول أولاً**

2. ✅ **فتح صفحة المستندات:**
   ```
   URL: http://localhost:5001/documents
   ```

3. ✅ **التحقق من العناصر:**
   - [ ] جدول المستندات موجود
   - [ ] زر إضافة مستند جديد موجود
   - [ ] حقل البحث موجود
   - [ ] أزرار التعديل/الحذف موجودة

4. ✅ **اختبار البحث:**
   - [ ] إدخال نص في حقل البحث
   - [ ] النقر على زر البحث
   - [ ] التحقق من ظهور النتائج

5. ✅ **اختبار التحميل:**
   - [ ] قياس وقت تحميل الصفحة
   - [ ] التحقق من عدم وجود تأخير غير طبيعي

**الكود (Selenium C#):**

```csharp
[Test]
public void TestDocumentsPage()
{
    // Login first
    Login();
    
    // Navigate to documents page
    driver.Navigate().GoToUrl("http://localhost:5001/documents");
    
    // Wait for page load
    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    var documentsTable = wait.Until(d => 
        d.FindElement(By.CssSelector("table")));
    
    // Assert elements exist
    Assert.IsTrue(documentsTable.Displayed);
    
    var searchField = driver.FindElement(By.CssSelector("input[type='search']"));
    Assert.IsTrue(searchField.Displayed);
    
    // Test search
    searchField.SendKeys("test");
    var searchButton = driver.FindElement(By.CssSelector("button[type='submit']"));
    searchButton.Click();
    
    // Wait for results
    Thread.Sleep(2000);
    
    // Assert results displayed
    var results = driver.FindElements(By.CssSelector("table tbody tr"));
    Assert.IsTrue(results.Count > 0);
}
```

**النتيجة المتوقعة:**
- ✅ الصفحة تتحمل بسرعة
- ✅ جميع العناصر موجودة
- ✅ البحث يعمل بشكل صحيح

---

#### السيناريو 3: إضافة مستند جديد

**الوصف:** اختبار إضافة مستند جديد عبر النموذج.

**الخطوات:**

1. ✅ **فتح صفحة المستندات**

2. ✅ **النقر على زر إضافة مستند**

3. ✅ **ملء النموذج:**
   - [ ] إدخال اسم المستند
   - [ ] اختيار نوع المستند
   - [ ] اختيار المجلد
   - [ ] رفع ملف

4. ✅ **التحقق من Validation:**
   - [ ] ترك الحقول المطلوبة فارغة
   - [ ] التحقق من ظهور رسائل التحقق

5. ✅ **إرسال النموذج:**
   - [ ] النقر على زر الحفظ
   - [ ] التحقق من ظهور رسالة نجاح
   - [ ] التحقق من ظهور المستند في القائمة

**الكود (Selenium C#):**

```csharp
[Test]
public void TestAddDocument()
{
    Login();
    driver.Navigate().GoToUrl("http://localhost:5001/documents");
    
    // Click add button
    var addButton = driver.FindElement(By.CssSelector("button.btn-primary"));
    addButton.Click();
    
    // Wait for modal
    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
    var modal = wait.Until(d => 
        d.FindElement(By.CssSelector(".modal")));
    
    // Fill form
    var documentNameField = driver.FindElement(By.Id("documentName"));
    documentNameField.SendKeys("Test Document");
    
    var documentTypeSelect = new SelectElement(
        driver.FindElement(By.Id("documentType")));
    documentTypeSelect.SelectByText("contract");
    
    // Submit form
    var saveButton = driver.FindElement(By.CssSelector("button[type='submit']"));
    saveButton.Click();
    
    // Wait for success message
    var successMessage = wait.Until(d => 
        d.FindElement(By.CssSelector(".alert-success")));
    
    // Assert
    Assert.IsTrue(successMessage.Displayed);
    Assert.IsTrue(successMessage.Text.Contains("تم"));
}
```

**النتيجة المتوقعة:**
- ✅ النموذج يعمل بشكل صحيح
- ✅ Validation يعمل
- ✅ رسائل النجاح واضحة

---

#### السيناريو 4: التوافق عبر المتصفحات

**الوصف:** اختبار التطبيق على متصفحات مختلفة.

**المتصفحات المطلوبة:**
- Chrome (Latest)
- Firefox (Latest)
- Edge (Latest)
- Safari (إذا كان متاحاً)

**الخطوات:**

1. ✅ **اختبار كل متصفح:**
   - [ ] تسجيل الدخول
   - [ ] عرض المستندات
   - [ ] إضافة مستند
   - [ ] البحث

2. ✅ **التحقق من:**
   - [ ] التصميم متسق
   - [ ] الوظائف تعمل
   - [ ] لا توجد أخطاء في Console

**الكود (Selenium C# - Cross-browser):**

```csharp
[TestCase("Chrome")]
[TestCase("Firefox")]
[TestCase("Edge")]
public void TestCrossBrowser(string browser)
{
    IWebDriver driver;
    
    switch (browser)
    {
        case "Chrome":
            driver = new ChromeDriver();
            break;
        case "Firefox":
            driver = new FirefoxDriver();
            break;
        case "Edge":
            driver = new EdgeDriver();
            break;
        default:
            throw new ArgumentException("Unknown browser");
    }
    
    try
    {
        driver.Navigate().GoToUrl("http://localhost:5001/login");
        
        // Test login
        var usernameField = driver.FindElement(By.Id("username"));
        var passwordField = driver.FindElement(By.Id("password"));
        usernameField.SendKeys("admin");
        passwordField.SendKeys("AdminPassword123");
        driver.FindElement(By.CssSelector("button[type='submit']")).Click();
        
        // Assert
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        wait.Until(d => !d.Url.Contains("/login"));
        Assert.IsTrue(driver.Url.Contains("/"));
    }
    finally
    {
        driver.Quit();
    }
}
```

**النتيجة المتوقعة:**
- ✅ التطبيق يعمل على جميع المتصفحات
- ✅ التصميم متسق
- ✅ لا توجد أخطاء

---

#### السيناريو 5: التوافق مع الأجهزة (Responsive)

**الوصف:** اختبار التطبيق على أحجام شاشات مختلفة.

**الأحجام المطلوبة:**
- Desktop (1920x1080)
- Tablet (768x1024)
- Mobile (375x667)

**الخطوات:**

1. ✅ **تغيير حجم النافذة:**
   ```csharp
   driver.Manage().Window.Size = new Size(375, 667); // Mobile
   ```

2. ✅ **التحقق من:**
   - [ ] التصميم متجاوب
   - [ ] جميع العناصر مرئية
   - [ ] القوائم تعمل بشكل صحيح

**الكود (Selenium C#):**

```csharp
[TestCase(1920, 1080, "Desktop")]
[TestCase(768, 1024, "Tablet")]
[TestCase(375, 667, "Mobile")]
public void TestResponsive(int width, int height, string device)
{
    driver.Manage().Window.Size = new Size(width, height);
    driver.Navigate().GoToUrl("http://localhost:5001/login");
    
    // Check if elements are visible
    var usernameField = driver.FindElement(By.Id("username"));
    Assert.IsTrue(usernameField.Displayed);
    
    // Check if layout is responsive
    var container = driver.FindElement(By.CssSelector(".container"));
    var containerWidth = container.Size.Width;
    Assert.IsTrue(containerWidth <= width);
}
```

**النتيجة المتوقعة:**
- ✅ التصميم متجاوب على جميع الأحجام
- ✅ جميع العناصر مرئية
- ✅ تجربة المستخدم جيدة

---

## 2. اختبار الأداء

### 2.1 نظرة عامة

الهدف من هذا الاختبار هو قياس أداء التطبيق بعد التحسينات والتأكد من عدم وجود تدهور في الأداء.

### 2.2 الأدوات المقترحة

- **Postman** - لاختبار API Performance
- **Apache JMeter** - لاختبار Load Testing
- **Application Insights** - لمراقبة الأداء
- **Browser DevTools Network Tab** - لقياس أوقات التحميل

### 2.3 سيناريوهات الاختبار

#### السيناريو 1: قياس أوقات استجابة API

**الوصف:** قياس أوقات استجابة جميع API endpoints.

**الخطوات:**

1. ✅ **إعداد Postman Collection:**
   - إنشاء Collection لجميع APIs
   - إضافة Environment Variables
   - إضافة Tests لقياس Response Time

2. ✅ **اختبار كل Endpoint:**
   - GET /api/documents
   - GET /api/documents/{id}
   - POST /api/documents
   - PUT /api/documents/{id}
   - DELETE /api/documents/{id}

3. ✅ **قياس:**
   - Response Time
   - Status Code
   - Response Size

**Postman Collection Example:**

```json
{
  "info": {
    "name": "LegalDocSystem API Performance Tests",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Get All Documents",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{token}}"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/api/documents",
          "host": ["{{baseUrl}}"],
          "path": ["api", "documents"]
        }
      },
      "event": [
        {
          "listen": "test",
          "script": {
            "exec": [
              "pm.test(\"Response time is less than 500ms\", function () {",
              "    pm.expect(pm.response.responseTime).to.be.below(500);",
              "});",
              "",
              "pm.test(\"Status code is 200\", function () {",
              "    pm.response.to.have.status(200);",
              "});"
            ]
          }
        }
      ]
    }
  ]
}
```

**النتائج المتوقعة:**

| Endpoint | Response Time (ms) | Status Code | Notes |
|---|---|---|---|
| GET /api/documents | < 500 | 200 | ✅ |
| GET /api/documents/{id} | < 200 | 200 | ✅ |
| POST /api/documents | < 1000 | 201 | ✅ |
| PUT /api/documents/{id} | < 500 | 204 | ✅ |
| DELETE /api/documents/{id} | < 500 | 204 | ✅ |

---

#### السيناريو 2: Load Testing

**الوصف:** اختبار التطبيق تحت حمل عالي.

**الأدوات:**
- Apache JMeter
- k6
- Artillery

**الخطوات:**

1. ✅ **إعداد JMeter Test Plan:**
   - Thread Group (100 users)
   - HTTP Request Samplers
   - Response Assertions

2. ✅ **تنفيذ الاختبار:**
   - Ramp-up: 10 seconds
   - Duration: 60 seconds
   - Users: 100 concurrent

3. ✅ **قياس:**
   - Throughput
   - Response Time (Average, Min, Max)
   - Error Rate

**JMeter Test Plan:**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<jmeterTestPlan version="1.2">
  <hashTree>
    <TestPlan guiclass="TestPlanGui" testclass="TestPlan" testname="LegalDocSystem Load Test">
      <boolProp name="TestPlan.functional_mode">false</boolProp>
      <boolProp name="TestPlan.serialize_threadgroups">false</boolProp>
    </TestPlan>
    <hashTree>
      <ThreadGroup guiclass="ThreadGroupGui" testclass="ThreadGroup" testname="Thread Group">
        <stringProp name="ThreadGroup.on_sample_error">continue</stringProp>
        <elementProp name="ThreadGroup.main_controller" elementType="LoopController">
          <boolProp name="LoopController.continue_forever">false</boolProp>
          <intProp name="LoopController.loops">-1</intProp>
        </elementProp>
        <stringProp name="ThreadGroup.num_threads">100</stringProp>
        <stringProp name="ThreadGroup.ramp_time">10</stringProp>
        <longProp name="ThreadGroup.start_time">0</longProp>
        <longProp name="ThreadGroup.end_time">0</longProp>
        <boolProp name="ThreadGroup.scheduler">true</boolProp>
        <stringProp name="ThreadGroup.duration">60</stringProp>
        <stringProp name="ThreadGroup.delay"></stringProp>
      </ThreadGroup>
      <hashTree>
        <HTTPSamplerProxy guiclass="HttpTestSampleGui" testclass="HTTPSamplerProxy" testname="Get Documents">
          <elementProp name="HTTPsampler.Arguments" elementType="Arguments">
            <collectionProp name="Arguments.arguments"/>
          </elementProp>
          <stringProp name="HTTPSampler.domain">localhost</stringProp>
          <stringProp name="HTTPSampler.port">5001</stringProp>
          <stringProp name="HTTPSampler.path">/api/documents</stringProp>
          <stringProp name="HTTPSampler.method">GET</stringProp>
        </HTTPSamplerProxy>
        <hashTree/>
      </hashTree>
    </hashTree>
  </hashTree>
</jmeterTestPlan>
```

**النتائج المتوقعة:**

| Metric | Target | Actual | Status |
|---|---|---|---|
| Throughput | > 100 req/s | - | ⏳ |
| Average Response Time | < 500ms | - | ⏳ |
| Error Rate | < 1% | - | ⏳ |
| 95th Percentile | < 1000ms | - | ⏳ |

---

#### السيناريو 3: اختبار تحميل الصفحات

**الوصف:** قياس أوقات تحميل صفحات Blazor.

**الأدوات:**
- Browser DevTools
- Lighthouse
- WebPageTest

**الخطوات:**

1. ✅ **فتح Browser DevTools:**
   - Network Tab
   - Performance Tab

2. ✅ **قياس تحميل الصفحات:**
   - /login
   - /documents
   - /folders
   - /tasks

3. ✅ **قياس:**
   - DOMContentLoaded
   - Load Complete
   - Time to Interactive

**الكود (JavaScript - Browser Console):**

```javascript
// Measure page load time
window.addEventListener('load', function() {
    const perfData = window.performance.timing;
    const pageLoadTime = perfData.loadEventEnd - perfData.navigationStart;
    const domReadyTime = perfData.domContentLoadedEventEnd - perfData.navigationStart;
    
    console.log('Page Load Time:', pageLoadTime, 'ms');
    console.log('DOM Ready Time:', domReadyTime, 'ms');
});
```

**النتائج المتوقعة:**

| Page | Load Time (ms) | DOM Ready (ms) | Status |
|---|---|---|---|
| /login | < 2000 | < 1000 | ✅ |
| /documents | < 3000 | < 2000 | ✅ |
| /folders | < 2000 | < 1500 | ✅ |
| /tasks | < 2500 | < 1800 | ✅ |

---

## 3. اختبار التعامل مع الأخطاء

### 3.1 نظرة عامة

الهدف من هذا الاختبار هو التأكد من أن التطبيق يتعامل مع الأخطاء بشكل صحيح ولا يكشف معلومات حساسة.

### 3.2 الأدوات المقترحة

- **Postman** - لاختبار Error Responses
- **Browser DevTools** - لفحص Network Errors
- **Application Logs** - لفحص Error Logging

### 3.3 سيناريوهات الاختبار

#### السيناريو 1: اختبار رسائل الخطأ في API

**الوصف:** التحقق من أن رسائل الخطأ في API عامة وآمنة.

**الخطوات:**

1. ✅ **اختبار خطأ في Create:**
   ```bash
   curl -X POST http://localhost:5001/api/documents \
     -H "Content-Type: application/json" \
     -H "X-CSRF-TOKEN: token" \
     -d '{"invalid": "data"}'
   ```

2. ✅ **التحقق من:**
   - [ ] Status Code = 500
   - [ ] Response Body = "An error occurred..."
   - [ ] لا توجد Stack Traces
   - [ ] لا توجد تفاصيل تقنية

**Postman Test:**

```javascript
pm.test("Error message is generic", function () {
    pm.expect(pm.response.text()).to.include("An error occurred");
    pm.expect(pm.response.text()).to.not.include("Stack Trace");
    pm.expect(pm.response.text()).to.not.include("Exception");
    pm.expect(pm.response.text()).to.not.include("at ");
});

pm.test("Status code is 500", function () {
    pm.response.to.have.status(500);
});
```

**النتيجة المتوقعة:**
- ✅ رسائل الخطأ عامة
- ✅ لا توجد معلومات حساسة
- ✅ Status Code صحيح

---

#### السيناريو 2: اختبار رسائل الخطأ في UI

**الوصف:** التحقق من أن رسائل الخطأ في واجهة المستخدم واضحة.

**الخطوات:**

1. ✅ **فتح صفحة Login**

2. ✅ **إدخال بيانات خاطئة**

3. ✅ **التحقق من:**
   - [ ] رسالة خطأ واضحة
   - [ ] لا توجد Stack Traces
   - [ ] التصميم مناسب (Bootstrap Alert)

**الكود (Selenium C#):**

```csharp
[Test]
public void TestErrorMessagesInUI()
{
    driver.Navigate().GoToUrl("http://localhost:5001/login");
    
    var usernameField = driver.FindElement(By.Id("username"));
    var passwordField = driver.FindElement(By.Id("password"));
    var loginButton = driver.FindElement(By.CssSelector("button[type='submit']"));
    
    // Enter wrong credentials
    usernameField.SendKeys("wronguser");
    passwordField.SendKeys("wrongpass");
    loginButton.Click();
    
    // Wait for error message
    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
    var errorMessage = wait.Until(d => 
        d.FindElement(By.CssSelector(".alert-danger")));
    
    // Assert
    Assert.IsTrue(errorMessage.Displayed);
    Assert.IsTrue(errorMessage.Text.Contains("اسم المستخدم أو كلمة المرور"));
    Assert.IsFalse(errorMessage.Text.Contains("Stack Trace"));
    Assert.IsFalse(errorMessage.Text.Contains("Exception"));
    Assert.IsFalse(errorMessage.Text.Contains("at "));
}
```

**النتيجة المتوقعة:**
- ✅ رسائل الخطأ واضحة
- ✅ لا توجد تفاصيل تقنية
- ✅ التصميم مناسب

---

#### السيناريو 3: اختبار Error Logging

**الوصف:** التحقق من أن الأخطاء يتم تسجيلها في Logs.

**الخطوات:**

1. ✅ **إحداث خطأ متعمد:**
   - إرسال طلب غير صحيح
   - محاولة الوصول لملف غير موجود

2. ✅ **فحص Logs:**
   - Application Logs
   - Database Audit Logs

3. ✅ **التحقق من:**
   - [ ] الخطأ مسجل في Logs
   - [ ] تفاصيل الخطأ موجودة
   - [ ] Timestamp موجود
   - [ ] User ID موجود (إن أمكن)

**الكود (C# - Log Verification):**

```csharp
[Test]
public void TestErrorLogging()
{
    // Trigger an error
    var client = new HttpClient();
    var response = await client.PostAsync(
        "http://localhost:5001/api/documents",
        new StringContent("{\"invalid\": \"data\"}", Encoding.UTF8, "application/json"));
    
    // Wait for logging
    await Task.Delay(2000);
    
    // Check logs (assuming logs are in a file or database)
    var logContent = File.ReadAllText("logs/app.log");
    Assert.IsTrue(logContent.Contains("Error creating document"));
    Assert.IsTrue(logContent.Contains("User:"));
    Assert.IsTrue(logContent.Contains("Timestamp:"));
}
```

**النتيجة المتوقعة:**
- ✅ الأخطاء مسجلة في Logs
- ✅ تفاصيل كافية للتحليل
- ✅ لا توجد معلومات حساسة في Logs

---

## 4. اختبار التحقق من الإدخال

### 4.1 نظرة عامة

الهدف من هذا الاختبار هو التأكد من أن التحقق من الإدخال يعمل بشكل صحيح على Client-side و Server-side.

### 4.2 الأدوات المقترحة

- **Selenium** - لاختبار Client-side Validation
- **Postman** - لاختبار Server-side Validation
- **Browser DevTools** - لفحص JavaScript Validation

### 4.3 سيناريوهات الاختبار

#### السيناريو 1: اختبار Client-side Validation

**الوصف:** التحقق من أن Validation يعمل في المتصفح.

**الخطوات:**

1. ✅ **فتح صفحة Login**

2. ✅ **ترك الحقول فارغة:**
   - [ ] النقر على زر تسجيل الدخول
   - [ ] التحقق من ظهور رسائل التحقق

3. ✅ **إدخال بيانات غير صحيحة:**
   - [ ] إدخال نص قصير جداً
   - [ ] إدخال نص طويل جداً
   - [ ] إدخال أحرف خاصة غير مسموحة

**الكود (Selenium C#):**

```csharp
[Test]
public void TestClientSideValidation()
{
    driver.Navigate().GoToUrl("http://localhost:5001/login");
    
    // Try to submit without filling fields
    var loginButton = driver.FindElement(By.CssSelector("button[type='submit']"));
    loginButton.Click();
    
    // Wait for validation messages
    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
    
    // Check if validation messages appear
    var validationMessages = driver.FindElements(By.CssSelector(".validation-message"));
    Assert.IsTrue(validationMessages.Count > 0);
    
    // Check if form was not submitted
    Assert.IsTrue(driver.Url.Contains("/login"));
}
```

**النتيجة المتوقعة:**
- ✅ رسائل التحقق تظهر فوراً
- ✅ النموذج لا يُرسل مع بيانات غير صحيحة
- ✅ الرسائل واضحة ومفيدة

---

#### السيناريو 2: اختبار Server-side Validation

**الوصف:** التحقق من أن Validation يعمل على الخادم.

**الخطوات:**

1. ✅ **إرسال طلب بدون بيانات:**
   ```bash
   curl -X POST http://localhost:5001/api/documents \
     -H "Content-Type: application/json" \
     -d '{}'
   ```

2. ✅ **التحقق من:**
   - [ ] Status Code = 400 Bad Request
   - [ ] رسالة خطأ واضحة
   - [ ] قائمة بالحقول المطلوبة

**Postman Test:**

```javascript
pm.test("Server-side validation works", function () {
    pm.response.to.have.status(400);
    var jsonData = pm.response.json();
    pm.expect(jsonData.errors).to.exist;
});

pm.test("Error message is clear", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.title).to.include("validation");
});
```

**النتيجة المتوقعة:**
- ✅ Server-side Validation يعمل
- ✅ رسائل الخطأ واضحة
- ✅ Status Code صحيح

---

#### السيناريو 3: اختبار SQL Injection Protection

**الوصف:** التحقق من أن التطبيق محمي من SQL Injection.

**الخطوات:**

1. ✅ **إرسال طلب مع SQL Injection:**
   ```bash
   curl -X GET "http://localhost:5001/api/documents/search?query=' OR '1'='1"
   ```

2. ✅ **التحقق من:**
   - [ ] لا يتم تنفيذ SQL
   - [ ] Response آمن
   - [ ] لا توجد أخطاء في Database

**Postman Test:**

```javascript
pm.test("SQL Injection is prevented", function () {
    pm.response.to.have.status(200);
    // Should return empty results or handle safely
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.be.an('array');
});
```

**النتيجة المتوقعة:**
- ✅ SQL Injection محمي
- ✅ Response آمن
- ✅ لا توجد أخطاء

---

#### السيناريو 4: اختبار XSS Protection

**الوصف:** التحقق من أن التطبيق محمي من XSS.

**الخطوات:**

1. ✅ **إدخال نص يحتوي على Script:**
   ```javascript
   <script>alert('XSS')</script>
   ```

2. ✅ **التحقق من:**
   - [ ] النص يتم Encoding
   - [ ] Script لا يتم تنفيذه
   - [ ] Response آمن

**الكود (Selenium C#):**

```csharp
[Test]
public void TestXSSProtection()
{
    Login();
    driver.Navigate().GoToUrl("http://localhost:5001/documents");
    
    // Try to add document with XSS payload
    var addButton = driver.FindElement(By.CssSelector("button.btn-primary"));
    addButton.Click();
    
    var documentNameField = driver.FindElement(By.Id("documentName"));
    documentNameField.SendKeys("<script>alert('XSS')</script>");
    
    var saveButton = driver.FindElement(By.CssSelector("button[type='submit']"));
    saveButton.Click();
    
    // Check if script was executed
    var alerts = driver.SwitchTo().Alert();
    Assert.IsFalse(alerts != null, "XSS script was executed!");
    
    // Check if text was encoded
    var pageSource = driver.PageSource;
    Assert.IsTrue(pageSource.Contains("&lt;script&gt;"));
}
```

**النتيجة المتوقعة:**
- ✅ XSS محمي
- ✅ النص يتم Encoding
- ✅ Script لا يتم تنفيذه

---

## 📊 خطة تنفيذ الاختبارات

### المرحلة 1: إعداد البيئة (يوم 1)

- [ ] ✅ تثبيت الأدوات (Selenium, Postman, JMeter)
- [ ] ✅ إعداد Test Environment
- [ ] ✅ إنشاء Test Data
- [ ] ✅ إعداد CI/CD Pipeline (اختياري)

### المرحلة 2: اختبار UI/UX (يوم 2-3)

- [ ] ✅ اختبار تسجيل الدخول
- [ ] ✅ اختبار عرض المستندات
- [ ] ✅ اختبار إضافة مستند
- [ ] ✅ اختبار التوافق عبر المتصفحات
- [ ] ✅ اختبار Responsive Design

### المرحلة 3: اختبار الأداء (يوم 4-5)

- [ ] ✅ قياس أوقات استجابة API
- [ ] ✅ Load Testing
- [ ] ✅ اختبار تحميل الصفحات
- [ ] ✅ تحليل النتائج

### المرحلة 4: اختبار الأخطاء (يوم 6)

- [ ] ✅ اختبار رسائل الخطأ في API
- [ ] ✅ اختبار رسائل الخطأ في UI
- [ ] ✅ اختبار Error Logging

### المرحلة 5: اختبار Validation (يوم 7)

- [ ] ✅ اختبار Client-side Validation
- [ ] ✅ اختبار Server-side Validation
- [ ] ✅ اختبار SQL Injection Protection
- [ ] ✅ اختبار XSS Protection

### المرحلة 6: تقرير النتائج (يوم 8)

- [ ] ✅ توثيق جميع النتائج
- [ ] ✅ تحديد المشاكل
- [ ] ✅ تقديم التوصيات

---

## 📝 قوالب التوثيق

### نموذج تقرير الاختبار

```markdown
## تقرير اختبار: [اسم الاختبار]

**التاريخ:** [تاريخ]
**المختبر:** [اسم]
**البيئة:** [Development/Staging/Production]

### النتائج:

| الاختبار | الحالة | الملاحظات |
|---|---|---|
| Test 1 | ✅/❌ | ... |
| Test 2 | ✅/❌ | ... |

### المشاكل المكتشفة:

1. [وصف المشكلة]
   - الخطورة: [عالية/متوسطة/منخفضة]
   - الحل المقترح: [وصف]

### التوصيات:

1. [توصية 1]
2. [توصية 2]
```

---

## 🎯 النتيجة المتوقعة

بعد تنفيذ جميع الاختبارات:

- ✅ **UI/UX:** جميع الواجهات تعمل بشكل صحيح
- ✅ **الأداء:** أوقات الاستجابة ضمن المعايير
- ✅ **الأخطاء:** رسائل واضحة وآمنة
- ✅ **Validation:** يعمل على Client و Server
- ✅ **الأمان:** محمي من الثغرات الشائعة

---

**آخر تحديث:** 2025  
**الحالة:** ✅ **جاهزة للتنفيذ**

