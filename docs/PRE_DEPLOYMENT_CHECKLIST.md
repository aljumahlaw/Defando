# قائمة التحقق قبل النشر - LegalDocSystem
## Pre-Deployment Checklist

**التاريخ:** 2025-11-29  
**المشروع:** LegalDocSystem (ASP.NET Core 8 + Blazor Server + PostgreSQL)  
**المصدر:** 
- `docs/TESTS_COMPREHENSIVE_AUDIT_REPORT.md`
- `docs/01-Security/COMPREHENSIVE_SECURITY_AUDIT_REPORT.md`

---

## 🔴 عالي (يجب إنهاؤه قبل النشر)

### أمان (Security)

| البند | النوع | الوصف |
|------|------|------|
| **نقل Database Password إلى User Secrets** | أمان | نقل `ConnectionStrings:DefaultConnection` من `appsettings.json` إلى User Secrets/Environment Variables |
| **نقل Encryption Key إلى User Secrets** | أمان | نقل `Encryption:Key` من `appsettings.json` إلى User Secrets/Environment Variables |
| **إصلاح Path Traversal في FileStorageService** | أمان | إضافة Path Sanitization في `FileStorageService.cs` (السطر 87) لمنع الوصول إلى ملفات خارج المجلد المحدد |

### اختبارات (Testing)

| البند | النوع | الوصف |
|------|------|------|
| **إضافة Unit Tests لـ FileStorageService** | اختبارات | إضافة Unit Tests لـ CRUD operations + Security checks (Path validation, File extension validation) |
| **إضافة Unit Tests لـ FolderService** | اختبارات | إضافة Unit Tests لـ CRUD operations + GetSubFolders + Hierarchy validation |
| **إضافة Unit Tests لـ TaskService** | اختبارات | إضافة Unit Tests لـ CRUD operations + UpdateStatus + GetTasksByUser |
| **إضافة Unit Tests لـ SharedLinkService** | اختبارات | إضافة Unit Tests لـ Create, Get, Validate, Expire, Access Logging |
| **إضافة Unit Tests لـ DocumentsController** | اختبارات | إضافة Unit Tests للـ Controllers (Authorization, Validation, Error Handling) |
| **إضافة Unit Tests لـ FoldersController** | اختبارات | إضافة Unit Tests للـ Controllers (Authorization, Validation, Error Handling) |
| **إضافة Unit Tests لـ TasksController** | اختبارات | إضافة Unit Tests للـ Controllers (Authorization, Validation, Error Handling) |
| **إضافة Unit Tests لـ UsersController** | اختبارات | إضافة Unit Tests للـ Controllers (Authorization, Validation, Error Handling) |
| **إضافة Unit Tests لـ AuditLoggingMiddleware** | اختبارات | إضافة Unit Tests للـ Middleware (Request logging, IP extraction, User extraction) |
| **إضافة Unit Tests لـ LoginRateLimitMiddleware** | اختبارات | إضافة Unit Tests للـ Middleware (Rate limiting logic, Blocking mechanism) |

---

## 🟡 متوسط (مفضّل إنهاؤه قبل النشر، أو في أول تحديث بعده)

### أمان (Security)

| البند | النوع | الوصف |
|------|------|------|
| **تغيير SecurePolicy إلى `Always` في الإنتاج** | أمان | تغيير `CookieSecurePolicy.SameAsRequest` إلى `CookieSecurePolicy.Always` في `Program.cs` (Cookie Authentication + Anti-Forgery) |
| **نقل SMTP Password إلى User Secrets** | أمان | نقل `Smtp:Password` من `appsettings.json` إلى User Secrets/Environment Variables |
| **إخفاء تفاصيل الأخطاء من المستخدم** | أمان | إخفاء Detailed Error Messages في Controllers من المستخدمين النهائيين (استخدام Generic messages) |
| **زيادة HSTS MaxAge إلى سنة واحدة** | أمان | تغيير `MaxAge` من 30 days إلى 365 days في `Program.cs` (السطر 197-201) |

### اختبارات (Testing)

| البند | النوع | الوصف |
|------|------|------|
| **إضافة Unit Tests لـ IncomingService** | اختبارات | إضافة Unit Tests لـ CRUD operations + Search + GenerateIncomingNumber |
| **إضافة Unit Tests لـ OutgoingService** | اختبارات | إضافة Unit Tests لـ CRUD operations + Search + GenerateOutgoingNumber |
| **إضافة Unit Tests لـ OcrService** | اختبارات | إضافة Unit Tests لـ ProcessOcr, QueueDocument, ExtractTextFromImage/PDF |
| **إضافة Unit Tests لـ NotificationService** | اختبارات | إضافة Unit Tests لـ ShowToast, ShowError, Notification display logic |
| **إضافة Unit Tests لـ BackgroundJobsService** | اختبارات | إضافة Unit Tests لـ ProcessOcrQueue, SendEmailNotifications, CleanupExpiredLinks |

### بنية (Architecture)

| البند | النوع | الوصف |
|------|------|------|
| **تنفيذ PruneAuditLogsAsync** | بنية | إضافة وظيفة `PruneAuditLogsAsync` في `AuditService` مع سياسة احتفاظ 180 يوماً (أو حسب متطلبات الامتثال) |
| **إنشاء BaseServiceTest class** | بنية | إنشاء Base Test Class مع Setup مشترك للـ Mocks والـ DbContext لتقليل التكرار في Test Classes |
| **استخدام xUnit Fixtures للـ Shared Setup** | بنية | استخدام xUnit Fixtures للـ Shared Setup بين Test Classes |
| **إضافة Test Categories (Traits)** | بنية | إضافة Traits لتجميع الاختبارات (Fast/Slow, Unit/Integration) |

---

## 🟢 منخفض (تحسينات يمكن جدولتها لاحقاً)

### أمان (Security)

| البند | النوع | الوصف |
|------|------|------|
| **استخدام Redis لـ Distributed Cache** | أمان | استبدال `DistributedMemoryCache` بـ Redis Cache للتطبيقات الموزعة |
| **إضافة IP Blocking mechanism** | أمان | إضافة آلية لحظر IP addresses بعد عدد محدد من المحاولات الفاشلة |
| **إضافة Two-Factor Authentication (2FA)** | أمان | إضافة 2FA كخيار إضافي للأمان (SMS/Email/App-based) |

### اختبارات (Testing)

| البند | النوع | الوصف |
|------|------|------|
| **تحسين Integration Tests** | اختبارات | استخدام FluentAssertions بدلاً من `Assert.True` في Integration Tests + إضافة Assertions أكثر تحديداً |
| **إضافة Load Testing** | اختبارات | إضافة اختبارات Load Testing و Stress Testing للـ Performance |
| **تحسين TestDataBuilder** | اختبارات | إضافة المزيد من Builder methods للـ Entities الأخرى + Fluent API للـ Complex Scenarios |
| **تحسين أسماء الاختبارات** | اختبارات | توحيد أسلوب التسمية + إضافة وصف أوضح للسيناريوهات |
| **ترتيب مجلدات الاختبارات** | اختبارات | تنظيم أفضل لملفات الاختبار حسب الـ Feature (Services/, Controllers/, Middleware/) |

### بنية (Architecture)

| البند | النوع | الوصف |
|------|------|------|
| **إضافة Rate Limiting tests في AuthService** | بنية | إضافة اختبارات لـ Rate Limiting (محاولات تسجيل دخول متعددة) في AuthServiceTests |
| **إضافة Session Timeout tests** | بنية | إضافة اختبارات لـ Session Timeout و Multiple Login Sessions |
| **إضافة Password Reset Flow tests** | بنية | إضافة اختبارات لـ Password Reset Flow (إن كانت موجودة) |

---

## 📊 ملخص الإحصائيات

### الخدمات والطبقات غير المغطاة بالاختبارات:

| الاسم | النوع | حالة الاختبارات الحالية | أولوية مقترحة |
|------|------|----------------------|--------------|
| FileStorageService | Service | لا يوجد | 🔴 عالي |
| FolderService | Service | لا يوجد | 🔴 عالي |
| TaskService | Service | لا يوجد | 🔴 عالي |
| SharedLinkService | Service | لا يوجد | 🔴 عالي |
| DocumentsController | Controller | لا يوجد | 🔴 عالي |
| FoldersController | Controller | لا يوجد | 🔴 عالي |
| TasksController | Controller | لا يوجد | 🔴 عالي |
| UsersController | Controller | لا يوجد | 🔴 عالي |
| AuditLoggingMiddleware | Middleware | لا يوجد | 🔴 عالي |
| LoginRateLimitMiddleware | Middleware | لا يوجد | 🔴 عالي |
| IncomingService | Service | لا يوجد | 🟡 متوسط |
| OutgoingService | Service | لا يوجد | 🟡 متوسط |
| OcrService | Service | لا يوجد | 🟡 متوسط |
| NotificationService | Service | لا يوجد | 🟡 متوسط |
| BackgroundJobsService | Service | لا يوجد | 🟡 متوسط |

### بنود الأمان المتبقية:

| البند | الحالة الحالية | أولوية التقرير الأصلية |
|------|---------------|---------------------|
| Database Password في User Secrets | غير منفّذة | 🔴 حرجة |
| Encryption Key في User Secrets | غير منفّذة | 🔴 حرجة |
| Path Traversal في FileStorageService | غير منفّذة | 🔴 حرجة |
| SecurePolicy = Always في الإنتاج | غير منفّذة | 🟡 مهمة |
| SMTP Password في User Secrets | غير منفّذة | 🟡 مهمة |
| إخفاء تفاصيل الأخطاء | غير منفّذة | 🟡 مهمة |
| HSTS MaxAge = 365 days | غير منفّذة | 🟡 مهمة |
| PruneAuditLogsAsync | غير منفّذة | 🟡 مهمة |
| Redis للـ Distributed Cache | غير منفّذة | 🟢 تحسين |
| IP Blocking mechanism | غير منفّذة | 🟢 تحسين |
| Two-Factor Authentication | غير منفّذة | 🟢 تحسين |

---

## 📝 ملاحظات

### الخدمات المغطاة حالياً:
- ✅ AuthService (13 اختبار)
- ✅ UserService (35 اختبار)
- ✅ DocumentService (45 اختبار)
- ✅ AuditService (28 اختبار)
- ✅ EmailService (27 اختبار)
- ✅ EncryptionService (28 اختبار)

**إجمالي:** 6 خدمات من 15 خدمة (40% تغطية)

### الخدمات المتبقية:
- ❌ FileStorageService
- ❌ FolderService
- ❌ TaskService
- ❌ SharedLinkService
- ❌ IncomingService
- ❌ OutgoingService
- ❌ OcrService
- ❌ NotificationService
- ❌ BackgroundJobsService

**إجمالي:** 9 خدمات بدون اختبارات

---

**تم إنشاء القائمة:** 2025-11-29  
**آخر تحديث:** 2025-11-29


