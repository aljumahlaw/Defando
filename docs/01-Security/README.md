# 01-Security

## 📋 نظرة عامة

هذا المجلد يحتوي على جميع ملفات الأمان والتوثيق الأمني للمشروع.

**الهيكل الجديد:** تم دمج جميع ملفات الأمان في **5 ملفات رئيسية** منظمة وواضحة.

---

## 📁 الملفات الرئيسية (5 ملفات)

### 1. [SECURITY_OVERVIEW.md](./SECURITY_OVERVIEW.md)
**نظرة عامة شاملة على الأمان**

- التقييم العام (8.2/10)
- نقاط القوة (15)
- الثغرات الحرجة (تم إصلاحها ✅)
- النقاط المهمة (تم إصلاحها ✅)
- قائمة فحص الأمان الشاملة

---

### 2. [AUTHENTICATION_AND_AUTHORIZATION.md](./AUTHENTICATION_AND_AUTHORIZATION.md)
**المصادقة والصلاحيات**

- Cookie Authentication
- Claims-based Authorization
- Role-based Access Control
- Rate Limiting
- Account Lockout
- Session Management
- Password Security

---

### 3. [DATA_PROTECTION_AND_ENCRYPTION.md](./DATA_PROTECTION_AND_ENCRYPTION.md)
**حماية البيانات والتشفير**

- Password Hashing (BCrypt)
- Data Encryption (DPAPI/AES)
- Email Security
- Connection String Security
- Sensitive Data Storage

---

### 4. [SECURE_CONFIGURATION_AND_INFRASTRUCTURE.md](./SECURE_CONFIGURATION_AND_INFRASTRUCTURE.md)
**الإعدادات الآمنة والبنية التحتية**

- CSRF Protection
- Path Traversal Protection
- Error Handling
- Cookie Security
- HSTS
- CORS
- Middleware Pipeline
- AllowedHosts

---

### 5. [AUDIT_LOGGING_AND_MONITORING.md](./AUDIT_LOGGING_AND_MONITORING.md)
**التدقيق والمراقبة**

- Audit Logging
- Audit Log Sanitization
- Security Testing
- Monitoring

---

## 🔗 روابط مهمة

- [دليل الإعداد](../03-Setup/USER_SECRETS_ENV_VARS_GUIDE.md) - إعداد Configuration الآمن
- [دليل الاختبار](../02-Testing/) - اختبارات الأمان

---

**آخر تحديث:** 2025  
**الحالة:** ✅ **منظم ومكتمل**
