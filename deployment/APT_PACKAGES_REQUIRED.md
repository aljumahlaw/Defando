# قائمة apt packages المطلوبة للتشغيل في Linux

## الحزم الأساسية المطلوبة

### 1. Tesseract OCR
```bash
sudo apt-get update
sudo apt-get install -y tesseract-ocr
```

### 2. Tesseract OCR - اللغة العربية
```bash
sudo apt-get install -y tesseract-ocr-ara
```

### 3. Tesseract OCR - اللغة الإنجليزية
```bash
sudo apt-get install -y tesseract-ocr-eng
```

## تثبيت جميع الحزم دفعة واحدة

```bash
sudo apt-get update
sudo apt-get install -y \
    tesseract-ocr \
    tesseract-ocr-ara \
    tesseract-ocr-eng
```

## التحقق من التثبيت

```bash
# التحقق من إصدار Tesseract
tesseract --version

# عرض اللغات المتاحة
tesseract --list-langs

# يجب أن ترى: ara و eng في القائمة
```

## ملاحظات

1. **مسار Tesseract في Linux**: `/usr/bin/tesseract`
2. **مسار ملفات اللغة**: `/usr/share/tesseract-ocr/5/tessdata/`
3. **التحقق من وجود اللغة العربية**: 
   ```bash
   ls /usr/share/tesseract-ocr/5/tessdata/ara.traineddata
   ```

## للـ CI/CD (GitHub Actions)

الحزم المطلوبة في `.github/workflows/build.yml`:
```yaml
- name: Install Tesseract OCR
  run: |
    sudo apt-get update
    sudo apt-get install -y tesseract-ocr tesseract-ocr-ara tesseract-ocr-eng
```

## حزم إضافية (اختيارية)

### Redis (إذا كنت تستخدم Redis للجلسات)
```bash
sudo apt-get install -y redis-server
```

### PostgreSQL Client (للإدارة)
```bash
sudo apt-get install -y postgresql-client
```

