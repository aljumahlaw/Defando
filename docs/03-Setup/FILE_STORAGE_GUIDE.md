# دليل تخزين الملفات

## نظرة عامة

نظام تخزين الملفات يدعم حفظ المستندات محلياً أو على NAS (Network Attached Storage). الملفات تُحفظ في بنية مجلدات منظمة بناءً على التاريخ.

---

## بنية التخزين

### هيكل المجلدات

الملفات تُحفظ في البنية التالية:

```
BasePath/
├── 2025/
│   ├── 01/
│   │   ├── 15/
│   │   │   ├── {guid}.pdf
│   │   │   ├── {guid}.docx
│   │   │   └── {guid}.jpg
│   │   └── 16/
│   │       └── {guid}.pdf
│   └── 02/
│       └── ...
└── 2026/
    └── ...
```

**الصيغة:** `Year/Month/Day/{GUID}.{Extension}`

**مثال:**
- ملف تم رفعه في 15 يناير 2025: `2025/01/15/a1b2c3d4-e5f6-7890-abcd-ef1234567890.pdf`

### مزايا هذه البنية

1. **تنظيم تلقائي:** الملفات منظمة حسب التاريخ
2. **أداء أفضل:** تقليل عدد الملفات في كل مجلد
3. **نسخ احتياطي أسهل:** يمكن نسخ مجلدات حسب التاريخ
4. **أمان:** استخدام GUID يخفي أسماء الملفات الأصلية

---

## الإعدادات

### appsettings.json

```json
{
  "FileStorage": {
    "BasePath": "D:\\LegalDMS\\Files",
    "MaxFileSizeMB": 100,
    "AllowedExtensions": [".pdf", ".docx", ".jpg", ".png", ".jpeg", ".tiff", ".bmp", ".txt"]
  }
}
```

**المعاملات:**
- `BasePath`: المسار الأساسي لتخزين الملفات
- `MaxFileSizeMB`: الحد الأقصى لحجم الملف (بالميجابايت)
- `AllowedExtensions`: قائمة الامتدادات المسموحة

---

## التخزين المحلي

### إعداد مجلد محلي

1. **إنشاء المجلد:**
   ```powershell
   New-Item -ItemType Directory -Path "D:\LegalDMS\Files" -Force
   ```

2. **تحديث appsettings.json:**
   ```json
   "BasePath": "D:\\LegalDMS\\Files"
   ```

3. **التحقق من الصلاحيات:**
   - تأكد من أن التطبيق لديه صلاحيات القراءة والكتابة
   - في Windows: Properties → Security → Edit Permissions

---

## التخزين على NAS

### إعداد NAS Storage

#### 1. ربط NAS كمجلد شبكي

**Windows:**
```powershell
# ربط NAS كمجلد شبكي
net use Z: \\nas-server\documents /persistent:yes
```

**Linux:**
```bash
# Mount NAS
sudo mount -t cifs //nas-server/documents /mnt/nas -o username=user,password=pass
```

#### 2. تحديث appsettings.json

```json
{
  "FileStorage": {
    "BasePath": "Z:\\LegalDMS\\Files",
    "MaxFileSizeMB": 100,
    "AllowedExtensions": [".pdf", ".docx", ".jpg", ".png"]
  }
}
```

**أو على Linux:**
```json
{
  "FileStorage": {
    "BasePath": "/mnt/nas/LegalDMS/Files",
    "MaxFileSizeMB": 100,
    "AllowedExtensions": [".pdf", ".docx", ".jpg", ".png"]
  }
}
```

#### 3. التحقق من الاتصال

```csharp
var fileStorageService = serviceProvider.GetRequiredService<IFileStorageService>();
var exists = await fileStorageService.FileExistsAsync("test.txt");
```

---

## النسخ الاحتياطي

### استراتيجية النسخ الاحتياطي

#### 1. نسخ احتياطي يومي

**Windows (Task Scheduler):**
```powershell
# Script backup.ps1
$source = "D:\LegalDMS\Files"
$destination = "\\backup-server\backups\$(Get-Date -Format 'yyyy-MM-dd')"
Robocopy $source $destination /MIR /R:3 /W:5
```

**Linux (Cron):**
```bash
# Crontab entry
0 2 * * * rsync -av /mnt/nas/LegalDMS/Files/ /backup/$(date +\%Y-\%m-\%d)/
```

#### 2. نسخ احتياطي تدريجي

**Windows:**
```powershell
# Incremental backup
Robocopy $source $destination /MIR /XO /R:3 /W:5
```

**Linux:**
```bash
# Incremental backup
rsync -av --delete /mnt/nas/LegalDMS/Files/ /backup/
```

#### 3. نسخ احتياطي سحابي

**Azure Blob Storage:**
```csharp
// TODO: Implement Azure Blob Storage integration
```

**AWS S3:**
```csharp
// TODO: Implement AWS S3 integration
```

---

## الأمان

### أفضل الممارسات

1. **صلاحيات الملفات:**
   - تقييد الوصول للملفات (Read/Write للمستخدمين المصرح لهم فقط)
   - استخدام Windows ACL أو Linux chmod

2. **تشفير الملفات:**
   ```csharp
   // TODO: Implement file encryption for sensitive documents
   ```

3. **فحص الفيروسات:**
   - دمج فحص الفيروسات قبل حفظ الملفات
   - استخدام Windows Defender أو ClamAV

4. **مراقبة الوصول:**
   - تسجيل جميع عمليات الوصول في AuditLog
   - مراقبة محاولات الوصول غير المصرح بها

---

## الأداء

### تحسين الأداء

1. **SSD Storage:**
   - استخدام SSD للتخزين المحلي
   - تحسين سرعة القراءة/الكتابة

2. **Network Optimization:**
   - استخدام Gigabit Ethernet للـ NAS
   - تقليل Latency بين الخادم والـ NAS

3. **Caching:**
   ```csharp
   // TODO: Implement file caching for frequently accessed files
   ```

4. **Compression:**
   ```csharp
   // TODO: Implement file compression for large files
   ```

---

## استكشاف الأخطاء

### مشكلة: "File not found"

**الحل:**
1. تحقق من المسار في `appsettings.json`
2. تأكد من وجود المجلد
3. تحقق من الصلاحيات

### مشكلة: "Access denied"

**الحل:**
1. تحقق من صلاحيات المجلد
2. تأكد من أن التطبيق يعمل بصلاحيات كافية
3. في Windows: Run as Administrator

### مشكلة: "Network path not found" (NAS)

**الحل:**
1. تحقق من اتصال الشبكة
2. تأكد من أن NAS متاح
3. تحقق من credentials
4. جرب ربط المجلد يدوياً أولاً

### مشكلة: "File size exceeds maximum"

**الحل:**
1. زيادة `MaxFileSizeMB` في `appsettings.json`
2. أو ضغط الملف قبل الرفع

---

## المراقبة

### مراقبة المساحة

**Windows:**
```powershell
Get-ChildItem "D:\LegalDMS\Files" -Recurse | 
    Measure-Object -Property Length -Sum | 
    Select-Object @{Name="Size(GB)";Expression={[math]::Round($_.Sum / 1GB, 2)}}
```

**Linux:**
```bash
du -sh /mnt/nas/LegalDMS/Files
```

### تنظيف الملفات القديمة

```csharp
// TODO: Implement cleanup job for old files
// Delete files older than X years
```

---

## التكامل مع Hangfire

### مهام تنظيف تلقائية

```csharp
RecurringJob.AddOrUpdate<FileStorageService>(
    "cleanup-old-files",
    x => x.CleanupOldFilesAsync(TimeSpan.FromYears(7)),
    Cron.Daily);
```

---

## المراجع

- [Windows File Permissions](https://docs.microsoft.com/en-us/windows/security/identity-protection/access-control/)
- [Linux File Permissions](https://www.linux.com/training-tutorials/understanding-linux-file-permissions/)
- [NAS Best Practices](https://www.synology.com/en-global/knowledgebase/DSM/help/DSM/AdminCenter/file_winmacmac_nfs_permission)

---

**آخر تحديث:** 2025

