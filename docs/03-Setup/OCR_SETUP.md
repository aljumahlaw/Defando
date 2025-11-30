# دليل إعداد Tesseract OCR

## نظرة عامة

Tesseract OCR هو محرك OCR مفتوح المصدر يدعم أكثر من 100 لغة، بما في ذلك العربية. يستخدم هذا المشروع Tesseract لاستخراج النص من المستندات الممسوحة ضوئياً.

---

## التثبيت على Windows

### 1. تحميل Tesseract

**الرابط:** https://github.com/UB-Mannheim/tesseract/wiki

**الخيارات:**
- **Tesseract 5.x** (الأحدث - موصى به)
- **Tesseract 4.x** (مستقر)

**ملاحظة:** استخدم نسخة Windows من UB-Mannheim (أفضل دعم للعربية).

### 2. تثبيت Tesseract

1. قم بتحميل ملف التثبيت (`.exe`)
2. شغّل المثبت كمسؤول (Run as Administrator)
3. اختر مسار التثبيت الافتراضي:
   ```
   C:\Program Files\Tesseract-OCR
   ```
4. أثناء التثبيت، تأكد من اختيار:
   - ✅ **Additional language data (Arabic)**
   - ✅ **Additional language data (English)**

### 3. التحقق من التثبيت

افتح PowerShell كمسؤول:

```powershell
cd "C:\Program Files\Tesseract-OCR"
.\tesseract.exe --version
```

يجب أن ترى:
```
tesseract 5.3.0
```

### 4. التحقق من دعم اللغة العربية

```powershell
.\tesseract.exe --list-langs
```

يجب أن ترى `ara` في القائمة.

---

## تحميل ملفات اللغة العربية

### الطريقة 1: أثناء التثبيت

- تأكد من اختيار **Additional language data (Arabic)** أثناء التثبيت.

### الطريقة 2: تحميل يدوي

1. **تحميل ملف اللغة:**
   - الرابط: https://github.com/tesseract-ocr/tessdata
   - ملف `ara.traineddata` (للغة العربية فقط)
   - ملف `ara+eng.traineddata` (للغة العربية والإنجليزية معاً - موصى به)

2. **نسخ الملف:**
   - انسخ الملف إلى: `C:\Program Files\Tesseract-OCR\tessdata\`

3. **التحقق:**
   ```powershell
   .\tesseract.exe --list-langs
   ```
   يجب أن ترى `ara` في القائمة.

---

## إعداد متغير المسار (PATH)

### الطريقة 1: إضافة يدوياً

1. افتح **System Properties** → **Environment Variables**
2. في **System variables**، ابحث عن `Path`
3. انقر **Edit**
4. أضف:
   ```
   C:\Program Files\Tesseract-OCR
   ```
5. انقر **OK** في جميع النوافذ

### الطريقة 2: استخدام PowerShell (كمسؤول)

```powershell
[Environment]::SetEnvironmentVariable(
    "Path",
    [Environment]::GetEnvironmentVariable("Path", "Machine") + ";C:\Program Files\Tesseract-OCR",
    "Machine"
)
```

### التحقق من PATH

افتح PowerShell جديد:

```powershell
tesseract --version
```

يجب أن يعمل بدون تحديد المسار الكامل.

---

## التثبيت على Linux

### Ubuntu/Debian

```bash
sudo apt-get update
sudo apt-get install tesseract-ocr
sudo apt-get install tesseract-ocr-ara  # للغة العربية
sudo apt-get install tesseract-ocr-eng  # للغة الإنجليزية
```

### التحقق

```bash
tesseract --version
tesseract --list-langs
```

---

## إعدادات المشروع

### 1. تحديث appsettings.json

افتح `src/appsettings.json` وعدّل:

```json
{
  "Ocr": {
    "TesseractPath": "C:\\Program Files\\Tesseract-OCR",
    "Language": "ara+eng",
    "Enabled": true
  }
}
```

**ملاحظات:**
- `TesseractPath`: المسار الكامل لمجلد Tesseract
- `Language`: اللغة المستخدمة (`ara` للعربية فقط، `ara+eng` للعربية والإنجليزية)
- `Enabled`: تفعيل/تعطيل OCR

### 2. Linux Configuration

```json
{
  "Ocr": {
    "TesseractPath": "/usr/bin",
    "Language": "ara+eng",
    "Enabled": true
  }
}
```

---

## اختبار OCR

### اختبار من سطر الأوامر

```powershell
cd "C:\Program Files\Tesseract-OCR"
.\tesseract.exe "C:\path\to\image.png" "C:\path\to\output" -l ara+eng
```

### اختبار من الكود

```csharp
var ocrService = serviceProvider.GetRequiredService<IOcrService>();
var text = await ocrService.ExtractTextFromImageAsync("path/to/image.png");
Console.WriteLine(text);
```

---

## استكشاف الأخطاء

### مشكلة: "Tesseract executable not found"

**الحل:**
1. تحقق من المسار في `appsettings.json`
2. تأكد من وجود `tesseract.exe` في المجلد
3. جرب المسار الكامل: `C:\\Program Files\\Tesseract-OCR\\tesseract.exe`

### مشكلة: "Language 'ara' not found"

**الحل:**
1. تحقق من وجود `ara.traineddata` في مجلد `tessdata`
2. قم بتحميل ملف اللغة من: https://github.com/tesseract-ocr/tessdata
3. انسخ الملف إلى: `C:\Program Files\Tesseract-OCR\tessdata\`

### مشكلة: "No text extracted"

**الحل:**
1. تأكد من جودة الصورة (DPI عالي أفضل)
2. جرب مع صورة واضحة أولاً
3. تحقق من Logs في Console
4. جرب لغة مختلفة: `eng` فقط

### مشكلة: دقة OCR منخفضة

**الحل:**
1. استخدم صور بجودة عالية (300 DPI أو أعلى)
2. تأكد من أن الصورة واضحة وغير مشوشة
3. جرب معالجة الصورة قبل OCR (تحسين التباين، إزالة الضوضاء)
4. استخدم `ara+eng` للدعم المزدوج

---

## دعم أنواع الملفات

### مدعوم حالياً:
- ✅ **الصور:** JPG, JPEG, PNG, TIFF, BMP
- ⏳ **PDF:** يتطلب تحويل PDF إلى صور أولاً

### قيد التطوير:
- PDF OCR (يتطلب iText7 أو Ghostscript)
- معالجة الصور قبل OCR (تحسين الجودة)

---

## الأداء

### نصائح لتحسين الأداء:

1. **معالجة الدُفعات:** معالجة عدة مستندات في وقت واحد
2. **Queue System:** استخدام Hangfire للمعالجة في الخلفية
3. **Caching:** تخزين نتائج OCR لتجنب إعادة المعالجة
4. **Parallel Processing:** معالجة متوازية للمستندات الكبيرة

---

## المراجع

- [Tesseract OCR Documentation](https://tesseract-ocr.github.io/)
- [Tesseract GitHub](https://github.com/tesseract-ocr/tesseract)
- [Arabic Language Data](https://github.com/tesseract-ocr/tessdata/blob/main/ara.traineddata)
- [UB-Mannheim Windows Installer](https://github.com/UB-Mannheim/tesseract/wiki)

---

## ملاحظات مهمة

1. **الصلاحيات:** قد تحتاج صلاحيات مسؤول لتثبيت Tesseract
2. **المسار:** استخدم مسارات مطلقة في `appsettings.json`
3. **اللغة:** استخدم `ara+eng` للدعم المزدوج (العربية والإنجليزية)
4. **الجودة:** جودة الصورة تؤثر بشكل كبير على دقة OCR

---

**آخر تحديث:** 2025

