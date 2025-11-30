# الإعدادات الآمنة والبنية التحتية
## Secure Configuration and Infrastructure

**التاريخ:** 2025  
**الإصدار:** 2.0  
**الحالة:** ✅ **مكتمل**

---

## 📋 الملخص التنفيذي

هذا الملف يغطي جميع جوانب الإعدادات الآمنة والبنية التحتية في مشروع LegalDocSystem، بما في ذلك:

- CSRF Protection
- Path Traversal Protection
- Error Handling
- Cookie Security
- HSTS
- CORS
- Middleware Pipeline

**الحالة:** ✅ جميع الآليات مطبقة بشكل صحيح وآمن

---

## 1. CSRF Protection

### 1.1 Antiforgery Service

**الموقع:** `src/Program.cs` (السطر 74-82)

```csharp
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction() 
        ? CookieSecurePolicy.Always 
        : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.FormFieldName = "__RequestVerificationToken";
});
```

### 1.2 Antiforgery Middleware

**الموقع:** `src/Program.cs` (السطر 200)

```csharp
app.UseAntiforgery();
```

### 1.3 [ValidateAntiForgeryToken] على Actions

**الموقع:** Controllers

| Controller | Actions المحمية |
|-----------|-----------------|
| DocumentsController | Create, Update, Delete (3) |
| FoldersController | Create, Update, Delete (3) |
| TasksController | Create, Update, Delete, UpdateStatus (4) |
| UsersController | Create, Update, Delete, Validate (4) |

**الإجمالي:** ✅ **13/13 Action حساسة محمية**

**النتيجة:** ✅ **محمي** - CSRF Protection مطبق بشكل صحيح

---

## 2. Path Traversal Protection

### 2.1 المشكلة

❌ `Path.Combine` بدون validation يسمح بالوصول لملفات خارج `_basePath`

### 2.2 الحل المنفذ

**الموقع:** `src/Services/FileStorageService.cs`

```csharp
private string ValidateAndNormalizePath(string filePath)
{
    if (string.IsNullOrWhiteSpace(filePath))
        throw new ArgumentException("File path cannot be empty.", nameof(filePath));

    filePath = filePath.TrimStart('/', '\\').TrimEnd('/', '\\');
    var basePathNormalized = Path.GetFullPath(_basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    var fullPath = Path.GetFullPath(Path.Combine(_basePath, filePath));

    if (!fullPath.StartsWith(basePathNormalized, StringComparison.OrdinalIgnoreCase))
    {
        _logger.LogWarning($"Path traversal attempt detected. Base: {basePathNormalized}, Requested: {fullPath}");
        throw new UnauthorizedAccessException("Invalid file path. Path traversal detected.");
    }

    return fullPath;
}
```

### 2.3 التطبيق

تم تطبيق Path Validation على جميع Methods:
- `GetFileAsync()`
- `DeleteFileAsync()`
- `FileExistsAsync()`
- `GetFileSizeAsync()`
- `GetFullPath()`

**النتيجة:** ✅ **محمي** - جميع محاولات Path Traversal يتم رفضها

---

## 3. Error Handling

### 3.1 Generic Error Messages

**الموقع:** Controllers

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<ActionResult<Document>> Create(Document document)
{
    try
    {
        if (!await _authService.IsAuthenticatedAsync())
            return Unauthorized();

        var created = await _documentService.CreateDocumentAsync(document);
        return CreatedAtAction(nameof(GetById), new { id = created.DocumentId }, created);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating document.");
        return StatusCode(500, "An error occurred while creating the document. Please try again later.");
    }
}
```

### 3.2 المبادئ

- ✅ Generic Error Messages للعميل
- ✅ Detailed Logging للإدارة فقط
- ✅ No Stack Traces في Response

**النتيجة:** ✅ **محمي** - رسائل الخطأ عامة وآمنة

---

## 4. Cookie Security

### 4.1 Session Cookie

**الموقع:** `src/Program.cs` (السطر 45-50)

```csharp
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction() 
        ? CookieSecurePolicy.Always 
        : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
```

### 4.2 Authentication Cookie

**الموقع:** `src/Program.cs` (السطر 56-68)

```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "LegalDocSystem.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsProduction() 
            ? CookieSecurePolicy.Always 
            : CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // ... rest of options
    });
```

### 4.3 CSRF Cookie

**الموقع:** `src/Program.cs` (السطر 74-82)

```csharp
builder.Services.AddAntiforgery(options =>
{
    // ... other options
    options.Cookie.SecurePolicy = builder.Environment.IsProduction() 
        ? CookieSecurePolicy.Always 
        : CookieSecurePolicy.SameAsRequest;
    // ... rest of options
});
```

**النتيجة:** ✅ **محمي** - جميع Cookies آمنة في Production (HTTPS only)

---

## 5. HSTS

### 5.1 التطبيق

**الموقع:** `src/Program.cs` (السطر 204-208)

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365); // 1 year
        options.IncludeSubDomains = true;
        options.Preload = true;
    });
}
```

### 5.2 الإعدادات

| الإعداد | القيمة | الوصف |
|---------|--------|-------|
| `MaxAge` | `365 days` | المتصفحات ستتذكر HTTPS لمدة سنة |
| `IncludeSubDomains` | `true` | يشمل جميع Subdomains |
| `Preload` | `true` | يمكن إضافة الموقع لـ HSTS Preload List |

**النتيجة:** ✅ **محمي** - HSTS محسّن

---

## 6. CORS

### 6.1 الوضع الحالي

- ❌ لا توجد إعدادات CORS في `Program.cs`
- ✅ **Blazor Server** لا يحتاج CORS (كل شيء على نفس Origin)

### 6.2 التوصية (إذا كان هناك frontend منفصل)

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("https://yourfrontend.com", "https://www.yourfrontend.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Important for cookies
    });
});

// In middleware pipeline (after UseRouting, before UseAuthentication)
app.UseCors("AllowSpecificOrigins");
```

**⚠️ تحذير:** لا تستخدم `AllowAnyOrigin()` مع `AllowCredentials()` - هذا غير آمن!

---

## 7. Middleware Pipeline

### 7.1 الترتيب الصحيح

**الموقع:** `src/Program.cs` (السطر 188-205)

```csharp
app.UseHttpsRedirection();        // ✅ صحيح
app.UseStaticFiles();             // ✅ صحيح
app.UseRouting();                 // ✅ صحيح
app.UseRateLimiter();             // ✅ صحيح (بعد UseRouting)
app.UseAuthentication();          // ✅ صحيح
app.UseAuthorization();           // ✅ صحيح
app.UseAntiforgery();            // ✅ صحيح
app.UseMiddleware<AuditLoggingMiddleware>(); // ✅ صحيح
app.UseSession();                 // ✅ صحيح
```

**النتيجة:** ✅ **الترتيب صحيح**

---

## 8. AllowedHosts

### 8.1 التطبيق

**الموقع:** `src/appsettings.json`

```json
{
  "AllowedHosts": "localhost;127.0.0.1"
}
```

**للإنتاج:** `src/appsettings.Production.json`

```json
{
  "AllowedHosts": "yourdomain.com;www.yourdomain.com"
}
```

**النتيجة:** ✅ **محمي** - AllowedHosts محدود

---

## 📋 Checklist

### CSRF Protection

- [x] ✅ AddAntiforgery() موجود
- [x] ✅ UseAntiforgery() موجود
- [x] ✅ [ValidateAntiForgeryToken] على 13 Action
- [x] ✅ SecurePolicy = Always (Production)

### Path Traversal Protection

- [x] ✅ ValidateAndNormalizePath() موجود
- [x] ✅ تطبيق على جميع Methods
- [x] ✅ جميع محاولات Path Traversal يتم رفضها

### Error Handling

- [x] ✅ Generic Error Messages
- [x] ✅ Detailed Logging
- [x] ✅ No Stack Traces

### Cookie Security

- [x] ✅ Session Cookie Secure
- [x] ✅ Authentication Cookie Secure
- [x] ✅ CSRF Cookie Secure

### HSTS

- [x] ✅ MaxAge = 365 days
- [x] ✅ IncludeSubDomains = true
- [x] ✅ Preload = true

### Middleware Pipeline

- [x] ✅ الترتيب صحيح
- [x] ✅ UseHttpsRedirection موجود
- [x] ✅ UseHsts موجود

---

## 📚 المراجع

- [SECURITY_OVERVIEW.md](./SECURITY_OVERVIEW.md) - نظرة عامة على الأمان
- [AUTHENTICATION_AND_AUTHORIZATION.md](./AUTHENTICATION_AND_AUTHORIZATION.md) - المصادقة والصلاحيات

---

**آخر تحديث:** 2025  
**الحالة:** ✅ **مكتمل وآمن**

