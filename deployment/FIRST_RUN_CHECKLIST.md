# قائمة التحقق للتشغيل الأول

استخدم هذه القائمة للتأكد من أن كل شيء يعمل بشكل صحيح عند التشغيل الأول.

---

## ✅ قبل البدء

- [ ] .NET 8 SDK مثبت (`dotnet --version` يعرض 8.0.xxx)
- [ ] PostgreSQL 14+ مثبت ويعمل
- [ ] Visual Studio 2022 أو VS Code مثبت
- [ ] Git مثبت (اختياري)

---

## ✅ إعداد قاعدة البيانات

### 1. إنشاء قاعدة البيانات

- [ ] فتح pgAdmin 4 أو psql
- [ ] إنشاء قاعدة بيانات جديدة:
  ```sql
  CREATE DATABASE legal_doc_system;
  ```
- [ ] إنشاء مستخدم:
  ```sql
  CREATE USER doc_user WITH PASSWORD 'your_secure_password';
  GRANT ALL PRIVILEGES ON DATABASE legal_doc_system TO doc_user;
  ```

### 2. تنفيذ Schema

- [ ] تنفيذ `database/schema.sql`:
  ```powershell
  psql -U doc_user -d legal_doc_system -f database/schema.sql
  ```
- [ ] التحقق من الجداول:
  ```sql
  \dt
  ```
  يجب أن ترى 14 جدول

---

## ✅ إعداد Connection String

- [ ] فتح `src/appsettings.json`
- [ ] تحديث ConnectionString:
  ```json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Host=localhost;Port=5432;Database=legal_doc_system;Username=doc_user;Password=your_password"
    }
  }
  ```
- [ ] حفظ الملف

---

## ✅ استعادة الحزم

- [ ] فتح Terminal في مجلد `src/`
- [ ] تشغيل:
  ```powershell
  dotnet restore
  ```
- [ ] التحقق من عدم وجود أخطاء

---

## ✅ التحقق من الكود

- [ ] فتح المشروع في Visual Studio أو VS Code
- [ ] التحقق من عدم وجود أخطاء في:
  - [ ] `src/Program.cs`
  - [ ] `src/Data/ApplicationDbContext.cs`
  - [ ] `src/Models/*.cs`
  - [ ] `src/Services/*.cs`
  - [ ] `src/Components/**/*.razor`

---

## ✅ تشغيل التطبيق

- [ ] من مجلد `src/`:
  ```powershell
  dotnet run
  ```
- [ ] التحقق من الرسائل:
  - [ ] لا توجد أخطاء في Console
  - [ ] رسالة: "Now listening on: https://localhost:5001"

---

## ✅ اختبار الواجهة

- [ ] فتح المتصفح على: `https://localhost:5001`
- [ ] التحقق من:
  - [ ] صفحة Login تظهر (أو Dashboard إذا لم يكن هناك Authentication)
  - [ ] القائمة الجانبية تظهر
  - [ ] لا توجد أخطاء في Console المتصفح (F12)

---

## ✅ اختبار الاتصال بقاعدة البيانات

### من Blazor Component (اختياري)

- [ ] فتح `src/Components/Pages/Dashboard.razor`
- [ ] إضافة كود تجريبي:
  ```csharp
  @inject ApplicationDbContext Db
  
  @code {
      protected override async Task OnInitializedAsync()
      {
          var count = await Db.Documents.CountAsync();
          // يجب أن يعمل بدون أخطاء
      }
  }
  ```

### من psql (مباشر)

- [ ] الاتصال بقاعدة البيانات:
  ```powershell
  psql -U doc_user -d legal_doc_system
  ```
- [ ] التحقق من الجداول:
  ```sql
  SELECT COUNT(*) FROM users;
  SELECT COUNT(*) FROM documents;
  ```

---

## ✅ اختبار Services

### اختبار DocumentService

- [ ] إنشاء صفحة تجريبية أو استخدام Dashboard
- [ ] حقن Service:
  ```csharp
  @inject IDocumentService DocumentService
  
  @code {
      protected override async Task OnInitializedAsync()
      {
          var documents = await DocumentService.GetAllDocumentsAsync();
          // يجب أن يعمل بدون أخطاء
      }
  }
  ```

---

## ✅ التحقق من Logging

- [ ] التحقق من Console output:
  - [ ] رسائل Logging تظهر
  - [ ] لا توجد أخطاء Database connection
  - [ ] لا توجد أخطاء في Services

---

## ✅ التحقق من Navigation

- [ ] النقر على روابط القائمة الجانبية:
  - [ ] Dashboard (`/`)
  - [ ] Login (`/login`)
  - [ ] Error (`/error` - يجب أن تظهر رسالة "غير موجود")

---

## ⚠️ المشاكل الشائعة

### مشكلة: "Connection string not found"
**الحل:** تأكد من وجود `appsettings.json` في `src/`

### مشكلة: "Table does not exist"
**الحل:** تأكد من تنفيذ `database/schema.sql`

### مشكلة: "Port 5001 already in use"
**الحل:** غيّر البورت في `appsettings.json` أو أغلق التطبيق الذي يستخدم البورت

### مشكلة: "Cannot find namespace"
**الحل:** شغّل `dotnet restore` مرة أخرى

---

## ✅ بعد التحقق

إذا اجتازت جميع النقاط أعلاه:

- [ ] ✅ التطبيق يعمل بشكل صحيح
- [ ] ✅ قاعدة البيانات متصلة
- [ ] ✅ Services تعمل
- [ ] ✅ الواجهة تظهر بدون أخطاء

**يمكنك الآن البدء في تطوير الميزات الإضافية!**

---

## 📝 الخطوات التالية

1. راجع `docs/DECISIONS.md` لفهم القرارات التقنية
2. راجع `docs/PROJECT_STRUCTURE.md` لفهم بنية المشروع
3. راجع `التعديلات النهائية للوثيقة.md` للخطة الكاملة
4. ابدأ بتطبيق Authentication (المرحلة 1)

---

**آخر تحديث:** 2025

