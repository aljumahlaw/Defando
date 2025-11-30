# إعداد المشروع لرفعه إلى GitHub
## GitHub Setup Guide

**التاريخ:** 2025-11-29  
**الحالة:** ✅ **جاهز**

---

## 📋 الملفات المطلوبة

تم إنشاء الملفات التالية لضمان أمان المشروع عند رفعه إلى GitHub:

### ✅ 1. `.gitignore`
- يستثني جميع الملفات الحساسة من Git
- يتضمن استثناءات .NET القياسية
- يستثني `appsettings.Development.json` و `.env` و `docs/Archive/`

### ✅ 2. `appsettings.Development.example.json`
- **قالب فقط** - لا يحتوي على كلمات مرور حقيقية
- يحتوي على أمثلة لجميع الإعدادات المطلوبة
- **لا ترفع `appsettings.Development.json` الحقيقي إلى Git!**

### ✅ 3. `.env.example`
- **قالب فقط** - لا يحتوي على أسرار حقيقية
- يحتوي على أمثلة لمتغيرات البيئة بصيغة ASP.NET Core
- **لا ترفع `.env` الحقيقي إلى Git!**

### ✅ 4. `prepare_for_github.bat`
- سكربت للتحقق من جاهزية المشروع قبل الرفع
- يتحقق من عدم وجود ملفات حساسة

---

## ⚠️ تحذيرات مهمة

### ❌ لا ترفع هذه الملفات إلى Git:

1. **`appsettings.Development.json`** - يحتوي على إعدادات محلية
2. **`appsettings.Production.json`** - يحتوي على إعدادات الإنتاج
3. **`.env`** - يحتوي على متغيرات البيئة الحقيقية
4. **`secrets.json`** - ملفات الأسرار
5. **`docs/Archive/`** - مجلد الأرشيف

### ✅ الملفات الآمنة للرفع:

1. **`appsettings.json`** - الإعدادات العامة (بدون كلمات مرور)
2. **`appsettings.Development.example.json`** - قالب للإعدادات
3. **`.env.example`** - قالب لمتغيرات البيئة
4. **`.gitignore`** - ملف الاستثناءات

---

## 🚀 خطوات الرفع إلى GitHub

### 1. التحقق من الملفات:

```bash
# شغّل سكربت التحقق
prepare_for_github.bat
```

### 2. التحقق من .gitignore:

```bash
git status
```

تأكد من أن الملفات التالية **لا تظهر** في القائمة:
- `appsettings.Development.json`
- `.env`
- `docs/Archive/`

### 3. إضافة الملفات الآمنة:

```bash
git add .gitignore
git add appsettings.Development.example.json
git add .env.example
git add src/appsettings.json
```

### 4. Commit و Push:

```bash
git commit -m "chore: add GitHub setup files (.gitignore, example configs)"
git push origin main
```

---

## 📝 كيفية استخدام الملفات القالبية

### للمطورين الجدد:

1. **انسخ `appsettings.Development.example.json`:**
   ```bash
   copy appsettings.Development.example.json src\appsettings.Development.json
   ```

2. **عدّل `src/appsettings.Development.json`:**
   - استبدل `YourDevelopmentPasswordHere` بكلمة المرور الحقيقية
   - استبدل `YourSmtpPasswordHere` بكلمة مرور SMTP الحقيقية
   - **⚠️ لا ترفع هذا الملف إلى Git!**

3. **أو استخدم User Secrets (موصى به):**
   ```bash
   cd src
   dotnet user-secrets set "Database:Password" "YourRealPassword"
   dotnet user-secrets set "Smtp:Password" "YourRealSmtpPassword"
   ```

### للإنتاج:

استخدم Environment Variables أو Azure Key Vault. راجع:
- [docs/03-Setup/USER_SECRETS_ENV_VARS_GUIDE.md](./docs/03-Setup/USER_SECRETS_ENV_VARS_GUIDE.md)

---

## ✅ التحقق النهائي

قبل الرفع، تأكد من:

- [ ] `.gitignore` موجود ويستثني الملفات الحساسة
- [ ] `appsettings.Development.json` **غير موجود** في Git
- [ ] `.env` **غير موجود** في Git
- [ ] `docs/Archive/` **غير موجود** في Git
- [ ] جميع الملفات القالبية تحتوي على **أمثلة فقط** (لا أسرار حقيقية)

---

**آخر تحديث:** 2025-11-29  
**الحالة:** ✅ **جاهز للرفع إلى GitHub**


