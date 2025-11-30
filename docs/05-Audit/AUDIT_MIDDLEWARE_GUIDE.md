# دليل Audit Logging Middleware
## Audit Logging Middleware Implementation Guide

**التاريخ:** 2025  
**الإصدار:** 1.0

---

## 📋 نظرة عامة

تم إنشاء `AuditLoggingMiddleware` لتسجيل جميع طلبات HTTP تلقائياً في نظام Audit Logging.

---

## 🎯 الميزات

### ✅ **التسجيل التلقائي:**
- ✅ جميع طلبات HTTP (GET, POST, PUT, DELETE, etc.)
- ✅ معلومات الطلب (URL, Method, Query String)
- ✅ معلومات المستخدم (User ID, Username, Role)
- ✅ معلومات الشبكة (IP Address, User Agent)
- ✅ وقت المعالجة (Duration)
- ✅ حالة الطلب (Success/Failed/Error)
- ✅ Status Code

### ✅ **الفلترة الذكية:**
- ✅ تخطي الملفات الثابتة (CSS, JS, Images, Fonts)
- ✅ تخطي Health Checks
- ✅ تخطي Framework Files

### ✅ **تصنيف تلقائي:**
- ✅ تصنيف الطلبات حسب المسار (Document, Folder, Task, User, etc.)

---

## 📁 الملفات

### 1. `src/Middleware/AuditLoggingMiddleware.cs`

**الوصف:** Middleware لتسجيل جميع طلبات HTTP.

**الميزات:**
- تسجيل بداية الطلب
- تسجيل اكتمال الطلب (نجاح/فشل)
- تسجيل الأخطاء
- استخلاص تلقائي لـ User و IP
- حساب وقت المعالجة

---

## 🔧 التكوين

### 1. إضافة Middleware في Program.cs

```csharp
// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Audit Logging Middleware (must be after Authentication and Authorization)
app.UseMiddleware<AuditLoggingMiddleware>();

app.UseSession();
```

**الترتيب مهم:**
1. ✅ `UseAuthentication()` - يجب أن يكون قبل Middleware
2. ✅ `UseAuthorization()` - يجب أن يكون قبل Middleware
3. ✅ `UseMiddleware<AuditLoggingMiddleware>()` - بعد Authentication/Authorization
4. ✅ `UseSession()` - بعد Middleware

---

## 📊 البيانات المسجلة

### 1. بداية الطلب (Request Start)

**Action:** `http_{method}_start`

**مثال:**
```json
{
  "Event": "HttpRequest",
  "Category": "Document",
  "Action": "http_get_start",
  "SubjectIdentifier": 123,
  "SubjectName": "admin",
  "SubjectType": "admin",
  "Data": "Request started: GET /api/documents/5 | UserAgent: Mozilla/5.0...",
  "IpAddress": "192.168.1.100",
  "UserAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)...",
  "Created": "2025-01-15T10:30:00Z"
}
```

---

### 2. اكتمال الطلب (Request Completion)

**Action:** `http_{method}_completed`

**مثال:**
```json
{
  "Event": "HttpRequest",
  "Category": "Document",
  "Action": "http_get_completed",
  "SubjectIdentifier": 123,
  "SubjectName": "admin",
  "SubjectType": "admin",
  "Data": "Request completed: GET /api/documents/5 | Status: 200 (Success) | Duration: 45ms",
  "IpAddress": "192.168.1.100",
  "UserAgent": "Mozilla/5.0...",
  "Created": "2025-01-15T10:30:00Z"
}
```

---

### 3. خطأ في الطلب (Request Error)

**Action:** `http_{method}_error`

**مثال:**
```json
{
  "Event": "HttpRequest",
  "Category": "Document",
  "Action": "http_get_error",
  "SubjectIdentifier": 123,
  "SubjectName": "admin",
  "SubjectType": "admin",
  "Data": "Request error: GET /api/documents/999 | Status: 404 | Duration: 12ms | Error: Document not found",
  "IpAddress": "192.168.1.100",
  "UserAgent": "Mozilla/5.0...",
  "Created": "2025-01-15T10:30:00Z"
}
```

---

## 🔍 التصنيف التلقائي

يتم تصنيف الطلبات تلقائياً حسب المسار:

| المسار | Category |
|---|---|
| `/api/documents` أو `/documents` | Document |
| `/api/folders` أو `/folders` | Folder |
| `/api/tasks` أو `/tasks` | Task |
| `/api/users` أو `/users` | User |
| `/api/shared` أو `/shared` | SharedLink |
| `/login` أو `/logout` | Authentication |
| `/api/outgoing` أو `/outgoing` | Outgoing |
| `/api/incoming` أو `/incoming` | Incoming |
| `/settings` أو `/smtp` | Settings |
| `/hangfire` | System |
| `/api/*` | API |
| أخرى | General |

---

## 🚫 الطلبات المتخطاة

الطلبات التالية لا يتم تسجيلها:

### 1. الملفات الثابتة:
- `/css/*`
- `/js/*`
- `/lib/*`
- `/images/*`
- `/fonts/*`
- `/favicon.ico`
- `/_framework/*`
- `/_content/*`

### 2. Health Checks:
- `/health`
- `/metrics`

---

## 📝 أمثلة الاستخدام

### مثال 1: طلب GET عادي

**الطلب:**
```
GET /api/documents/5
```

**السجلات:**
1. **بداية الطلب:**
   - Action: `http_get_start`
   - Data: "Request started: GET /api/documents/5 | UserAgent: ..."

2. **اكتمال الطلب:**
   - Action: `http_get_completed`
   - Data: "Request completed: GET /api/documents/5 | Status: 200 (Success) | Duration: 45ms"

---

### مثال 2: طلب POST مع خطأ

**الطلب:**
```
POST /api/documents
```

**السجلات:**
1. **بداية الطلب:**
   - Action: `http_post_start`
   - Data: "Request started: POST /api/documents | UserAgent: ..."

2. **خطأ في الطلب:**
   - Action: `http_post_error`
   - Data: "Request error: POST /api/documents | Status: 400 | Duration: 120ms | Error: Validation failed"

---

### مثال 3: طلب بدون تسجيل دخول

**الطلب:**
```
GET /login
```

**السجلات:**
1. **بداية الطلب:**
   - Action: `http_get_start`
   - SubjectIdentifier: null
   - SubjectName: null
   - Category: "Authentication"

2. **اكتمال الطلب:**
   - Action: `http_get_completed`
   - Status: 200

---

## ⚙️ التخصيص

### 1. إضافة مسارات للتخطي

عدّل `ShouldSkipLogging()`:

```csharp
private static bool ShouldSkipLogging(PathString path)
{
    var pathValue = path.Value?.ToLower() ?? string.Empty;

    // إضافة مسارات جديدة
    if (pathValue.StartsWith("/custom-path/"))
    {
        return true;
    }

    // ... باقي الكود
}
```

---

### 2. إضافة تصنيفات جديدة

عدّل `DetermineCategory()`:

```csharp
private static string DetermineCategory(string path)
{
    var pathLower = path.ToLower();

    // إضافة تصنيف جديد
    if (pathLower.Contains("/reports"))
        return "Report";

    // ... باقي الكود
}
```

---

## 🔒 الأمان

### 1. معالجة الأخطاء

✅ **جميع استدعاءات Audit Logging محمية:**
```csharp
try
{
    await auditService.LogEventAsync(entry);
}
catch (Exception ex)
{
    // Don't throw - audit logging should not break the request
    _logger.LogWarning(ex, "Failed to log request");
}
```

### 2. عدم تعطيل الطلبات

✅ **فشل Audit Logging لا يوقف الطلب:**
- الطلب يستمر حتى لو فشل التسجيل
- الأخطاء تُسجل في Logger فقط

---

## 📊 الأداء

### 1. التأثير على الأداء

- ⚠️ **تسجيل كل طلب:** قد يؤثر على الأداء في حالة الطلبات الكثيرة
- 💡 **توصية:** في المستقبل، استخدم Background Jobs للتسجيل

### 2. التحسينات المقترحة

1. **استخدام Hangfire Background Jobs:**
   ```csharp
   // بدلاً من:
   await auditService.LogEventAsync(entry);
   
   // استخدم:
   BackgroundJob.Enqueue(() => auditService.LogEventAsync(entry));
   ```

2. **Batch Logging:**
   - جمع عدة طلبات وتسجيلها دفعة واحدة

3. **Filtering:**
   - تخطي الطلبات غير المهمة (مثل polling requests)

---

## 🧪 الاختبار

### 1. اختبار تسجيل الطلبات

1. **شغّل التطبيق**
2. **قم بطلب:**
   ```
   GET /api/documents
   ```
3. **تحقق من `audit_log` table:**
   - يجب أن تجد سجلين: `http_get_start` و `http_get_completed`

---

### 2. اختبار تخطي الملفات الثابتة

1. **قم بطلب:**
   ```
   GET /css/bootstrap.min.css
   ```
2. **تحقق من `audit_log` table:**
   - يجب ألا تجد أي سجل

---

### 3. اختبار تسجيل الأخطاء

1. **قم بطلب غير موجود:**
   ```
   GET /api/documents/99999
   ```
2. **تحقق من `audit_log` table:**
   - يجب أن تجد سجل `http_get_error`

---

## 📋 الخلاصة

✅ **تم إضافة:**
- ✅ `AuditLoggingMiddleware` لتسجيل جميع طلبات HTTP
- ✅ تسجيل تلقائي لبداية واكتمال الطلبات
- ✅ تسجيل الأخطاء
- ✅ تصنيف تلقائي حسب المسار
- ✅ فلترة ذكية للملفات الثابتة
- ✅ استخلاص تلقائي لـ User و IP

✅ **النظام جاهز:**
- ✅ جميع طلبات HTTP تُسجل تلقائياً
- ✅ لا يؤثر على أداء الطلبات
- ✅ معالجة أخطاء آمنة

---

**آخر تحديث:** 2025  
**الحالة:** ✅ جاهز للاستخدام

