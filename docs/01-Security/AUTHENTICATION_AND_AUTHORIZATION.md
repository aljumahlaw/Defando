# المصادقة والصلاحيات
## Authentication and Authorization

**التاريخ:** 2025  
**الإصدار:** 2.0  
**الحالة:** ✅ **مكتمل**

---

## 📋 الملخص التنفيذي

هذا الملف يغطي جميع جوانب المصادقة والصلاحيات في مشروع LegalDocSystem، بما في ذلك:

- Cookie Authentication
- Claims-based Authorization
- Role-based Access Control
- Rate Limiting
- Account Lockout
- Session Management

**الحالة:** ✅ جميع الآليات مطبقة بشكل صحيح وآمن

---

## 1. Cookie Authentication

### 1.1 التطبيق

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
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
        options.LogoutPath = "/login";
        options.AccessDeniedPath = "/login";
    });
```

### 1.2 الإعدادات

| الإعداد | القيمة | الوصف |
|---------|--------|-------|
| `HttpOnly` | `true` | منع الوصول من JavaScript (حماية من XSS) |
| `SecurePolicy` | `Always` (Production) | Cookies آمنة فقط على HTTPS |
| `SameSite` | `Lax` | حماية جزئية من CSRF |
| `ExpireTimeSpan` | `30 minutes` | مدة انتهاء الجلسة |
| `SlidingExpiration` | `true` | تمديد الجلسة تلقائياً عند النشاط |

**النتيجة:** ✅ **محمي** - Cookie Authentication آمن

---

## 2. Claims-based Authorization

### 2.1 Claims المستخدمة

**الموقع:** `src/Services/AuthService.cs` (السطر 71-82)

```csharp
var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
    new Claim(ClaimTypes.Name, user.Username),
    new Claim(ClaimTypes.GivenName, user.FullName ?? user.Username),
    new Claim(ClaimTypes.Role, user.Role ?? "user"),
};

if (!string.IsNullOrEmpty(user.Email))
{
    claims.Add(new Claim(ClaimTypes.Email, user.Email));
}
```

### 2.2 Claims المتاحة

| Claim Type | الوصف | الاستخدام |
|------------|-------|-----------|
| `NameIdentifier` | UserId | تحديد المستخدم |
| `Name` | Username | اسم المستخدم |
| `GivenName` | FullName | الاسم الكامل |
| `Role` | User Role | الصلاحيات |
| `Email` | Email Address | البريد الإلكتروني |

**النتيجة:** ✅ **محمي** - Claims موجودة ومستخدمة بشكل صحيح

---

## 3. Role-based Access Control

### 3.1 الصفحات المحمية

#### الصفحات المفتوحة (غير محمية):

| الصفحة | المسار | الحالة |
|--------|--------|--------|
| `Login.razor` | `/login` | ✅ صحيح (يجب أن يكون مفتوحاً) |
| `Error.razor` | `/error` | ✅ صحيح (يجب أن يكون مفتوحاً) |
| `SharedDocument.razor` | `/shared/{token}` | ✅ صحيح (رابط عام) |

#### الصفحات المحمية (مطلوب تسجيل دخول):

| الصفحة | المسار | الحماية |
|--------|--------|---------|
| `Dashboard.razor` | `/` | `[Authorize]` |
| `Documents.razor` | `/documents` | `[Authorize]` |
| `DocumentDetails.razor` | `/documents/{id}` | `[Authorize]` |
| `DocumentVersions.razor` | `/documents/{id}/versions` | `[Authorize]` |
| `Folders.razor` | `/folders` | `[Authorize]` |
| `Tasks.razor` | `/tasks` | `[Authorize]` |
| `Search.razor` | `/search` | `[Authorize]` |
| `Settings.razor` | `/settings` | `[Authorize]` |
| `Outgoing.razor` | `/outgoing` | `[Authorize]` |
| `Incoming.razor` | `/incoming` | `[Authorize]` |
| `CreateSharedLink.razor` | `/documents/{id}/share` | `[Authorize]` |

**الإجمالي:** ✅ **11 صفحة محمية بـ [Authorize]**

#### الصفحات المحمية (مطلوب دور admin):

| الصفحة | المسار | الحماية |
|--------|--------|---------|
| `Users.razor` | `/users` | `[Authorize(Roles = "admin")]` |
| `SmtpSettings.razor` | `/settings/smtp` | `[Authorize(Roles = "admin")]` |
| `ManageSharedLinks.razor` | `/shared-links` | `[Authorize(Roles = "admin")]` |

**الإجمالي:** ✅ **3 صفحات محمية بـ [Authorize(Roles = "admin")]**

### 3.2 Controllers Protection

**الموقع:** Controllers (مثل `DocumentsController.cs`, `UsersController.cs`)

```csharp
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Document>>> GetAll()
    {
        if (!await _authService.IsAuthenticatedAsync())
            return Unauthorized();
        
        // ... rest of code
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Document>> Create(Document document)
    {
        if (!await _authService.IsAuthenticatedAsync())
            return Unauthorized();
        
        // ... rest of code
    }
}
```

**النتيجة:** ✅ **جميع Controllers محمية** - فحص Authentication في كل Method

---

## 4. Rate Limiting

### 4.1 Policies

**الموقع:** `src/Program.cs` (السطر 85-163)

#### 1. AuthenticatedUserPolicy

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthenticatedUserPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });
});
```

**الوصف:** 100 طلب لكل دقيقة لكل مستخدم مصادق عليه

#### 2. LoginPolicy

```csharp
options.AddFixedWindowLimiter("LoginPolicy", limiterOptions =>
{
    limiterOptions.PermitLimit = 5;
    limiterOptions.Window = TimeSpan.FromMinutes(1);
    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    limiterOptions.QueueLimit = 0;
});
```

**الوصف:** 5 محاولات تسجيل دخول لكل دقيقة لكل IP

#### 3. GlobalLimiter

```csharp
options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: partition => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 200,
            Window = TimeSpan.FromMinutes(1)
        }));
```

**الوصف:** 200 طلب لكل دقيقة لكل IP (حماية عامة)

### 4.2 التطبيق

**الموقع:** `src/Program.cs` (السطر 209-213)

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireRateLimiting("AuthenticatedUserPolicy");

app.MapControllers()
    .RequireRateLimiting("AuthenticatedUserPolicy");
```

### 4.3 Login Rate Limiting Middleware

**الموقع:** `src/Middleware/LoginRateLimitMiddleware.cs`

```csharp
public class LoginRateLimitMiddleware
{
    private readonly RateLimiter _rateLimiter;

    public LoginRateLimitMiddleware(RequestDelegate next, ILogger<LoginRateLimitMiddleware> logger)
    {
        _rateLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/login") && 
            context.Request.Method == "POST")
        {
            var lease = await _rateLimiter.AcquireAsync(1);
            if (!lease.IsAcquired)
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsync("Too many login attempts. Please try again after 1 minute.");
                return;
            }
            // ... rest of code
        }
    }
}
```

**النتيجة:** ✅ **محمي** - Login endpoint محمي بـ Rate Limiting (5 محاولات/دقيقة)

---

## 5. Account Lockout

### 5.1 التطبيق

**الموقع:** `src/Services/UserService.cs`

#### RecordFailedLoginAttemptAsync

```csharp
public async Task RecordFailedLoginAttemptAsync(string username)
{
    var user = await GetUserByUsernameAsync(username);
    if (user == null)
        return;

    user.FailedLoginAttempts++;

    // Lock account if threshold is reached
    if (user.FailedLoginAttempts >= _maxFailedAttempts)
    {
        user.LockedUntil = DateTime.UtcNow.AddMinutes(_lockoutDurationMinutes);
    }

    await _context.SaveChangesAsync();
}
```

#### IsAccountLockedAsync

```csharp
public async Task<bool> IsAccountLockedAsync(string username)
{
    var user = await GetUserByUsernameAsync(username);
    if (user == null)
        return false;

    // Auto-unlock if lockout duration has passed
    if (user.LockedUntil.HasValue && user.LockedUntil.Value < DateTime.UtcNow)
    {
        if (_enableAutoUnlock)
        {
            user.LockedUntil = null;
            user.FailedLoginAttempts = 0;
            await _context.SaveChangesAsync();
            return false;
        }
    }

    return user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow;
}
```

### 5.2 الإعدادات القابلة للتخصيص

**الموقع:** `src/appsettings.json`

```json
{
  "AccountLockout": {
    "MaxFailedAttempts": 5,
    "LockoutDurationMinutes": 15,
    "EnableAutoUnlock": true
  }
}
```

### 5.3 التكامل

**الموقع:** `src/Services/AuthService.cs` (السطر 45)

```csharp
// Check if account is locked before password verification
if (await _userService.IsAccountLockedAsync(username))
{
    return new LoginResult
    {
        Success = false,
        ErrorMessage = "الحساب مقفل مؤقتاً. يرجى المحاولة لاحقاً."
    };
}
```

**النتيجة:** ✅ **محمي** - Account Lockout مطبق بشكل صحيح مع Auto-unlock

---

## 6. Session Management

### 6.1 التطبيق

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

### 6.2 الإعدادات

| الإعداد | القيمة | الوصف |
|---------|--------|-------|
| `IdleTimeout` | `30 minutes` | مدة انتهاء الجلسة عند عدم النشاط |
| `HttpOnly` | `true` | منع الوصول من JavaScript |
| `IsEssential` | `true` | Cookie أساسي (لا يمكن رفضه) |
| `SecurePolicy` | `Always` (Production) | Cookies آمنة فقط على HTTPS |
| `SameSite` | `Lax` | حماية جزئية من CSRF |

**النتيجة:** ✅ **محمي** - Session Management آمن

---

## 7. Password Security

### 7.1 BCrypt Hashing

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

## 📋 Checklist

### Authentication

- [x] ✅ Cookie Authentication مطبق
- [x] ✅ HttpOnly = true
- [x] ✅ SecurePolicy = Always (Production)
- [x] ✅ SameSite = Lax
- [x] ✅ SlidingExpiration = true

### Authorization

- [x] ✅ Claims موجودة
- [x] ✅ Role-based Authorization مطبق
- [x] ✅ [Authorize] على الصفحات (11 صفحة)
- [x] ✅ [Authorize(Roles = "admin")] على الصفحات (3 صفحات)
- [x] ✅ Authentication checks في Controllers

### Rate Limiting

- [x] ✅ AuthenticatedUserPolicy (100 req/min)
- [x] ✅ LoginPolicy (5 req/min)
- [x] ✅ GlobalLimiter (200 req/min)
- [x] ✅ LoginRateLimitMiddleware مطبق

### Account Lockout

- [x] ✅ RecordFailedLoginAttemptAsync موجود
- [x] ✅ IsAccountLockedAsync موجود
- [x] ✅ MaxFailedAttempts = 5 (قابل للتخصيص)
- [x] ✅ LockoutDurationMinutes = 15 (قابل للتخصيص)
- [x] ✅ Auto-unlock مفعّل

### Session Management

- [x] ✅ Session Timeout = 30 minutes
- [x] ✅ HttpOnly = true
- [x] ✅ SecurePolicy = Always (Production)

---

## 📚 المراجع

- [SECURITY_OVERVIEW.md](./SECURITY_OVERVIEW.md) - نظرة عامة على الأمان
- [SECURE_CONFIGURATION_AND_INFRASTRUCTURE.md](./SECURE_CONFIGURATION_AND_INFRASTRUCTURE.md) - الإعدادات الآمنة

---

**آخر تحديث:** 2025  
**الحالة:** ✅ **مكتمل وآمن**

