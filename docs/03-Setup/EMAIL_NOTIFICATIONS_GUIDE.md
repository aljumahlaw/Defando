# دليل إشعارات البريد الإلكتروني

## نظرة عامة

تم إنشاء نظام شامل لإشعارات البريد الإلكتروني في المشروع يتضمن:
- إرسال إشعارات عند إنشاء روابط مشاركة
- إرسال إشعارات عند الوصول إلى روابط مشاركة
- إشعارات تذكير المهام
- Toast notifications في واجهة المستخدم
- إعادة المحاولة التلقائية في حالة فشل الإرسال

## المكونات المُنشأة

### 1. EmailService.cs (محدّث)

**الميزات الجديدة:**
- `SendEmailWithRetryAsync()` - إرسال بريد مع إعادة محاولة تلقائية
- `SendSharedLinkCreatedNotificationAsync()` - إشعار عند إنشاء رابط مشاركة
- `SendSharedLinkAccessedNotificationAsync()` - إشعار عند الوصول إلى رابط مشاركة
- دعم MailKit (جاهز للتفعيل بعد تثبيت الحزمة)

**إعدادات إعادة المحاولة:**
- عدد المحاولات الافتراضي: 3
- التأخير بين المحاولات: 5 ثوانٍ
- قابل للتخصيص لكل نوع إشعار

### 2. EmailTemplates.cs (جديد)

**القوالب المتوفرة:**
- `SharedLinkCreated()` - قالب إشعار إنشاء رابط مشاركة
- `SharedLinkAccessed()` - قالب إشعار الوصول إلى رابط مشاركة
- `TaskReminder()` - قالب تذكير المهام
- `TestEmail()` - قالب بريد الاختبار
- `BaseTemplate()` - قالب أساسي مع دعم RTL

**المميزات:**
- تصميم HTML احترافي
- دعم RTL كامل
- Responsive design
- ألوان مميزة لكل نوع إشعار

### 3. NotificationService.cs (جديد)

**الوظائف:**
- `ShowSuccess()` - عرض إشعار نجاح
- `ShowError()` - عرض إشعار خطأ
- `ShowWarning()` - عرض إشعار تحذير
- `ShowInfo()` - عرض إشعار معلومات

**المميزات:**
- Auto-dismiss بعد مدة محددة
- إمكانية الإغلاق اليدوي
- Event-driven updates
- Thread-safe

### 4. NotificationToast.razor (جديد)

**المميزات:**
- Toast notifications في أعلى الصفحة
- 4 أنواع من الإشعارات (نجاح، خطأ، تحذير، معلومات)
- Animations (slide in/out)
- Responsive design
- RTL support

### 5. BackgroundJobsService.cs (محدّث)

**التحديثات:**
- استخدام `EmailTemplates.TaskReminder()` بدلاً من HTML مضمّن
- استخدام `SendEmailWithRetryAsync()` لإعادة المحاولة التلقائية
- تحسين معالجة الأخطاء

### 6. SharedLinkService.cs (محدّث)

**التحديثات:**
- إرسال إشعار عند إنشاء رابط مشاركة (`CreateSharedLinkAsync`)
- إرسال إشعار عند الوصول إلى رابط مشاركة (`RecordAccessAsync`)
- استخدام IConfiguration للحصول على BaseUrl
- Fire-and-forget pattern للإشعارات (لا تعطل العملية الرئيسية)

### 7. SmtpSettings.razor (محدّث)

**الإضافات:**
- إعدادات الإشعارات:
  - إشعار عند إنشاء رابط مشاركة
  - إشعار عند الوصول إلى رابط مشاركة
  - إشعارات تذكير المهام
- حفظ الإعدادات في قاعدة البيانات

### 8. appsettings.json (محدّث)

**الإضافات:**
```json
"EmailNotifications": {
  "Enabled": true,
  "NotifyOnSharedLinkCreated": true,
  "NotifyOnSharedLinkAccessed": true,
  "NotifyOnTaskReminder": true,
  "BaseUrl": "https://yourdomain.com",
  "RetryAttempts": 3,
  "RetryDelaySeconds": 5
}
```

## التثبيت والإعداد

### 1. تثبيت MailKit (مطلوب للإنتاج)

```bash
dotnet add package MailKit
```

### 2. تفعيل MailKit في EmailService.cs

بعد تثبيت MailKit، قم بإلغاء تعليق الكود في `SendEmailAsync()`:

```csharp
// Uncomment these lines:
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

// Then uncomment the MailKit code in SendEmailAsync()
```

### 3. إعداد BaseUrl

قم بتحديث `BaseUrl` في `appsettings.json`:

```json
"EmailNotifications": {
  "BaseUrl": "https://your-actual-domain.com"
}
```

### 4. إعداد SMTP

1. افتح صفحة `/settings/smtp`
2. أدخل إعدادات SMTP
3. فعّل/عطّل أنواع الإشعارات حسب الحاجة
4. احفظ الإعدادات

## الاستخدام

### في Blazor Pages

```csharp
@inject INotificationService NotificationService

// Show success notification
NotificationService.ShowSuccess("تم الحفظ بنجاح!");

// Show error notification
NotificationService.ShowError("حدث خطأ أثناء الحفظ");

// Show warning notification
NotificationService.ShowWarning("تحذير: البيانات غير مكتملة");

// Show info notification
NotificationService.ShowInfo("معلومة: تم تحديث البيانات");
```

### في Services

```csharp
// Send email with retry
await emailService.SendEmailWithRetryAsync(
    to: "user@example.com",
    subject: "Subject",
    body: "Body",
    maxRetries: 3,
    delaySeconds: 5
);

// Send shared link created notification
await emailService.SendSharedLinkCreatedNotificationAsync(
    recipientEmail: "user@example.com",
    recipientName: "User Name",
    documentName: "Document.pdf",
    linkUrl: "https://domain.com/shared/token",
    expiresAt: DateTime.Now.AddDays(7)
);
```

## قوالب البريد الإلكتروني

### تخصيص القوالب

يمكنك تخصيص القوالب في `EmailTemplates.cs`:

```csharp
// Example: Customize colors
private static string BaseTemplate(string content, string title)
{
    // Modify styles here
    return $@"
    <style>
        .header {{
            background-color: #YOUR_COLOR;
        }}
    </style>
    ...
    ";
}
```

### إضافة قوالب جديدة

```csharp
public static string YourCustomTemplate(string param1, string param2)
{
    var content = $@"
        <h2>Your Title</h2>
        <p>Your content: {param1}</p>
    ";
    return BaseTemplate(content, "Your Template Title");
}
```

## إعادة المحاولة التلقائية

### آلية العمل

1. المحاولة الأولى: إرسال فوري
2. في حالة الفشل: انتظار 5 ثوانٍ (قابل للتعديل)
3. المحاولة الثانية: إعادة المحاولة
4. في حالة الفشل: انتظار 5 ثوانٍ
5. المحاولة الثالثة: إعادة المحاولة الأخيرة

### تخصيص إعدادات إعادة المحاولة

```csharp
// في appsettings.json
"EmailNotifications": {
  "RetryAttempts": 5,        // عدد المحاولات
  "RetryDelaySeconds": 10    // التأخير بالثواني
}

// أو في الكود
await emailService.SendEmailWithRetryAsync(
    to: "user@example.com",
    subject: "Subject",
    body: "Body",
    maxRetries: 5,      // عدد المحاولات
    delaySeconds: 10    // التأخير
);
```

## معالجة الأخطاء

### تسجيل الأخطاء

جميع الأخطاء تُسجل في:
- Application logs
- EmailLog table في قاعدة البيانات

### حالة الإرسال

- `pending` - في انتظار الإرسال
- `sent` - تم الإرسال بنجاح
- `failed` - فشل الإرسال

### عرض الأخطاء للمستخدم

```csharp
try
{
    var result = await emailService.SendEmailAsync(...);
    if (!result)
    {
        NotificationService.ShowError("فشل إرسال البريد الإلكتروني");
    }
}
catch (Exception ex)
{
    NotificationService.ShowError($"حدث خطأ: {ex.Message}");
}
```

## أفضل الممارسات

1. **استخدم Fire-and-Forget للإشعارات:**
   ```csharp
   _ = Task.Run(async () =>
   {
       await emailService.SendSharedLinkCreatedNotificationAsync(...);
   });
   ```

2. **تحقق من تفعيل الإشعارات:**
   ```csharp
   var settings = await GetSmtpSettingsAsync();
   if (settings?.NotifyOnSharedLinkCreated == true)
   {
       // Send notification
   }
   ```

3. **استخدم قوالب HTML:**
   ```csharp
   var body = EmailTemplates.SharedLinkCreated(...);
   ```

4. **سجّل جميع المحاولات:**
   - جميع محاولات الإرسال تُسجل في `EmailLog`
   - يمكن مراجعة السجلات لتحليل المشاكل

## استكشاف الأخطاء

### المشكلة: البريد لا يُرسل

**الحلول:**
1. تحقق من إعدادات SMTP في `/settings/smtp`
2. تأكد من تفعيل SMTP (`Enabled = true`)
3. تحقق من صحة بيانات SMTP (Host, Port, Username, Password)
4. راجع سجلات الأخطاء في Logs

### المشكلة: الإشعارات لا تظهر

**الحلول:**
1. تأكد من إضافة `<NotificationToast />` في `App.razor`
2. تحقق من تسجيل `INotificationService` في `Program.cs`
3. تأكد من استخدام `@inject INotificationService NotificationService`

### المشكلة: MailKit لا يعمل

**الحلول:**
1. تأكد من تثبيت الحزمة: `dotnet add package MailKit`
2. ألغِ تعليق using statements في `EmailService.cs`
3. ألغِ تعليق كود MailKit في `SendEmailAsync()`

## ملاحظات مهمة

1. **MailKit غير مثبت حالياً:**
   - الكود جاهز لاستخدام MailKit
   - يحتاج تثبيت الحزمة وإلغاء تعليق الكود
   - حالياً يتم محاكاة الإرسال للاختبار

2. **BaseUrl:**
   - يجب تحديث `BaseUrl` في `appsettings.json`
   - يُستخدم لإنشاء روابط المشاركة في الإشعارات

3. **التشفير:**
   - كلمات مرور SMTP مشفرة بـ Base64 (غير آمن للإنتاج)
   - TODO: استخدام DPAPI أو Azure Key Vault

4. **Fire-and-Forget:**
   - الإشعارات تُرسل بشكل غير متزامن
   - لا تعطل العملية الرئيسية
   - الأخطاء تُسجل ولكن لا تؤثر على العملية

---

**آخر تحديث**: 2025

