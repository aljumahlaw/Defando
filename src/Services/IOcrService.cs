namespace Defando.Services;

/// <summary>
/// Service interface for OCR (Optical Character Recognition) operations.
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Extracts text from an image file using Tesseract OCR.
    /// </summary>
    Task<string> ExtractTextFromImageAsync(string imagePath);

    /// <summary>
    /// Extracts text from a PDF file using Tesseract OCR.
    /// </summary>
    Task<string> ExtractTextFromPdfAsync(string pdfPath);

    /// <summary>
    /// Processes OCR queue items from the database.
    /// </summary>
    Task ProcessOcrQueueAsync();

    /// <summary>
    /// Checks if OCR is enabled and configured.
    /// </summary>
    bool IsOcrEnabled();
}

