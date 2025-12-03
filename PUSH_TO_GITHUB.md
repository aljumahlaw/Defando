# دليل دفع التغييرات إلى GitHub

## الخطوات السريعة:

### 1. إضافة جميع الملفات المعدلة
```powershell
cd "C:\Users\HP\Desktop\تفكير كلو\LegalDocSystem"
git add .
```

### 2. إنشاء Commit مع رسالة واضحة
```powershell
git commit -m "Fix critical issues and add production hardening improvements

- Fix: Full-text search using proper PostgreSQL tsvector
- Fix: Add pagination to GetAllDocumentsAsync
- Fix: Save LastLogin timestamp in database
- Fix: Fix LogoutAsync reading session before clear
- Fix: Reorder middleware (UseSession before UseAuthentication)
- Add: Database index on documents.uploaded_at
- Add: Create initial DocumentVersion on upload
- Add: Health Checks endpoint (/healthz)
- Add: Redis Session support for production (config-based)
- Add: ILogger to AuthService and DocumentService for better observability"
```

### 3. دفع التغييرات إلى GitHub
```powershell
git push origin main
```

---

## ملاحظات مهمة:

- **إذا طلب منك إدخال username/password:**
  - استخدم GitHub Personal Access Token بدلاً من كلمة المرور
  - أو استخدم Git Credential Manager

- **إذا كانت هناك تغييرات على GitHub لم تكن لديك:**
  - أولاً: `git pull origin main`
  - ثم: `git push origin main`

- **للتحقق من التغييرات قبل الـ push:**
  ```powershell
  git status
  git log --oneline -5
  ```

