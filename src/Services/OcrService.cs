using LegalDocSystem.Data;
using LegalDocSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace LegalDocSystem.Services;

/// <summary>
/// Service implementation for OCR operations using Tesseract.
/// </summary>
public class OcrService : IOcrService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OcrService> _logger;
    private readonly string _tesseractPath;
    private readonly string _language;

    public OcrService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<OcrService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;

        _tesseractPath = _configuration["Ocr:TesseractPath"] ?? "C:\\Program Files\\Tesseract-OCR";
        _language = _configuration["Ocr:Language"] ?? "ara+eng";
    }

    /// <summary>
    /// Extracts text from an image file using Tesseract OCR.
    /// </summary>
    public async Task<string> ExtractTextFromImageAsync(string imagePath)
    {
        if (!IsOcrEnabled())
        {
            throw new InvalidOperationException("OCR is not enabled or Tesseract is not configured.");
        }

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Image file not found: {imagePath}");
        }

        try
        {
            var tesseractExe = Path.Combine(_tesseractPath, "tesseract.exe");
            if (!File.Exists(tesseractExe))
            {
                throw new FileNotFoundException($"Tesseract executable not found: {tesseractExe}");
            }

            // Create temporary output file
            var tempOutput = Path.GetTempFileName();
            var outputPath = tempOutput.Replace(".tmp", "");

            var processInfo = new ProcessStartInfo
            {
                FileName = tesseractExe,
                Arguments = $"\"{imagePath}\" \"{outputPath}\" -l {_language}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                throw new Exception("Failed to start Tesseract process.");
            }

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"Tesseract OCR failed with exit code {process.ExitCode}: {error}");
            }

            // Read extracted text
            var textFile = outputPath + ".txt";
            if (File.Exists(textFile))
            {
                var text = await File.ReadAllTextAsync(textFile);
                
                // Clean up temporary files
                try
                {
                    File.Delete(textFile);
                    File.Delete(tempOutput);
                }
                catch { }

                return text.Trim();
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error extracting text from image: {imagePath}");
            throw;
        }
    }

    /// <summary>
    /// Extracts text from a PDF file using Tesseract OCR.
    /// </summary>
    public async Task<string> ExtractTextFromPdfAsync(string pdfPath)
    {
        if (!IsOcrEnabled())
        {
            throw new InvalidOperationException("OCR is not enabled or Tesseract is not configured.");
        }

        if (!File.Exists(pdfPath))
        {
            throw new FileNotFoundException($"PDF file not found: {pdfPath}");
        }

        try
        {
            // TODO: Convert PDF pages to images first
            // Option 1: Use iText7 to extract text if PDF has text layer
            // Option 2: Convert PDF pages to images using Ghostscript or ImageMagick
            // Option 3: Use Tesseract with PDF support (requires additional setup)

            // For now, return placeholder
            _logger.LogWarning("PDF OCR not fully implemented. Requires PDF to image conversion.");
            
            // TODO: Implement PDF to image conversion
            // 1. Convert PDF pages to images
            // 2. Process each image with ExtractTextFromImageAsync
            // 3. Combine results

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error extracting text from PDF: {pdfPath}");
            throw;
        }
    }

    /// <summary>
    /// Processes OCR queue items from the database.
    /// </summary>
    public async Task ProcessOcrQueueAsync()
    {
        if (!IsOcrEnabled())
        {
            _logger.LogWarning("OCR is disabled. Skipping queue processing.");
            return;
        }

        try
        {
            var pendingItems = await _context.OcrQueue
                .Where(q => q.Status == "pending")
                .OrderBy(q => q.CreatedAt)
                .Take(10) // Process 10 items at a time
                .ToListAsync();

            foreach (var item in pendingItems)
            {
                try
                {
                    item.Status = "processing";
                    item.ProcessedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Get document
                    var document = await _context.Documents
                        .FirstOrDefaultAsync(d => d.DocumentId == item.DocumentId);

                    if (document == null)
                    {
                        item.Status = "failed";
                        item.ErrorMessage = "Document not found";
                        await _context.SaveChangesAsync();
                        continue;
                    }

                    // Get file path from NAS storage using file_guid
                    // TODO: Implement file retrieval from NAS storage
                    var filePath = GetFilePathFromGuid(document.FileGuid);

                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    {
                        item.Status = "failed";
                        item.ErrorMessage = "File not found in storage";
                        await _context.SaveChangesAsync();
                        continue;
                    }

                    // Extract text based on file type
                    string extractedText = string.Empty;
                    var fileExtension = Path.GetExtension(filePath).ToLower();

                    if (fileExtension == ".pdf")
                    {
                        extractedText = await ExtractTextFromPdfAsync(filePath);
                    }
                    else if (new[] { ".jpg", ".jpeg", ".png", ".tiff", ".bmp" }.Contains(fileExtension))
                    {
                        extractedText = await ExtractTextFromImageAsync(filePath);
                    }
                    else
                    {
                        item.Status = "failed";
                        item.ErrorMessage = $"Unsupported file type: {fileExtension}";
                        await _context.SaveChangesAsync();
                        continue;
                    }

                    // Update document metadata with OCR text
                    if (!string.IsNullOrEmpty(extractedText))
                    {
                        // TODO: Update JSONB metadata field
                        // document.Metadata["ocr_text"] = extractedText;
                        
                        // Update search_vector will be triggered automatically by database trigger
                        // But we can also update it manually if needed

                        item.Status = "completed";
                        item.CompletedAt = DateTime.UtcNow;
                        item.OcrText = extractedText;
                    }
                    else
                    {
                        item.Status = "failed";
                        item.ErrorMessage = "No text extracted from document";
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"OCR processing completed for document {document.DocumentId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing OCR queue item {item.QueueId}");
                    item.Status = "failed";
                    item.ErrorMessage = ex.Message;
                    await _context.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessOcrQueueAsync");
        }
    }

    /// <summary>
    /// Checks if OCR is enabled and configured.
    /// </summary>
    public bool IsOcrEnabled()
    {
        var enabled = _configuration.GetValue<bool>("Ocr:Enabled", false);
        if (!enabled)
            return false;

        var tesseractExe = Path.Combine(_tesseractPath, "tesseract.exe");
        return File.Exists(tesseractExe);
    }

    /// <summary>
    /// Gets file path from GUID (placeholder for NAS storage implementation).
    /// </summary>
    private string GetFilePathFromGuid(string? fileGuid)
    {
        if (string.IsNullOrEmpty(fileGuid))
            return string.Empty;

        // TODO: Implement NAS storage path resolution
        // Example: return Path.Combine(nasStoragePath, fileGuid);
        
        return string.Empty;
    }
}

