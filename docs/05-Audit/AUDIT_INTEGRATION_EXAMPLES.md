# أمثلة دمج Audit Logging في الخدمات
## Audit Logging Integration Examples

**التاريخ:** 2025  
**الإصدار:** 1.0

---

## 📋 نظرة عامة

تم دمج نظام Audit Logging في الخدمات الحرجة التالية:

1. ✅ **AuthService** - تسجيل الدخول/الخروج، فشل الدخول، Account Lockout
2. ✅ **DocumentService** - إنشاء، تعديل، حذف مستند
3. ✅ **SharedLinkService** - إنشاء رابط، حذف رابط
4. ✅ **UserService** - إضافة مستخدم

---

## 1. AuthService - مثال شامل

### 1.1 تسجيل الدخول الناجح

```csharp
public async Task<User?> LoginAsync(string username, string password)
{
    try
    {
        var user = await _userService.GetUserByUsernameAsync(username);
        
        if (user == null || !user.IsActive)
        {
            // Log failed login attempt (user not found or inactive)
            await LogFailedLoginAsync(username, "User not found or inactive");
            return null;
        }

        // Check if account is locked
        if (await _userService.IsAccountLockedAsync(username))
        {
            // Log account lockout attempt
            await LogAccountLockoutAttemptAsync(user.UserId, username);
            return null;
        }

        var isValid = await _userService.ValidatePasswordAsync(username, password);
        
        if (!isValid)
        {
            // Log failed login attempt (invalid password)
            await LogFailedLoginAsync(username, "Invalid password");
            return null;
        }

        // ... authentication logic ...

        // Log successful login
        await LogSuccessfulLoginAsync(user.UserId, user.Username);

        return user;
    }
    catch (Exception)
    {
        return null;
    }
}

private async Task LogSuccessfulLoginAsync(int userId, string username)
{
    try
    {
        await _auditService.LogLoginAsync(
            userId: userId,
            username: username,
            success: true,
            additionalData: $"Login successful from IP: {GetClientIpAddress()}"
        );
    }
    catch (Exception)
    {
        // Don't throw - audit logging should not break the login process
    }
}
```

**البيانات المسجلة:**
- Event: "Login"
- Category: "Authentication"
- Action: "login_success"
- SubjectIdentifier: UserId
- SubjectName: Username
- Data: "Login successful from IP: 192.168.1.100"

---

### 1.2 تسجيل الدخول الفاشل

```csharp
private async Task LogFailedLoginAsync(string username, string reason)
{
    try
    {
        await _auditService.LogLoginAsync(
            userId: null,
            username: username,
            success: false,
            additionalData: $"Login failed: {reason}, IP: {GetClientIpAddress()}"
        );
    }
    catch (Exception)
    {
        // Don't throw - audit logging should not break the login process
    }
}
```

**البيانات المسجلة:**
- Event: "Login"
- Category: "Authentication"
- Action: "login_failed"
- SubjectIdentifier: null
- SubjectName: Username (المحاولة)
- Data: "Login failed: Invalid password, IP: 192.168.1.100"

---

### 1.3 Account Lockout

```csharp
private async Task LogAccountLockoutAttemptAsync(int userId, string username)
{
    try
    {
        var entry = new AuditLogEntry
        {
            Event = "AccountLockout",
            Category = "Authentication",
            Action = "account_locked_attempt",
            SubjectIdentifier = userId,
            SubjectName = username,
            SubjectType = "User",
            Data = $"Account is locked. Login attempt blocked. IP: {GetClientIpAddress()}",
            Created = DateTime.UtcNow
        };

        await _auditService.LogEventAsync(entry);
    }
    catch (Exception)
    {
        // Don't throw - audit logging should not break the login process
    }
}
```

**البيانات المسجلة:**
- Event: "AccountLockout"
- Category: "Authentication"
- Action: "account_locked_attempt"
- SubjectIdentifier: UserId
- SubjectName: Username
- Data: "Account is locked. Login attempt blocked. IP: 192.168.1.100"

---

### 1.4 تسجيل الخروج

```csharp
public async Task LogoutAsync()
{
    var httpContext = _httpContextAccessor.HttpContext;
    if (httpContext != null)
    {
        // Get user info before clearing session
        var userId = GetCurrentUserId();
        var username = _httpContextAccessor.HttpContext?.Session?.GetString(UsernameSessionKey);

        // Sign out from Cookie Authentication
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Clear session
        var session = httpContext.Session;
        if (session != null)
        {
            session.Clear();
        }

        // Log logout
        if (userId.HasValue && !string.IsNullOrEmpty(username))
        {
            await LogLogoutAsync(userId.Value, username);
        }
    }

    await Task.CompletedTask;
}

private async Task LogLogoutAsync(int userId, string username)
{
    try
    {
        await _auditService.LogLogoutAsync(
            userId: userId,
            username: username
        );
    }
    catch (Exception)
    {
        // Don't throw - audit logging should not break the logout process
    }
}
```

**البيانات المسجلة:**
- Event: "Logout"
- Category: "Authentication"
- Action: "logout"
- SubjectIdentifier: UserId
- SubjectName: Username
- Data: "User logged out"

---

## 2. DocumentService - مثال شامل

### 2.1 إنشاء مستند

```csharp
public async Task<Document> CreateDocumentAsync(Document document)
{
    _context.Documents.Add(document);
    await _context.SaveChangesAsync();

    // Log document creation
    try
    {
        await _auditService.LogCreateAsync(
            entityType: "Document",
            entityId: document.DocumentId,
            additionalData: $"Document Name: {document.DocumentName}, Type: {document.DocumentType}, Folder: {document.FolderId}"
        );
    }
    catch (Exception)
    {
        // Don't throw - audit logging should not break document creation
    }

    return document;
}
```

**البيانات المسجلة:**
- Event: "Create"
- Category: "Document"
- Action: "create_document"
- EntityType: "Document"
- EntityId: DocumentId
- Data: "Document Name: Contract_2025.pdf, Type: contract, Folder: 5"

---

### 2.2 تعديل مستند

```csharp
public async Task UpdateDocumentAsync(Document document)
{
    // Get old document data for audit log
    var oldDocument = await _context.Documents
        .AsNoTracking()
        .FirstOrDefaultAsync(d => d.DocumentId == document.DocumentId);

    _context.Documents.Update(document);
    await _context.SaveChangesAsync();

    // Log document update
    try
    {
        var changes = new List<string>();
        if (oldDocument != null)
        {
            if (oldDocument.DocumentName != document.DocumentName)
                changes.Add($"Name: {oldDocument.DocumentName} -> {document.DocumentName}");
            if (oldDocument.DocumentType != document.DocumentType)
                changes.Add($"Type: {oldDocument.DocumentType} -> {document.DocumentType}");
            if (oldDocument.Status != document.Status)
                changes.Add($"Status: {oldDocument.Status} -> {document.Status}");
        }

        await _auditService.LogUpdateAsync(
            entityType: "Document",
            entityId: document.DocumentId,
            additionalData: changes.Count > 0 
                ? $"Updated fields: {string.Join(", ", changes)}" 
                : "Document updated"
        );
    }
    catch (Exception)
    {
        // Don't throw - audit logging should not break document update
    }
}
```

**البيانات المسجلة:**
- Event: "Update"
- Category: "Document"
- Action: "update_document"
- EntityType: "Document"
- EntityId: DocumentId
- Data: "Updated fields: Name: Contract_2024.pdf -> Contract_2025.pdf, Status: draft -> final"

---

### 2.3 حذف مستند

```csharp
public async Task DeleteDocumentAsync(int id)
{
    var document = await _context.Documents.FindAsync(id);
    if (document != null)
    {
        var documentName = document.DocumentName;
        var documentType = document.DocumentType;
        var folderId = document.FolderId;

        // Delete physical file from storage
        if (!string.IsNullOrEmpty(document.FilePath))
        {
            try
            {
                await _fileStorageService.DeleteFileAsync(document.FilePath);
            }
            catch (Exception ex)
            {
                // Log file deletion error
                try
                {
                    await _auditService.LogEventAsync(new AuditLogEntry
                    {
                        Event = "Delete",
                        Category = "Document",
                        Action = "delete_document_file_error",
                        EntityType = "Document",
                        EntityId = id,
                        Data = $"Error deleting file: {ex.Message}, FilePath: {document.FilePath}",
                        Created = DateTime.UtcNow
                    });
                }
                catch
                {
                    // Ignore audit logging errors
                }
            }
        }

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        // Log document deletion
        try
        {
            await _auditService.LogDeleteAsync(
                entityType: "Document",
                entityId: id,
                additionalData: $"Deleted document: {documentName}, Type: {documentType}, Folder: {folderId}"
            );
        }
        catch (Exception)
        {
            // Don't throw - audit logging should not break document deletion
        }
    }
}
```

**البيانات المسجلة:**
- Event: "Delete"
- Category: "Document"
- Action: "delete_document"
- EntityType: "Document"
- EntityId: DocumentId
- Data: "Deleted document: Contract_2025.pdf, Type: contract, Folder: 5"

---

## 3. SharedLinkService - مثال شامل

### 3.1 إنشاء رابط مشاركة

```csharp
public async Task<SharedLink> CreateSharedLinkAsync(
    int documentId,
    int createdBy,
    DateTime expiresAt,
    int? maxAccessCount = null,
    string? password = null)
{
    // Verify document exists
    var document = await _context.Documents.FindAsync(documentId);
    if (document == null)
        throw new ArgumentException("Document not found", nameof(documentId));

    // ... create link logic ...

    _context.SharedLinks.Add(sharedLink);
    await _context.SaveChangesAsync();

    // Log shared link creation
    try
    {
        await _auditService.LogCreateAsync(
            entityType: "SharedLink",
            entityId: sharedLink.LinkId,
            additionalData: $"Document: {document.DocumentName} (ID: {documentId}), Expires: {expiresAt:yyyy-MM-dd HH:mm}, MaxAccess: {maxAccessCount?.ToString() ?? "Unlimited"}, PasswordProtected: {!string.IsNullOrEmpty(password)}"
        );
    }
    catch (Exception)
    {
        // Don't throw - audit logging should not break link creation
    }

    return sharedLink;
}
```

**البيانات المسجلة:**
- Event: "Create"
- Category: "SharedLink"
- Action: "create_sharedlink"
- EntityType: "SharedLink"
- EntityId: LinkId
- Data: "Document: Contract_2025.pdf (ID: 123), Expires: 2025-02-15 10:30, MaxAccess: 10, PasswordProtected: True"

---

### 3.2 حذف رابط مشاركة

```csharp
public async Task DeleteLinkAsync(int linkId)
{
    var link = await _context.SharedLinks
        .Include(l => l.Document)
        .FirstOrDefaultAsync(l => l.LinkId == linkId);

    if (link != null)
    {
        var documentName = link.Document?.DocumentName ?? "Unknown";
        var documentId = link.DocumentId;
        var accessCount = link.CurrentAccessCount;

        _context.SharedLinks.Remove(link);
        await _context.SaveChangesAsync();

        // Log shared link deletion
        try
        {
            await _auditService.LogDeleteAsync(
                entityType: "SharedLink",
                entityId: linkId,
                additionalData: $"Deleted link for document: {documentName} (ID: {documentId}), Access count: {accessCount}"
            );
        }
        catch (Exception)
        {
            // Don't throw - audit logging should not break link deletion
        }
    }
}
```

**البيانات المسجلة:**
- Event: "Delete"
- Category: "SharedLink"
- Action: "delete_sharedlink"
- EntityType: "SharedLink"
- EntityId: LinkId
- Data: "Deleted link for document: Contract_2025.pdf (ID: 123), Access count: 5"

---

## 4. UserService - مثال شامل

### 4.1 إنشاء مستخدم

```csharp
public async Task<User> CreateUserAsync(User user)
{
    // Hash password before saving
    if (!string.IsNullOrEmpty(user.PasswordHash) && 
        !user.PasswordHash.StartsWith("$2"))
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
    }

    user.CreatedAt = DateTime.UtcNow;
    user.IsActive = true;

    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    // Log user creation
    try
    {
        await _auditService.LogCreateAsync(
            entityType: "User",
            entityId: user.UserId,
            additionalData: $"Username: {user.Username}, FullName: {user.FullName}, Role: {user.Role}, Email: {user.Email ?? "N/A"}"
        );
    }
    catch (Exception)
    {
        // Don't throw - audit logging should not break user creation
    }

    return user;
}
```

**البيانات المسجلة:**
- Event: "Create"
- Category: "User"
- Action: "create_user"
- EntityType: "User"
- EntityId: UserId
- Data: "Username: john.doe, FullName: John Doe, Role: senior_lawyer, Email: john.doe@example.com"

---

## 🔑 المبادئ المهمة

### 1. معالجة الأخطاء

✅ **صحيح:**
```csharp
try
{
    await _auditService.LogCreateAsync(...);
}
catch (Exception)
{
    // Don't throw - audit logging should not break the operation
}
```

❌ **خطأ:**
```csharp
await _auditService.LogCreateAsync(...); // قد يوقف العملية عند فشل التسجيل
```

---

### 2. استخلاص معلومات المستخدم

✅ **صحيح:**
- استخدام `IHttpContextAccessor` لاستخلاص User Claims
- `AuditService` يستخلص تلقائياً User ID, Username, IP Address, User Agent

---

### 3. البيانات التفصيلية

✅ **صحيح:**
```csharp
additionalData: $"Document Name: {document.DocumentName}, Type: {document.DocumentType}, Folder: {document.FolderId}"
```

❌ **خطأ:**
```csharp
additionalData: "Document created" // غير مفيد
```

---

## 📊 ملخص الأحداث المسجلة

| الخدمة | الحدث | Action | EntityType |
|---|---|---|---|
| **AuthService** | Login Success | `login_success` | Authentication |
| **AuthService** | Login Failed | `login_failed` | Authentication |
| **AuthService** | Account Lockout | `account_locked_attempt` | Authentication |
| **AuthService** | Logout | `logout` | Authentication |
| **DocumentService** | Create Document | `create_document` | Document |
| **DocumentService** | Update Document | `update_document` | Document |
| **DocumentService** | Delete Document | `delete_document` | Document |
| **SharedLinkService** | Create Link | `create_sharedlink` | SharedLink |
| **SharedLinkService** | Delete Link | `delete_sharedlink` | SharedLink |
| **UserService** | Create User | `create_user` | User |

---

**آخر تحديث:** 2025  
**الحالة:** ✅ تم الدمج بنجاح

