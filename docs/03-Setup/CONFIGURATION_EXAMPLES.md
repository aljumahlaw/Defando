# أمثلة على قراءة Configuration
## Configuration Reading Examples

**التاريخ:** 2025  
**الإصدار:** 1.0

---

## 📋 نظرة عامة

هذا الملف يحتوي على أمثلة تفصيلية لكيفية قراءة Configuration من User Secrets و Environment Variables في `Program.cs` و Services.

---

## 🔧 1. أمثلة في Program.cs

### 1.1 قراءة Connection String (موجود بالفعل)

**الكود الحالي:**

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
```

**كيف يعمل:**
- يقرأ من `appsettings.json` أولاً
- ثم من User Secrets (في Development)
- ثم من Environment Variables (في Production)
- إذا لم يجد قيمة، يرمي Exception

---

### 1.2 قراءة قيمة بسيطة

```csharp
// قراءة BaseUrl من EmailNotifications
var baseUrl = builder.Configuration["EmailNotifications:BaseUrl"] 
    ?? "https://localhost:5001";

// استخدام القيمة
builder.Services.Configure<EmailNotificationOptions>(options =>
{
    options.BaseUrl = baseUrl;
});
```

---

### 1.3 قراءة قيمة مع نوع محدد

```csharp
// قراءة MaxFileSizeMB مع قيمة افتراضية
var maxFileSize = builder.Configuration.GetValue<int>("FileStorage:MaxFileSizeMB", 100);

// قراءة Enabled مع قيمة افتراضية
var ocrEnabled = builder.Configuration.GetValue<bool>("Ocr:Enabled", false);
```

---

### 1.4 قراءة قسم كامل (Section)

```csharp
// قراءة قسم Ocr
var ocrSection = builder.Configuration.GetSection("Ocr");
var tesseractPath = ocrSection["TesseractPath"] ?? "C:\\Program Files\\Tesseract-OCR";
var language = ocrSection["Language"] ?? "ara+eng";
var enabled = ocrSection.GetValue<bool>("Enabled", true);

// استخدام القيم
if (enabled)
{
    builder.Services.AddScoped<IOcrService, OcrService>();
}
```

---

### 1.5 ربط Configuration Section إلى Class

**إنشاء Class:**

```csharp
// في ملف جديد: src/Configuration/OcrSettings.cs
namespace LegalDocSystem.Configuration;

public class OcrSettings
{
    public string TesseractPath { get; set; } = "C:\\Program Files\\Tesseract-OCR";
    public string Language { get; set; } = "ara+eng";
    public bool Enabled { get; set; } = true;
}
```

**في `Program.cs`:**

```csharp
// ربط Configuration Section إلى Class
builder.Services.Configure<OcrSettings>(
    builder.Configuration.GetSection("Ocr"));

// أو قراءة مباشرة
var ocrSettings = builder.Configuration.GetSection("Ocr").Get<OcrSettings>()
    ?? new OcrSettings();
```

**استخدام في Service:**

```csharp
public class OcrService : IOcrService
{
    private readonly OcrSettings _settings;

    public OcrService(IOptions<OcrSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<string> ExtractTextFromImageAsync(string imagePath)
    {
        if (!_settings.Enabled)
            throw new InvalidOperationException("OCR is disabled.");

        // استخدام _settings.TesseractPath و _settings.Language
    }
}
```

---

## 🔧 2. أمثلة في Services

### 2.1 قراءة Configuration في Service

```csharp
public class MyService : IMyService
{
    private readonly IConfiguration _configuration;

    public MyService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void DoSomething()
    {
        var baseUrl = _configuration["EmailNotifications:BaseUrl"];
        var maxFileSize = _configuration.GetValue<int>("FileStorage:MaxFileSizeMB", 100);
    }
}
```

---

### 2.2 استخدام IOptions<T> (الطريقة الموصى بها)

**في `Program.cs`:**

```csharp
// ربط Configuration Section
builder.Services.Configure<FileStorageSettings>(
    builder.Configuration.GetSection("FileStorage"));
```

**إنشاء Class:**

```csharp
// src/Configuration/FileStorageSettings.cs
namespace LegalDocSystem.Configuration;

public class FileStorageSettings
{
    public string BasePath { get; set; } = string.Empty;
    public int MaxFileSizeMB { get; set; } = 100;
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
}
```

**في Service:**

```csharp
public class FileStorageService : IFileStorageService
{
    private readonly FileStorageSettings _settings;

    public FileStorageService(IOptions<FileStorageSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName)
    {
        // التحقق من الحجم
        if (fileStream.Length > _settings.MaxFileSizeMB * 1024 * 1024)
            throw new InvalidOperationException("File size exceeds maximum allowed size.");

        // التحقق من الامتداد
        var extension = Path.GetExtension(fileName);
        if (!_settings.AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("File extension not allowed.");

        // استخدام _settings.BasePath
        var filePath = Path.Combine(_settings.BasePath, fileName);
        // ...
    }
}
```

---

## 🔧 3. أمثلة متقدمة

### 3.1 قراءة Connection String من Environment Variable مباشرة

```csharp
// قراءة من Environment Variable مباشرة (بدون Configuration)
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found in environment variables.");

// أو مع قيمة افتراضية
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=LegalDocDb;Username=postgres;Password=default;";
```

---

### 3.2 التحقق من وجود قيمة

```csharp
// التحقق من وجود Connection String
if (string.IsNullOrEmpty(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is required. " +
        "Please set it in User Secrets (Development) or Environment Variables (Production).");
}
```

---

### 3.3 قراءة قيم متعددة مع Validation

```csharp
// قراءة و التحقق من جميع القيم المطلوبة
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var baseUrl = builder.Configuration["EmailNotifications:BaseUrl"];
var tesseractPath = builder.Configuration["Ocr:TesseractPath"];

var missingSettings = new List<string>();

if (string.IsNullOrEmpty(connectionString))
    missingSettings.Add("ConnectionStrings:DefaultConnection");

if (string.IsNullOrEmpty(baseUrl))
    missingSettings.Add("EmailNotifications:BaseUrl");

if (string.IsNullOrEmpty(tesseractPath))
    missingSettings.Add("Ocr:TesseractPath");

if (missingSettings.Any())
{
    throw new InvalidOperationException(
        $"Missing required configuration settings: {string.Join(", ", missingSettings)}. " +
        "Please set them in User Secrets (Development) or Environment Variables (Production).");
}
```

---

### 3.4 قراءة Configuration بناءً على Environment

```csharp
var environment = builder.Environment.EnvironmentName;

if (environment == "Development")
{
    // قراءة من User Secrets
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}
else if (environment == "Production")
{
    // قراءة من Environment Variables
    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
}
```

---

## 📋 4. أمثلة عملية للمشروع

### 4.1 قراءة Connection String (موجود في Program.cs)

```csharp
// ✅ موجود بالفعل في Program.cs
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
```

---

### 4.2 قراءة Ocr Settings (مثال)

```csharp
// في Program.cs
var ocrSection = builder.Configuration.GetSection("Ocr");
var ocrEnabled = ocrSection.GetValue<bool>("Enabled", false);

if (ocrEnabled)
{
    var tesseractPath = ocrSection["TesseractPath"] 
        ?? throw new InvalidOperationException("Ocr:TesseractPath is required when Ocr:Enabled is true.");
    
    var language = ocrSection["Language"] ?? "ara+eng";
    
    // تسجيل OcrService
    builder.Services.AddScoped<IOcrService, OcrService>();
}
```

---

### 4.3 قراءة FileStorage Settings (مثال)

```csharp
// في Program.cs
var fileStorageSection = builder.Configuration.GetSection("FileStorage");
var basePath = fileStorageSection["BasePath"] 
    ?? throw new InvalidOperationException("FileStorage:BasePath is required.");

// إنشاء المجلد إذا لم يكن موجوداً
if (!Directory.Exists(basePath))
{
    Directory.CreateDirectory(basePath);
}

// تسجيل FileStorageService
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
```

---

### 4.4 قراءة EmailNotifications Settings (مثال)

```csharp
// في Program.cs
var emailSection = builder.Configuration.GetSection("EmailNotifications");
var emailEnabled = emailSection.GetValue<bool>("Enabled", false);
var baseUrl = emailSection["BaseUrl"] ?? "https://localhost:5001";

if (emailEnabled)
{
    // تسجيل EmailService
    builder.Services.AddScoped<IEmailService, EmailService>();
}
```

---

## ✅ 5. Checklist للاستخدام

- [ ] ✅ قراءة Connection String من Configuration (موجود)
- [ ] ⚠️ إضافة قراءة Ocr Settings (اختياري)
- [ ] ⚠️ إضافة قراءة FileStorage Settings (اختياري)
- [ ] ⚠️ إضافة قراءة EmailNotifications Settings (اختياري)
- [ ] ⚠️ استخدام `IOptions<T>` للـ Settings المعقدة (موصى به)

---

**آخر تحديث:** 2025  
**الحالة:** ✅ جاهز للاستخدام

