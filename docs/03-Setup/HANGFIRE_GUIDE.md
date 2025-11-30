# دليل Hangfire - المهام الخلفية

## نظرة عامة

Hangfire هو مكتبة لإدارة المهام الخلفية (Background Jobs) في ASP.NET Core. يستخدم هذا المشروع Hangfire لتنفيذ المهام المتكررة والمرتبة.

---

## التثبيت

### 1. تثبيت الحزم NuGet

```powershell
cd src
dotnet add package Hangfire.Core
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.PostgreSql
```

### 2. إعداد قاعدة البيانات

Hangfire يحتاج إلى جداول في قاعدة البيانات PostgreSQL. سيتم إنشاؤها تلقائياً عند أول تشغيل.

**ملاحظة:** تأكد من أن المستخدم لديه صلاحيات CREATE TABLE.

---

## الإعداد في Program.cs

تم إعداد Hangfire في `Program.cs`:

```csharp
// Hangfire
builder.Services.AddHangfire(config => config
    .UsePostgreSqlStorage(connectionString)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings());

builder.Services.AddHangfireServer();
builder.Services.AddScoped<BackgroundJobsService>();
```

---

## الوصول إلى Dashboard

### URL

افتح المتصفح على:
```
https://localhost:5001/hangfire
```

### الأمان

**⚠️ مهم:** Dashboard مفتوح حالياً. يجب إضافة Authentication قبل النشر في Production.

**التحديث المطلوب:**
- استبدال `HangfireAuthorizationFilter` بفحص Authentication حقيقي
- إضافة فحص الدور (Admin only)

---

## المهام المتكررة (Recurring Jobs)

### 1. معالجة قائمة انتظار OCR

**التكرار:** كل دقيقة

```csharp
RecurringJob.AddOrUpdate<BackgroundJobsService>(
    "process-ocr-queue",
    x => x.ProcessOcrQueueAsync(),
    Cron.Minutely);
```

**الوظيفة:**
- معالجة المستندات الممسوحة في قائمة انتظار OCR
- استخراج النص باستخدام Tesseract
- تحديث `document.metadata->>'ocr_text'`
- تحديث `search_vector` للبحث النصي الكامل

---

### 2. إرسال إشعارات البريد الإلكتروني

**التكرار:** كل ساعة

```csharp
RecurringJob.AddOrUpdate<BackgroundJobsService>(
    "send-email-notifications",
    x => x.SendEmailNotificationsAsync(),
    Cron.Hourly);
```

**الوظيفة:**
- إرسال تذكيرات للمهام المستحقة خلال 24 ساعة
- إرسال إشعارات عند تعيين مهام جديدة
- تسجيل الإرسال في `email_log` table

---

### 3. تنظيف الروابط المنتهية

**التكرار:** يومياً

```csharp
RecurringJob.AddOrUpdate<BackgroundJobsService>(
    "cleanup-expired-links",
    x => x.CleanupExpiredLinksAsync(),
    Cron.Daily);
```

**الوظيفة:**
- تعطيل الروابط المشتركة المنتهية الصلاحية
- تحديث `is_active = false` في `shared_links` table

---

### 4. توليد تقارير التدقيق

**التكرار:** يومياً في منتصف الليل

```csharp
RecurringJob.AddOrUpdate<BackgroundJobsService>(
    "generate-audit-reports",
    x => x.GenerateAuditReportsAsync(),
    Cron.Daily(0, 0));
```

**الوظيفة:**
- تجميع سجلات التدقيق من اليوم السابق
- توليد تقرير PDF
- إرسال التقرير للمدير عبر البريد الإلكتروني

---

### 5. فك قفل المستندات المنتهية

**التكرار:** كل ساعة

```csharp
RecurringJob.AddOrUpdate<BackgroundJobsService>(
    "unlock-expired-documents",
    x => x.UnlockExpiredDocumentsAsync(),
    Cron.Hourly);
```

**الوظيفة:**
- فك قفل المستندات المقفولة لأكثر من 8 ساعات
- تحديث `is_locked = false` في `documents` table

---

## إضافة مهام جديدة

### 1. إضافة Method في BackgroundJobsService

```csharp
public async Task MyNewJobAsync()
{
    try
    {
        _logger.LogInformation("Starting my new job...");
        
        // Your job logic here
        
        _logger.LogInformation("Job completed successfully.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in MyNewJobAsync");
    }
}
```

### 2. تسجيل المهمة في Program.cs

```csharp
RecurringJob.AddOrUpdate<BackgroundJobsService>(
    "my-new-job",
    x => x.MyNewJobAsync(),
    Cron.Hourly);
```

---

## Cron Expressions

Hangfire يدعم Cron expressions:

| التعبير | الوصف |
|---------|-------|
| `Cron.Minutely` | كل دقيقة |
| `Cron.Hourly` | كل ساعة |
| `Cron.Daily` | يومياً في منتصف الليل |
| `Cron.Weekly` | أسبوعياً |
| `Cron.Monthly` | شهرياً |
| `Cron.Daily(14, 30)` | يومياً في 14:30 |
| `Cron.Weekly(DayOfWeek.Monday, 9, 0)` | كل إثنين في 9:00 |

**مزيد من الأمثلة:**
```csharp
Cron.Daily(0, 0)        // منتصف الليل
Cron.Daily(14, 30)      // 2:30 PM
Cron.Hourly(30)         // كل ساعة في الدقيقة 30
Cron.Weekly(DayOfWeek.Monday, 9, 0)  // كل إثنين في 9:00 AM
```

---

## المهام الفورية (Fire-and-Forget)

يمكن تنفيذ مهام فورية من أي مكان في الكود:

```csharp
BackgroundJob.Enqueue<BackgroundJobsService>(
    x => x.ProcessOcrQueueAsync());
```

---

## المهام المؤجلة (Delayed Jobs)

```csharp
BackgroundJob.Schedule<BackgroundJobsService>(
    x => x.SendEmailNotificationsAsync(),
    TimeSpan.FromMinutes(30));
```

---

## المهام المستمرة (Continuations)

```csharp
var jobId = BackgroundJob.Enqueue<BackgroundJobsService>(
    x => x.ProcessOcrQueueAsync());

BackgroundJob.ContinueJobWith<BackgroundJobsService>(
    jobId,
    x => x.SendEmailNotificationsAsync());
```

---

## مراقبة المهام

### Dashboard Features

1. **Jobs:** عرض جميع المهام (Pending, Processing, Succeeded, Failed)
2. **Recurring Jobs:** عرض المهام المتكررة
3. **Retries:** إعادة المحاولة التلقائية للمهام الفاشلة
4. **Logs:** سجلات تنفيذ المهام

### فحص حالة المهمة

```csharp
var jobId = BackgroundJob.Enqueue<BackgroundJobsService>(
    x => x.ProcessOcrQueueAsync());

// Check job state
var state = BackgroundJob.GetJobState(jobId);
```

---

## استكشاف الأخطاء

### مشكلة: Hangfire Dashboard لا يفتح

**الحل:**
1. تأكد من أن Hangfire Server يعمل
2. تحقق من Connection String
3. تأكد من صلاحيات قاعدة البيانات

### مشكلة: المهام لا تنفذ

**الحل:**
1. تحقق من Logs في Dashboard
2. تأكد من أن Hangfire Server يعمل
3. تحقق من Connection String

### مشكلة: خطأ في PostgreSQL

**الحل:**
1. تأكد من تثبيت `Hangfire.PostgreSql`
2. تحقق من صلاحيات المستخدم (CREATE TABLE)
3. راجع Logs في Console

---

## أفضل الممارسات

1. **Error Handling:** استخدم try/catch في جميع المهام
2. **Logging:** سجل جميع العمليات المهمة
3. **Idempotency:** تأكد من أن المهام قابلة للتكرار بأمان
4. **Timeouts:** حدد timeout مناسب للمهام الطويلة
5. **Resource Management:** أغلق الاتصالات والموارد بشكل صحيح

---

## الأمان

### Production Checklist

- [ ] إضافة Authentication للـ Dashboard
- [ ] تقييد الوصول للمديرين فقط
- [ ] استخدام HTTPS
- [ ] مراجعة Logs بانتظام
- [ ] إعداد Backup لقاعدة البيانات

---

## المراجع

- [Hangfire Documentation](https://docs.hangfire.io/)
- [Hangfire PostgreSQL Storage](https://github.com/frankhommers/Hangfire.PostgreSql)
- [Cron Expression Generator](https://crontab.guru/)

---

**آخر تحديث:** 2025

