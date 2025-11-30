# دليل استخدام User Secrets و Environment Variables
## User Secrets and Environment Variables Guide

**التاريخ:** 2025  
**الإصدار:** 1.0

---

## 📋 نظرة عامة

هذا الدليل يشرح كيفية استخدام **User Secrets** (للبيئة التطويرية) و **Environment Variables** (للبيئة الإنتاجية) لتخزين البيانات الحساسة بشكل آمن في مشروع LegalDocSystem.

---

## 🔒 مبادئ التهيئة الآمنة (Secure Configuration)

### لماذا لا نضع كلمات المرور في appsettings.json؟

**المشكلة:**
- ملفات `appsettings.json` تُرفع عادة إلى Git
- كلمات المرور في Git = خطر أمني كبير
- أي شخص لديه وصول للمستودع يمكنه رؤية كلمات المرور

**الحل:**
- استخدام **User Secrets** في بيئة التطوير
- استخدام **Environment Variables** في بيئة الإنتاج
- إبقاء `appsettings.json` بدون كلمات مرور فعلية

### التغييرات المنفذة

#### 1. إضافة UserSecretsId إلى .csproj

**الملف:** `src/LegalDocSystem.csproj`

```xml
<PropertyGroup>
  <UserSecretsId>LegalDocSystem-2025-01-15</UserSecretsId>
</PropertyGroup>
```

✅ **تم التنفيذ**

#### 2. تعديل appsettings.json

**الملف:** `src/appsettings.json`

**قبل:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=LegalDocDb;Username=postgres;Password=ChangeMe;"
  }
}
```

**بعد:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=LegalDocDb;Username=postgres;Password=;"
  }
}
```

✅ **تم التنفيذ**

#### 3. تعديل Program.cs

**الملف:** `src/Program.cs`

```csharp
// Get connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Override password from environment variable or User Secrets (secure storage)
var dbPassword = builder.Configuration["Database:Password"]
    ?? Environment.GetEnvironmentVariable("LEGALDOC_DB_PASSWORD")
    ?? throw new InvalidOperationException(
        "Database password not configured. Set LEGALDOC_DB_PASSWORD environment variable or use User Secrets: dotnet user-secrets set \"Database:Password\" \"YourPassword\"");

// Replace password placeholder in connection string
if (connectionString.Contains("Password=;") || connectionString.Contains("Password=YOUR_PASSWORD_HERE"))
{
    connectionString = connectionString.Replace("Password=;", $"Password={dbPassword};")
                                      .Replace("Password=YOUR_PASSWORD_HERE;", $"Password={dbPassword};");
}
```

✅ **تم التنفيذ**

### أمثلة قراءة Configuration

#### 1. قراءة Connection String:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
```

#### 2. قراءة قيمة بسيطة:

```csharp
var baseUrl = builder.Configuration["EmailNotifications:BaseUrl"] 
    ?? "https://localhost:5001";
```

#### 3. قراءة قسم كامل:

```csharp
var ocrSettings = builder.Configuration.GetSection("Ocr");
var tesseractPath = ocrSettings["TesseractPath"];
```

#### 4. قراءة من User Secrets:

```csharp
var dbPassword = builder.Configuration["Database:Password"];
```

#### 5. قراءة من Environment Variables:

```csharp
var dbPassword = Environment.GetEnvironmentVariable("LEGALDOC_DB_PASSWORD");
```

---

## 📝 خطوات إعداد التهيئة الآمنة (Development و Production)

### للتطوير (Development):

#### الخطوة 1: تهيئة User Secrets

```bash
cd src
dotnet user-secrets init
```

**ملاحظة:** إذا كان `UserSecretsId` موجوداً في `.csproj`، يمكنك تخطي هذه الخطوة.

#### الخطوة 2: إضافة Database Password

```bash
dotnet user-secrets set "Database:Password" "YourActualPassword"
```

**استبدل `YourActualPassword` بكلمة المرور الفعلية لقاعدة البيانات.**

#### الخطوة 3: التحقق

```bash
dotnet user-secrets list
```

**النتيجة المتوقعة:**
```
Database:Password = YourActualPassword
```

### للإنتاج (Production):

#### الخطوة 1: إعداد Environment Variable

**في Windows:**
```cmd
setx LEGALDOC_DB_PASSWORD "YourProductionPassword"
```

**في Linux/macOS:**
```bash
export LEGALDOC_DB_PASSWORD="YourProductionPassword"
```

**في Azure App Service:**
1. اذهب إلى Azure Portal
2. App Service → Configuration
3. Application settings → New application setting
4. **Name:** `LEGALDOC_DB_PASSWORD`
5. **Value:** `YourProductionPassword`

#### الخطوة 2: التحقق

**في Windows:**
```cmd
echo %LEGALDOC_DB_PASSWORD%
```

**في Linux/macOS:**
```bash
echo $LEGALDOC_DB_PASSWORD
```

### كيفية عمل النظام

#### ترتيب أولوية Configuration:

1. **`appsettings.json`** (أقل أولوية)
2. **`appsettings.Development.json`** (في Development)
3. **User Secrets** (في Development فقط)
4. **Environment Variables** (أعلى أولوية)

**مثال:**

| المصدر | القيمة |
|---|---|
| `appsettings.json` | `Password=YOUR_PASSWORD_HERE` |
| User Secrets | `Password=MyDevPassword` |
| **النتيجة** | `Password=MyDevPassword` ✅ |

### ⚠️ ملاحظات مهمة

#### 1. SMTP Password

**ملاحظة:** SMTP Password محفوظة في قاعدة البيانات (جدول `settings`) وليس في `appsettings.json`. 

**لذلك:**
- ✅ لا حاجة لنقل SMTP Password إلى User Secrets
- ✅ SMTP Password مشفرة في قاعدة البيانات
- ⚠️ **مستقبلاً:** يمكن استخدام User Secrets أو Environment Variables لتخزين مفتاح التشفير

#### 2. .gitignore

تأكد من أن `.gitignore` يحتوي على:

```
**/appsettings.Development.json
**/secrets.json
```

**ملاحظة:** `appsettings.json` و `appsettings.Development.json` **يجب** رفعهما إلى Git (بدون كلمات مرور فعلية).

#### 3. أمان إضافي

**للإنتاج:**
- ✅ استخدم HTTPS دائماً
- ✅ استخدم `CookieSecurePolicy.Always` في `Program.cs`
- ✅ استخدم Environment Variables بدلاً من User Secrets
- ✅ لا ترفع `appsettings.Production.json` إلى Git إذا كان يحتوي على بيانات حساسة

---

## 🔐 البيانات الحساسة التي يجب تخزينها بشكل آمن

1. **Database Password** - كلمة مرور قاعدة البيانات
2. **Encryption Key** - مفتاح التشفير
3. **SMTP Password** - كلمة مرور البريد الإلكتروني (إذا كانت مخزنة في Settings)
4. **API Keys** - مفاتيح API الخارجية (إذا كانت موجودة)

---

## 💻 للبيئة التطويرية: User Secrets

### ما هي User Secrets؟

User Secrets هي طريقة آمنة لتخزين البيانات الحساسة في بيئة التطوير. البيانات مخزنة خارج المشروع ولا يتم رفعها إلى Git.

### الموقع

User Secrets مخزنة في:
- **Windows:** `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
- **Linux/macOS:** `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`

### كيفية الاستخدام

#### 1. التحقق من UserSecretsId

افتح `src/LegalDocSystem.csproj` وتحقق من:
```xml
<UserSecretsId>LegalDocSystem-2025-01-15</UserSecretsId>
```

#### 2. إضافة Database Password

```bash
cd src
dotnet user-secrets set "Database:Password" "YourDatabasePassword"
```

#### 3. إضافة Encryption Key

```bash
dotnet user-secrets set "Encryption:Key" "YourEncryptionKey32CharactersLong!"
```

#### 4. عرض جميع Secrets

```bash
dotnet user-secrets list
```

#### 5. حذف Secret

```bash
dotnet user-secrets remove "Database:Password"
```

#### 6. مسح جميع Secrets

```bash
dotnet user-secrets clear
```

### مثال كامل

```bash
# الانتقال لمجلد المشروع
cd src

# إضافة Database Password
dotnet user-secrets set "Database:Password" "MySecurePassword123"

# إضافة Encryption Key
dotnet user-secrets set "Encryption:Key" "MyEncryptionKey32CharactersLong!"

# التحقق من الإضافة
dotnet user-secrets list
```

**النتيجة المتوقعة:**
```
Database:Password = MySecurePassword123
Encryption:Key = MyEncryptionKey32CharactersLong!
```

---

## 🌐 للبيئة الإنتاجية: Environment Variables

### ما هي Environment Variables؟

Environment Variables هي متغيرات بيئة النظام المستخدمة لتخزين البيانات الحساسة في بيئة الإنتاج. هذه الطريقة آمنة ولا يتم رفعها إلى Git.

### كيفية الاستخدام

#### Windows (PowerShell)

```powershell
# Database Password
$env:LEGALDOC_DB_PASSWORD="YourDatabasePassword"

# Encryption Key
$env:LEGALDOC_ENCRYPTION_KEY="YourEncryptionKey32CharactersLong!"

# التحقق
echo $env:LEGALDOC_DB_PASSWORD
```

#### Windows (Command Prompt)

```cmd
set LEGALDOC_DB_PASSWORD=YourDatabasePassword
set LEGALDOC_ENCRYPTION_KEY=YourEncryptionKey32CharactersLong!
```

#### Linux/macOS (Bash)

```bash
# Database Password
export LEGALDOC_DB_PASSWORD="YourDatabasePassword"

# Encryption Key
export LEGALDOC_ENCRYPTION_KEY="YourEncryptionKey32CharactersLong!"

# التحقق
echo $LEGALDOC_DB_PASSWORD
```

#### Linux/macOS (Permanent - في ~/.bashrc أو ~/.zshrc)

```bash
# إضافة إلى ملف ~/.bashrc أو ~/.zshrc
export LEGALDOC_DB_PASSWORD="YourDatabasePassword"
export LEGALDOC_ENCRYPTION_KEY="YourEncryptionKey32CharactersLong!"

# إعادة تحميل
source ~/.bashrc  # أو source ~/.zshrc
```

---

## 🐳 Docker Environment Variables

### في docker-compose.yml

```yaml
services:
  legaldocsystem:
    environment:
      - LEGALDOC_DB_PASSWORD=YourDatabasePassword
      - LEGALDOC_ENCRYPTION_KEY=YourEncryptionKey32CharactersLong!
    # أو استخدام ملف .env
    env_file:
      - .env
```

### في ملف .env

```env
LEGALDOC_DB_PASSWORD=YourDatabasePassword
LEGALDOC_ENCRYPTION_KEY=YourEncryptionKey32CharactersLong!
```

---

## ☁️ Azure App Service

### استخدام Azure Portal

1. اذهب إلى **Configuration** → **Application settings**
2. أضف:
   - `LEGALDOC_DB_PASSWORD` = `YourDatabasePassword`
   - `LEGALDOC_ENCRYPTION_KEY` = `YourEncryptionKey32CharactersLong!`

### استخدام Azure CLI

```bash
az webapp config appsettings set \
  --resource-group YourResourceGroup \
  --name YourAppName \
  --settings \
    LEGALDOC_DB_PASSWORD="YourDatabasePassword" \
    LEGALDOC_ENCRYPTION_KEY="YourEncryptionKey32CharactersLong!"
```

---

## 🔧 AWS / Linux Servers

### استخدام systemd service file

إنشاء `/etc/systemd/system/legaldocsystem.service`:

```ini
[Service]
Environment="LEGALDOC_DB_PASSWORD=YourDatabasePassword"
Environment="LEGALDOC_ENCRYPTION_KEY=YourEncryptionKey32CharactersLong!"
```

### استخدام .env file (مع حماية)

```bash
# إنشاء ملف .env
sudo nano /var/www/legaldocsystem/.env

# إضافة المتغيرات
LEGALDOC_DB_PASSWORD=YourDatabasePassword
LEGALDOC_ENCRYPTION_KEY=YourEncryptionKey32CharactersLong!

# حماية الملف
sudo chmod 600 /var/www/legaldocsystem/.env
sudo chown www-data:www-data /var/www/legaldocsystem/.env
```

---

## 📝 قائمة Environment Variables المطلوبة

### للإنتاج

| المتغير | الوصف | مثال |
|---|---|---|
| `LEGALDOC_DB_PASSWORD` | كلمة مرور قاعدة البيانات | `MySecurePassword123` |
| `LEGALDOC_ENCRYPTION_KEY` | مفتاح التشفير (32+ حرف) | `MyEncryptionKey32CharactersLong!` |

### اختياري

| المتغير | الوصف | مثال |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | بيئة التطبيق | `Production` |
| `ASPNETCORE_URLS` | URLs للتطبيق | `http://localhost:5000` |

---

## ✅ Checklist الإعداد

### للبيئة التطويرية:

- [ ] ✅ تعيين `Database:Password` في User Secrets
- [ ] ✅ تعيين `Encryption:Key` في User Secrets (اختياري)
- [ ] ✅ التحقق من `dotnet user-secrets list`
- [ ] ✅ اختبار التطبيق يعمل

### للبيئة الإنتاجية:

- [ ] ✅ تعيين `LEGALDOC_DB_PASSWORD` Environment Variable
- [ ] ✅ تعيين `LEGALDOC_ENCRYPTION_KEY` Environment Variable
- [ ] ✅ التحقق من المتغيرات (`echo $LEGALDOC_DB_PASSWORD`)
- [ ] ✅ اختبار التطبيق يعمل
- [ ] ✅ التأكد من عدم وجود المتغيرات في Git

---

## 🔒 أفضل الممارسات

### 1. لا ترفع Secrets إلى Git

✅ **صحيح:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=LegalDocDb;Username=postgres;Password=;"
  }
}
```

❌ **خطأ:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=LegalDocDb;Username=postgres;Password=MyPassword123;"
  }
}
```

### 2. استخدم كلمات مرور قوية

✅ **صحيح:**
- 16+ حرف
- مزيج من أحرف كبيرة وصغيرة وأرقام ورموز
- لا تستخدم كلمات شائعة

❌ **خطأ:**
- `password123`
- `admin`
- `123456`

### 3. استخدم Encryption Key قوي

✅ **صحيح:**
- 32+ حرف
- عشوائي
- مزيج من أحرف كبيرة وصغيرة وأرقام ورموز

❌ **خطأ:**
- `MyKey123`
- `LegalDocSystem2025`

### 4. راجع Secrets بانتظام

- غيّر كلمات المرور كل 90 يوم
- راجع من لديه وصول للـ Secrets
- استخدم Key Rotation

---

## 🧪 الاختبار

### اختبار User Secrets

```bash
# إضافة Secret
dotnet user-secrets set "Database:Password" "TestPassword123"

# تشغيل التطبيق
dotnet run

# التحقق من أن التطبيق يعمل
# يجب أن يتصل بقاعدة البيانات بنجاح
```

### اختبار Environment Variables

```bash
# تعيين المتغير
export LEGALDOC_DB_PASSWORD="TestPassword123"

# تشغيل التطبيق
dotnet run

# التحقق من أن التطبيق يعمل
# يجب أن يتصل بقاعدة البيانات بنجاح
```

---

## 🚨 استكشاف الأخطاء

### المشكلة: "Database password not configured"

**السبب:** لم يتم تعيين Password

**الحل:**
```bash
# Development
dotnet user-secrets set "Database:Password" "YourPassword"

# Production
export LEGALDOC_DB_PASSWORD="YourPassword"
```

### المشكلة: "Invalid connection string"

**السبب:** Password غير صحيح أو Connection String غير صحيح

**الحل:**
- تحقق من Password
- تحقق من Connection String في `appsettings.json`
- تحقق من Environment Variables

### المشكلة: User Secrets لا تعمل

**السبب:** UserSecretsId غير موجود في `.csproj`

**الحل:**
```xml
<PropertyGroup>
  <UserSecretsId>LegalDocSystem-2025-01-15</UserSecretsId>
</PropertyGroup>
```

---

## 📚 مراجع إضافية

- [ASP.NET Core User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Environment Variables in .NET](https://learn.microsoft.com/en-us/dotnet/api/system.environment.getenvironmentvariable)
- [Azure App Service Configuration](https://learn.microsoft.com/en-us/azure/app-service/configure-common)

---

**آخر تحديث:** 2025  
**الحالة:** ✅ **جاهز للاستخدام**

