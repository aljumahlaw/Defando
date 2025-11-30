# دليل تنفيذ نظام Audit Logging
## Audit Logging Service Implementation Guide

**التاريخ:** 2025  
**الإصدار:** 1.0

---

## 📋 نظرة عامة

تم إنشاء نظام Audit Logging كامل يتكون من:

1. ✅ **IAuditService** - واجهة الخدمة
2. ✅ **AuditService** - تنفيذ الخدمة
3. ✅ **AuditLogEntry** - DTO للبيانات
4. ✅ **تسجيل الخدمة** في `Program.cs`

---

## 📁 الملفات المنشأة

### 1. `src/Models/AuditLogEntry.cs`

**نوع الملف:** DTO (Data Transfer Object)  
**الوصف:** نموذج بيانات يستخدم في Service layer لإنشاء سجلات التدقيق.

**الحقول:**
- `Event` - نوع الحدث (Login, Logout, Create, Update, Delete)
- `Category` - فئة الحدث (Authentication, Document, User, etc.)
- `Action` - العمل المحدد (login_success, create_document, etc.)
- `SubjectIdentifier` - معرف المستخدم
- `SubjectName` - اسم المستخدم
- `SubjectType` - نوع المستخدم (User, System)
- `EntityType` - نوع الكيان المتأثر
- `EntityId` - معرف الكيان المتأثر
- `Data` - بيانات إضافية
- `IpAddress` - عنوان IP
- `UserAgent` - معلومات المتصفح/العميل
- `Created` - وقت الحدث

---

### 2. `src/Services/IAuditService.cs`

**نوع الملف:** Interface  
**الوصف:** واجهة الخدمة لعمليات Audit Logging.

**الدوال:**
- `LogEventAsync(AuditLogEntry entry)` - تسجيل حدث عام
- `LogLoginAsync(...)` - تسجيل تسجيل دخول
- `LogLogoutAsync(...)` - تسجيل تسجيل خروج
- `LogCreateAsync(...)` - تسجيل إنشاء كيان
- `LogUpdateAsync(...)` - تسجيل تعديل كيان
- `LogDeleteAsync(...)` - تسجيل حذف كيان
- `GetLogsAsync(...)` - استرجاع السجلات مع فلترة

---

### 3. `src/Services/AuditService.cs`

**نوع الملف:** Service Implementation  
**الوصف:** تنفيذ الخدمة لعمليات Audit Logging.

**الميزات:**
- ✅ استخدام `IHttpContextAccessor` لاستخلاص User و IP
- ✅ تسجيل غير متزامن (Async)
- ✅ معالجة أخطاء (لا يوقف التطبيق عند فشل التسجيل)
- ✅ دعم تسجيل أحداث متنوعة
- ✅ استخلاص تلقائي لـ User Claims (UserId, Username, Role)
- ✅ استخلاص تلقائي لـ IP Address (يدعم X-Forwarded-For, X-Real-IP)
- ✅ استخلاص تلقائي لـ User Agent

**الدوال المساعدة:**
- `ExtractUserInfo()` - استخلاص معلومات المستخدم من Claims
- `GetClientIpAddress()` - استخلاص IP Address
- `GetUserAgent()` - استخلاص User Agent
- `FormatDetails()` - تنسيق التفاصيل للتخزين

---

## 🔧 التعديلات على الملفات الموجودة

### 1. `src/Program.cs`

**التعديل:**
```csharp
// Register Services
builder.Services.AddScoped<IAuditService, AuditService>();
```

**الموقع:** بعد تسجيل `IAuthService`

---

## 📝 أمثلة الاستخدام

### مثال 1: تسجيل تسجيل دخول

```csharp
public class AuthService : IAuthService
{
    private readonly IAuditService _auditService;

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _userService.GetUserByUsernameAsync(username);
        
        if (user != null && await _userService.ValidatePasswordAsync(username, password))
        {
            // تسجيل تسجيل دخول ناجح
            await _auditService.LogLoginAsync(
                userId: user.UserId,
                username: user.Username,
                success: true,
                additionalData: $"Login successful from {GetClientIpAddress()}"
            );
            
            return user;
        }
        else
        {
            // تسجيل تسجيل دخول فاشل
            await _auditService.LogLoginAsync(
                userId: null,
                username: username,
                success: false,
                additionalData: "Invalid credentials"
            );
            
            return null;
        }
    }
}
```

---

### مثال 2: تسجيل إنشاء مستند

```csharp
public class DocumentService : IDocumentService
{
    private readonly IAuditService _auditService;

    public async Task<Document> CreateDocumentAsync(Document document)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // تسجيل إنشاء المستند
        await _auditService.LogCreateAsync(
            entityType: "Document",
            entityId: document.DocumentId,
            additionalData: $"Document: {document.DocumentName}, Type: {document.DocumentType}"
        );

        return document;
    }
}
```

---

### مثال 3: تسجيل تعديل مستند

```csharp
public async Task UpdateDocumentAsync(Document document)
{
    var oldDocument = await _context.Documents.FindAsync(document.DocumentId);
    
    _context.Documents.Update(document);
    await _context.SaveChangesAsync();

    // تسجيل تعديل المستند
    await _auditService.LogUpdateAsync(
        entityType: "Document",
        entityId: document.DocumentId,
        additionalData: $"Updated fields: Name, Type, Status"
    );
}
```

---

### مثال 4: تسجيل حذف مستند

```csharp
public async Task DeleteDocumentAsync(int id)
{
    var document = await _context.Documents.FindAsync(id);
    
    if (document != null)
    {
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        // تسجيل حذف المستند
        await _auditService.LogDeleteAsync(
            entityType: "Document",
            entityId: id,
            additionalData: $"Deleted document: {document.DocumentName}"
        );
    }
}
```

---

### مثال 5: تسجيل حدث مخصص

```csharp
var entry = new AuditLogEntry
{
    Event = "CheckOut",
    Category = "Document",
    Action = "checkout_document",
    EntityType = "Document",
    EntityId = documentId,
    Data = $"Document checked out by user {userId}",
    Created = DateTime.UtcNow
};

await _auditService.LogEventAsync(entry);
```

---

### مثال 6: استرجاع السجلات

```csharp
// استرجاع جميع سجلات مستخدم معين
var userLogs = await _auditService.GetLogsAsync(
    userId: 123,
    skip: 0,
    take: 50
);

// استرجاع سجلات إنشاء مستندات في آخر 7 أيام
var documentLogs = await _auditService.GetLogsAsync(
    action: "create_document",
    entityType: "Document",
    startDate: DateTime.UtcNow.AddDays(-7),
    endDate: DateTime.UtcNow
);
```

---

## 🔍 كيفية عمل النظام

### 1. استخلاص معلومات المستخدم

عند استدعاء `LogEventAsync()` بدون `SubjectIdentifier` أو `SubjectName`، يقوم النظام تلقائياً بـ:

1. التحقق من `HttpContext.User.Identity.IsAuthenticated`
2. استخلاص `UserId` من `ClaimTypes.NameIdentifier`
3. استخلاص `Username` من `ClaimTypes.Name`
4. استخلاص `Role` من `ClaimTypes.Role`

### 2. استخلاص IP Address

النظام يحاول استخلاص IP Address بالترتيب التالي:

1. `X-Forwarded-For` header (للـ Load Balancers/Proxies)
2. `X-Real-IP` header
3. `Connection.RemoteIpAddress` (fallback)

### 3. استخلاص User Agent

يتم استخلاص User Agent من `Request.Headers["User-Agent"]`.

### 4. حفظ السجل

يتم تحويل `AuditLogEntry` (DTO) إلى `AuditLog` (Entity) وحفظه في قاعدة البيانات.

---

## ⚠️ ملاحظات مهمة

### 1. معالجة الأخطاء

- ✅ التسجيل لا يوقف التطبيق عند فشله
- ✅ الأخطاء تُسجل في Console (في الإنتاج، استخدم Serilog أو NLog)
- ⚠️ في الإنتاج، فكر في استخدام Background Jobs للتسجيل

### 2. الأداء

- ✅ التسجيل غير متزامن (Async)
- ⚠️ التسجيل يتم داخل نفس Transaction - قد يؤثر على الأداء
- 💡 **توصية:** في المستقبل، استخدم Hangfire Background Jobs للتسجيل

### 3. الأمان

- ✅ لا يتم تسجيل كلمات المرور أو بيانات حساسة
- ✅ IP Address و User Agent تُسجل للتحليل الأمني
- ⚠️ تأكد من عدم تسجيل بيانات حساسة في `Data` field

---

## 📊 البيانات المخزنة

### مثال على سجل محفوظ:

```json
{
  "log_id": 1,
  "user_id": 123,
  "action": "login_success",
  "entity_type": "Authentication",
  "entity_id": null,
  "details": "Event: Login | Category: Authentication | Subject: admin | SubjectType: admin | Data: Login successful | UserAgent: Mozilla/5.0...",
  "ip_address": "192.168.1.100",
  "created_at": "2025-01-15T10:30:00Z"
}
```

---

## ✅ الخطوات التالية

1. **إضافة تسجيل في الأحداث الحرجة:**
   - ✅ تسجيل الدخول/الخروج (في `AuthService`)
   - ⚠️ إنشاء/تعديل/حذف المستندات (في `DocumentService`)
   - ⚠️ إنشاء/تعديل/حذف المجلدات (في `FolderService`)
   - ⚠️ إنشاء/تعديل/حذف المهام (في `TaskService`)
   - ⚠️ إنشاء/حذف الروابط المشاركة (في `SharedLinkService`)

2. **تحسين الأداء:**
   - استخدام Hangfire Background Jobs للتسجيل غير المتزامن
   - استخدام Queue للتسجيل

3. **إضافة ميزات:**
   - إضافة صفحة Blazor لعرض السجلات
   - إضافة فلترة متقدمة
   - إضافة تصدير السجلات (PDF, Excel)

---

**آخر تحديث:** 2025  
**الحالة:** ✅ جاهز للاستخدام

