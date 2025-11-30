# دليل النشر والتشغيل

## المتطلبات الأساسية

### 1. تثبيت PostgreSQL 14+

**التحميل:**
- الرابط: https://www.postgresql.org/download/windows/
- اختر: **PostgreSQL 14.x** أو **PostgreSQL 16** (الأحدث)

**أثناء التثبيت:**
- احفظ كلمة مرور المستخدم `postgres` في مكان آمن
- البورت الافتراضي: `5432`
- تأكد من تثبيت **pgAdmin 4** (أداة إدارة قاعدة البيانات)

**التحقق من التثبيت:**
```powershell
psql --version
```

---

### 2. تثبيت .NET 8 SDK

**التحميل:**
- الرابط: https://dotnet.microsoft.com/download/dotnet/8.0
- اختر: **.NET 8.0 SDK** (ليس Runtime فقط)

**التحقق من التثبيت:**
```powershell
dotnet --version
```
يجب أن ترى: `8.0.xxx` أو أعلى

---

### 3. تثبيت Visual Studio 2022 أو VS Code

**الخيار 1: Visual Studio 2022**
- الرابط: https://visualstudio.microsoft.com/downloads/
- اختر: **Visual Studio Community 2022**
- أثناء التثبيت، اختر:
  - ✅ **ASP.NET and web development**
  - ✅ **.NET desktop development**

**الخيار 2: Visual Studio Code**
- الرابط: https://code.visualstudio.com/
- بعد التثبيت، ثبّت الإضافات:
  - C# (Microsoft)
  - C# Dev Kit (Microsoft)
  - .NET Extension Pack

---

## إعداد قاعدة البيانات

### 1. إنشاء قاعدة البيانات

افتح **pgAdmin 4** أو استخدم `psql`:

```sql
CREATE DATABASE legal_doc_system;
CREATE USER doc_user WITH PASSWORD 'your_secure_password';
GRANT ALL PRIVILEGES ON DATABASE legal_doc_system TO doc_user;
```

### 2. تنفيذ Schema

```powershell
psql -U doc_user -d legal_doc_system -f database/schema.sql
```

أو من pgAdmin:
1. انقر بالزر الأيمن على قاعدة البيانات `legal_doc_system`
2. اختر **Query Tool**
3. افتح ملف `database/schema.sql`
4. انقر **Execute**

---

## إعداد Connection String

### 1. تعديل appsettings.json

افتح `src/appsettings.json` وعدّل:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=legal_doc_system;Username=doc_user;Password=your_secure_password"
  }
}
```

### 2. إعدادات الإنتاج (appsettings.Production.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your_server;Port=5432;Database=legal_doc_system;Username=doc_user;Password=your_secure_password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

---

## تشغيل Migrations (اختياري)

إذا كنت تستخدم Entity Framework Migrations:

```powershell
cd src
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**ملاحظة:** المشروع الحالي يستخدم `schema.sql` مباشرة، لذا Migrations اختياري.

---

## تشغيل التطبيق

### 1. استعادة الحزم

```powershell
cd src
dotnet restore
```

### 2. تشغيل التطبيق

```powershell
dotnet run
```

### 3. فتح المتصفح

افتح المتصفح على: `https://localhost:5001` أو `http://localhost:5000`

---

## إعدادات إضافية

### NAS Storage (تخزين الملفات)

1. قم بإعداد مجلد مشترك على NAS أو خادم الملفات
2. أضف المسار في `appsettings.json`:

```json
{
  "Storage": {
    "BasePath": "\\\\nas-server\\documents\\storage"
  }
}
```

### Tesseract OCR

1. ثبّت Tesseract OCR 5.x
2. أضف المسار في `appsettings.json`:

```json
{
  "OCR": {
    "TesseractPath": "C:\\Program Files\\Tesseract-OCR",
    "Language": "ara+eng"
  }
}
```

### SMTP (البريد الإلكتروني)

```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "UseSsl": true
  }
}
```

---

## استكشاف الأخطاء

### مشكلة: Connection string غير موجود
**الحل:** تأكد من وجود `appsettings.json` في مجلد `src/`

### مشكلة: PostgreSQL لا يعمل
**الحل:** 
1. افتح **Services** (الخدمات)
2. ابحث عن **postgresql-x64-14** أو **postgresql-x64-16**
3. تأكد من أن الحالة **Running**

### مشكلة: Port 5000/5001 مستخدم
**الحل:** غيّر البورت في `appsettings.json`:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5002"
      }
    }
  }
}
```

---

**آخر تحديث:** 2025

