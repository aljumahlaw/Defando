# تحليل فشل GitHub Actions

## السبب الفعلي للفشل

### 1. عدم وجود ملف CI/CD
**المشكلة**: لا يوجد ملف `.github/workflows/build.yml` في المشروع.

**الحل**: تم إنشاء ملف CI/CD كامل في `.github/workflows/build.yml`

---

### 2. الاعتماديات المفقودة في CI

#### أ. PostgreSQL Service
**المشكلة**: المشروع يحتاج PostgreSQL لكن لم يتم إضافته كـ service في CI.

**الحل**: تم إضافة PostgreSQL service:
```yaml
services:
  postgres:
    image: postgres:16
    env:
      POSTGRES_DB: LegalDocDb
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - 5432:5432
```

#### ب. Tesseract OCR
**المشكلة**: المشروع يستخدم Tesseract OCR لكن لم يتم تثبيته في CI.

**الحل**: تم إضافة خطوة تثبيت Tesseract:
```yaml
- name: Install Tesseract OCR
  run: |
    sudo apt-get update
    sudo apt-get install -y tesseract-ocr tesseract-ocr-ara tesseract-ocr-eng
```

#### ج. متغيرات البيئة
**المشكلة**: المشروع يحتاج متغيرات بيئة محددة (ConnectionString, TesseractPath, etc.)

**الحل**: تم إضافة جميع المتغيرات المطلوبة في كل خطوة.

---

### 3. مشكلة التوافق مع Linux

#### أ. OcrService.cs يستخدم Windows فقط
**المشكلة**: الكود يستخدم `tesseract.exe` (Windows فقط) ولا يدعم Linux.

**الحل**: تم تعديل `OcrService.cs` ليدعم كلا النظامين:
- Windows: `tesseract.exe`
- Linux: `tesseract`

#### ب. مسار Tesseract الافتراضي
**المشكلة**: المسار الافتراضي `C:\Program Files\Tesseract-OCR` لا يعمل في Linux.

**الحل**: تم إضافة كشف تلقائي للنظام التشغيل:
- Windows: `C:\Program Files\Tesseract-OCR`
- Linux: `/usr/bin`

---

## الملفات المطلوبة للتشغيل في Linux

### 1. appsettings.Production.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=LegalDocDb;Username=postgres;Password=postgres;"
  },
  "Ocr": {
    "TesseractPath": "/usr/bin",
    "Language": "ara+eng",
    "Enabled": true
  },
  "FileStorage": {
    "BasePath": "/var/legal-dms/files"
  }
}
```

### 2. متغيرات البيئة المطلوبة
- `LEGALDOC_DB_PASSWORD`: كلمة مرور قاعدة البيانات
- `ConnectionStrings__DefaultConnection`: سلسلة الاتصال بقاعدة البيانات
- `Ocr__TesseractPath`: مسار Tesseract
- `Ocr__Enabled`: تفعيل/تعطيل OCR
- `FileStorage__BasePath`: مسار تخزين الملفات

---

## قائمة apt packages المطلوبة

```bash
# الحزم الأساسية
tesseract-ocr          # Tesseract OCR الأساسي
tesseract-ocr-ara      # اللغة العربية
tesseract-ocr-eng      # اللغة الإنجليزية
```

راجع `APT_PACKAGES_REQUIRED.md` للتفاصيل الكاملة.

---

## التحقق من نجاح CI

### الخطوات التي يجب أن تمر بنجاح:
1. ✅ Checkout code
2. ✅ Setup .NET 8
3. ✅ Install Tesseract OCR
4. ✅ Verify Tesseract installation
5. ✅ Cache NuGet packages
6. ✅ Restore dependencies
7. ✅ Build (Release)
8. ✅ Run tests
9. ✅ Publish

---

## ملاحظات مهمة

1. **PostgreSQL**: يجب أن يكون service جاهز قبل البدء في Build
2. **Tesseract**: يجب تثبيته قبل Build لأن الكود يتحقق من وجوده
3. **متغيرات البيئة**: يجب تعيينها في كل خطوة تحتاجها
4. **FileStorage**: في CI، استخدم `/tmp/legal-dms/files` (مؤقت)

---

## اختبار محلي

```powershell
# 1. اختبر التشغيل محلياً
cd "C:\Users\HP\Desktop\تفكير كلو_2\LegalDocSystem\src"
dotnet run

# 2. تحقق من appsettings.json
type appsettings.json | findstr "ConnectionString\|Tesseract"
```

---

## المراجع

- `.github/workflows/build.yml` - ملف CI/CD الكامل
- `APT_PACKAGES_REQUIRED.md` - قائمة الحزم المطلوبة
- `appsettings.Production.json` - إعدادات الإنتاج

