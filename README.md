# 📄 نظام إدارة المستندات القانونية - LegalDocSystem

## 🎯 نظرة عامة

نظام إدارة مستندات قانونية داخلي (on-premises) مبني باستخدام:
- **ASP.NET Core 8** (Backend)
- **Blazor Server** (Frontend)
- **PostgreSQL 14+** (قاعدة البيانات)
- **Tesseract OCR** (معالجة المستندات الممسوحة)

---

## 📁 هيكل التوثيق

تم تنظيم التوثيق في 8 مجلدات:

### 00-Getting-Started
**الملفات الأساسية للبدء:**
- [TECHNICAL_DECISIONS.md](./00-Getting-Started/TECHNICAL_DECISIONS.md) - القرارات التقنية والمعمارية

### 01-Security
**جميع ملفات الأمان (5 ملفات رئيسية):**
- [README.md](./01-Security/README.md) - دليل المجلد
- [SECURITY_OVERVIEW.md](./01-Security/SECURITY_OVERVIEW.md) - نظرة عامة شاملة على الأمان
- [AUTHENTICATION_AND_AUTHORIZATION.md](./01-Security/AUTHENTICATION_AND_AUTHORIZATION.md) - المصادقة والصلاحيات
- [DATA_PROTECTION_AND_ENCRYPTION.md](./01-Security/DATA_PROTECTION_AND_ENCRYPTION.md) - حماية البيانات والتشفير
- [SECURE_CONFIGURATION_AND_INFRASTRUCTURE.md](./01-Security/SECURE_CONFIGURATION_AND_INFRASTRUCTURE.md) - الإعدادات الآمنة والبنية التحتية
- [AUDIT_LOGGING_AND_MONITORING.md](./01-Security/AUDIT_LOGGING_AND_MONITORING.md) - التدقيق والمراقبة

### 02-Testing
**جميع ملفات الاختبار:**
- [README.md](./02-Testing/README.md) - دليل المجلد
- [TESTING_COMPLETE_GUIDE.md](./02-Testing/TESTING_COMPLETE_GUIDE.md) - دليل شامل للاختبار
- [COMPREHENSIVE_TESTING_PLAN.md](./02-Testing/COMPREHENSIVE_TESTING_PLAN.md) - خطة الاختبار الشاملة

### 03-Setup
**جميع أدلة الإعداد:**
- [README.md](./03-Setup/README.md) - دليل المجلد
- [USER_SECRETS_ENV_VARS_GUIDE.md](./03-Setup/USER_SECRETS_ENV_VARS_GUIDE.md) - دليل User Secrets
- [HANGFIRE_GUIDE.md](./03-Setup/HANGFIRE_GUIDE.md) - دليل Hangfire
- [OCR_SETUP.md](./03-Setup/OCR_SETUP.md) - إعداد OCR

### 04-Architecture
**تقارير المراجعة المعمارية:**
- [README.md](./04-Architecture/README.md) - دليل المجلد

### 05-Audit
**ملفات Audit Logging:**
- [README.md](./05-Audit/README.md) - دليل المجلد
- [AUDIT_SERVICE_IMPLEMENTATION.md](./05-Audit/AUDIT_SERVICE_IMPLEMENTATION.md) - تنفيذ Audit Service

### 06-Delivery
**ملفات التسليم:**
- [README.md](./06-Delivery/README.md) - دليل المجلد
- [DELIVERY_NOTE.md](./06-Delivery/DELIVERY_NOTE.md) - ملاحظات التسليم

### 99-Reference
**الملفات المرجعية:**
- [README.md](./99-Reference/README.md) - دليل المجلد

---

## 🚀 البدء السريع

### 1. تثبيت المتطلبات الأساسية

راجع ملف [TECHNICAL_DECISIONS.md](./00-Getting-Started/TECHNICAL_DECISIONS.md) للتعليمات الكاملة للبدء وتشغيل المشروع لأول مرة.

**الحد الأدنى المطلوب:**
- ✅ .NET 8 SDK
- ✅ Git
- ⏳ PostgreSQL 14+
- ⏳ Visual Studio 2022 أو VS Code

### 2. إعداد Configuration الآمن

راجع [03-Setup/USER_SECRETS_ENV_VARS_GUIDE.md](./03-Setup/USER_SECRETS_ENV_VARS_GUIDE.md) لإعداد:
- User Secrets (Development)
- Environment Variables (Production)

### 3. فهم الأمان

راجع [01-Security/README.md](./01-Security/README.md) لفهم:
- الإصلاحات الأمنية المنفذة
- أفضل الممارسات
- قائمة التحقق الأمنية

---

## 📋 الفهرس الكامل

راجع ملفات README في كل مجلد للحصول على:
- قائمة كاملة بجميع الملفات
- روابط مباشرة لكل ملف
- دليل سريع حسب الحاجة

---

## ✅ ما تم إنجازه

### قاعدة البيانات
- ✅ `database/schema.sql` - 14 جدول مع العلاقات والـ Indexes
- ✅ Full-Text Search trigger
- ✅ Foreign Keys و Constraints

### Models & Services
- ✅ 14 Model مع Data Annotations
- ✅ 13 Service (Interface + Implementation)
- ✅ DbContext كامل مع Fluent API

### Blazor Components
- ✅ 17 صفحة Blazor
- ✅ Authentication & Authorization
- ✅ REST API Controllers

### الأمان
- ✅ Security Fixes (Critical + Important)
- ✅ CSRF Protection
- ✅ Rate Limiting
- ✅ Audit Logging

---

## 📚 الوثائق المهمة

### للبدء:
- [TECHNICAL_DECISIONS.md](./00-Getting-Started/TECHNICAL_DECISIONS.md)

### للأمان:
- [01-Security/SECURITY_OVERVIEW.md](./01-Security/SECURITY_OVERVIEW.md)
- [01-Security/COMPREHENSIVE_SECURITY_AUDIT_REPORT.md](./01-Security/COMPREHENSIVE_SECURITY_AUDIT_REPORT.md)

### للإعداد:
- [03-Setup/USER_SECRETS_ENV_VARS_GUIDE.md](./03-Setup/USER_SECRETS_ENV_VARS_GUIDE.md)

### للاختبار:
- [02-Testing/TESTING_COMPLETE_GUIDE.md](./02-Testing/TESTING_COMPLETE_GUIDE.md)
- [02-Testing/COMPREHENSIVE_TESTING_PLAN.md](./02-Testing/COMPREHENSIVE_TESTING_PLAN.md)

---


---

**آخر تحديث:** 2025  
**النسخة:** 2.0
