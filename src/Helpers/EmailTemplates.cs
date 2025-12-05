namespace Defando.Helpers;

/// <summary>
/// Email templates for system notifications.
/// </summary>
public static class EmailTemplates
{
    /// <summary>
    /// Base HTML template wrapper with RTL support.
    /// </summary>
    private static string BaseTemplate(string content, string title)
    {
        return $@"
<!DOCTYPE html>
<html dir=""rtl"" lang=""ar"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f4f4f4;
        }}
        .container {{
            background-color: #ffffff;
            border-radius: 8px;
            padding: 30px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .header {{
            background-color: #0d6efd;
            color: white;
            padding: 20px;
            border-radius: 8px 8px 0 0;
            margin: -30px -30px 20px -30px;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
        }}
        .content {{
            padding: 20px 0;
        }}
        .button {{
            display: inline-block;
            padding: 12px 24px;
            background-color: #0d6efd;
            color: white;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 0;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
            font-size: 12px;
            color: #666;
            text-align: center;
        }}
        .info-box {{
            background-color: #e7f3ff;
            border-right: 4px solid #0d6efd;
            padding: 15px;
            margin: 15px 0;
            border-radius: 4px;
        }}
        .warning-box {{
            background-color: #fff3cd;
            border-right: 4px solid #ffc107;
            padding: 15px;
            margin: 15px 0;
            border-radius: 4px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>نظام إدارة المستندات القانونية</h1>
        </div>
        <div class=""content"">
            {content}
        </div>
        <div class=""footer"">
            <p>هذه رسالة تلقائية من نظام إدارة المستندات القانونية</p>
            <p>يرجى عدم الرد على هذا البريد الإلكتروني</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Template for shared link created notification.
    /// </summary>
    public static string SharedLinkCreated(
        string recipientName,
        string documentName,
        string linkUrl,
        DateTime expiresAt,
        bool isPasswordProtected)
    {
        var passwordInfo = isPasswordProtected
            ? "<div class=\"warning-box\"><strong>⚠️ تنبيه:</strong> هذا الرابط محمي بكلمة مرور. سيحتاج المستلمون إلى إدخال كلمة المرور للوصول إلى المستند.</div>"
            : "";

        var content = $@"
            <h2>تم إنشاء رابط مشاركة جديد</h2>
            <p>عزيزي/عزيزتي <strong>{recipientName}</strong>,</p>
            <p>تم إنشاء رابط مشاركة جديد للمستند التالي:</p>
            <div class=""info-box"">
                <p><strong>اسم المستند:</strong> {documentName}</p>
                <p><strong>تاريخ انتهاء الصلاحية:</strong> {expiresAt:yyyy-MM-dd HH:mm}</p>
            </div>
            {passwordInfo}
            <p>يمكنك مشاركة الرابط التالي:</p>
            <p style=""background-color: #f8f9fa; padding: 15px; border-radius: 5px; word-break: break-all; font-family: monospace;"">
                {linkUrl}
            </p>
            <p style=""text-align: center;"">
                <a href=""{linkUrl}"" class=""button"">فتح الرابط</a>
            </p>
            <p><small>ملاحظة: هذا الرابط سينتهي تلقائياً في التاريخ المحدد أعلاه.</small></p>";

        return BaseTemplate(content, "رابط مشاركة جديد");
    }

    /// <summary>
    /// Template for shared link accessed notification.
    /// </summary>
    public static string SharedLinkAccessed(
        string recipientName,
        string documentName,
        string linkUrl,
        int accessCount,
        DateTime accessedAt)
    {
        var content = $@"
            <h2>تم الوصول إلى رابط المشاركة</h2>
            <p>عزيزي/عزيزتي <strong>{recipientName}</strong>,</p>
            <p>تم الوصول إلى رابط المشاركة الذي أنشأته للمستند التالي:</p>
            <div class=""info-box"">
                <p><strong>اسم المستند:</strong> {documentName}</p>
                <p><strong>وقت الوصول:</strong> {accessedAt:yyyy-MM-dd HH:mm}</p>
                <p><strong>إجمالي مرات الوصول:</strong> {accessCount}</p>
            </div>
            <p>الرابط المشترك:</p>
            <p style=""background-color: #f8f9fa; padding: 15px; border-radius: 5px; word-break: break-all; font-family: monospace;"">
                {linkUrl}
            </p>
            <div class=""warning-box"">
                <strong>💡 تذكير:</strong> إذا لم تكن تتوقع هذا الوصول، قد ترغب في مراجعة إعدادات الأمان للرابط.
            </div>";

        return BaseTemplate(content, "إشعار وصول إلى رابط مشاركة");
    }

    /// <summary>
    /// Template for task reminder notification.
    /// </summary>
    public static string TaskReminder(
        string recipientName,
        string taskTitle,
        DateTime dueDate,
        string taskStatus)
    {
        var content = $@"
            <h2>تذكير بمهمة قريبة من الاستحقاق</h2>
            <p>عزيزي/عزيزتي <strong>{recipientName}</strong>,</p>
            <p>هذه رسالة تذكير بأن لديك مهمة قريبة من الاستحقاق:</p>
            <div class=""info-box"">
                <p><strong>المهمة:</strong> {taskTitle}</p>
                <p><strong>تاريخ الاستحقاق:</strong> {dueDate:yyyy-MM-dd}</p>
                <p><strong>الحالة:</strong> {taskStatus}</p>
            </div>
            <div class=""warning-box"">
                <strong>⏰ تنبيه:</strong> يرجى مراجعة المهمة وإكمالها قبل الموعد المحدد.
            </div>";

        return BaseTemplate(content, "تذكير بمهمة");
    }

    /// <summary>
    /// Template for test email.
    /// </summary>
    public static string TestEmail(DateTime sentAt)
    {
        var content = $@"
            <h2>اختبار إعدادات البريد الإلكتروني</h2>
            <p>هذه رسالة اختبار للتحقق من إعدادات SMTP.</p>
            <p>إذا تلقيت هذه الرسالة، فهذا يعني أن الإعدادات صحيحة.</p>
            <div class=""info-box"">
                <p><strong>تم الإرسال في:</strong> {sentAt:yyyy-MM-dd HH:mm:ss}</p>
            </div>";

        return BaseTemplate(content, "اختبار SMTP");
    }
}

