using LegalDocSystem.Data;
using LegalDocSystem.Models;
using LegalDocSystem.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.RegularExpressions;
using Npgsql;

namespace LegalDocSystem.Services;

/// <summary>
/// Service implementation for document management operations.
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IOcrService _ocrService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAuditService _auditService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        ApplicationDbContext context, 
        IOcrService ocrService, 
        IFileStorageService fileStorageService,
        IAuditService auditService,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _ocrService = ocrService;
        _fileStorageService = fileStorageService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all documents from the database with pagination.
    /// </summary>
    public async Task<(List<Document> Documents, int TotalCount)> GetAllDocumentsAsync(int page = 1, int pageSize = 20)
    {
        var query = _context.Documents
            .Include(d => d.Folder)
            .Include(d => d.UploadedByUser)
            .OrderByDescending(d => d.UploadedAt);

        var totalCount = await query.CountAsync();
        
        var documents = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (documents, totalCount);
    }

    /// <summary>
    /// Retrieves a document by its unique identifier.
    /// </summary>
    public async Task<Document?> GetDocumentByIdAsync(int id)
    {
        return await _context.Documents
            .Include(d => d.Folder)
            .Include(d => d.UploadedByUser)
            .Include(d => d.LockedByUser)
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.DocumentId == id);
    }

    /// <summary>
    /// Creates a new document in the database.
    /// </summary>
    public async Task<Document> CreateDocumentAsync(Document document)
    {
        // TODO: Add file upload to NAS storage
        // TODO: Generate file_guid if not provided
        // TODO: Add to OCR queue if document is scanned image/PDF
        // TODO: Create initial version in DocumentVersions table

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Log document creation
        try
        {
            await _auditService.LogCreateAsync(
                entityType: "Document",
                entityId: document.DocumentId,
                additionalData: $"Document Name: {document.DocumentName}, Type: {document.DocumentType}, Folder: {document.FolderId}"
            );
        }
        catch (Exception)
        {
            // Don't throw - audit logging should not break document creation
        }

        return document;
    }

    /// <summary>
    /// Updates an existing document in the database.
    /// </summary>
    public async Task UpdateDocumentAsync(Document document)
    {
        // Get old document data for audit log
        var oldDocument = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentId == document.DocumentId);

        // TODO: Handle version control (create new version if file changed)
        // TODO: Update search_vector trigger will fire automatically

        _context.Documents.Update(document);
        await _context.SaveChangesAsync();

        // Log document update
        try
        {
            var changes = new List<string>();
            if (oldDocument != null)
            {
                if (oldDocument.DocumentName != document.DocumentName)
                    changes.Add($"Name: {oldDocument.DocumentName} -> {document.DocumentName}");
                if (oldDocument.DocumentType != document.DocumentType)
                    changes.Add($"Type: {oldDocument.DocumentType} -> {document.DocumentType}");
                if (oldDocument.Status != document.Status)
                    changes.Add($"Status: {oldDocument.Status} -> {document.Status}");
            }

            await _auditService.LogUpdateAsync(
                entityType: "Document",
                entityId: document.DocumentId,
                additionalData: changes.Count > 0 
                    ? $"Updated fields: {string.Join(", ", changes)}" 
                    : "Document updated"
            );
        }
        catch (Exception)
        {
            // Don't throw - audit logging should not break document update
        }
    }

    /// <summary>
    /// Deletes a document from the database by its ID.
    /// </summary>
    public async Task DeleteDocumentAsync(int id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document != null)
        {
            var documentName = document.DocumentName;
            var documentType = document.DocumentType;
            var folderId = document.FolderId;

            // Delete physical file from storage
            if (!string.IsNullOrEmpty(document.FilePath))
            {
                try
                {
                    await _fileStorageService.DeleteFileAsync(document.FilePath);
                }
                catch (Exception ex)
                {
                    // Log error but continue with database deletion
                    _logger.LogWarning(ex, "Error deleting physical file for document {DocumentId} at {FilePath}", id, document.FilePath);
                    try
                    {
                        await _auditService.LogEventAsync(new AuditLogEntry
                        {
                            Event = "Delete",
                            Category = "Document",
                            Action = "delete_document_file_error",
                            EntityType = "Document",
                            EntityId = id,
                            Data = $"Error deleting file: {ex.Message}, FilePath: {document.FilePath}",
                            Created = DateTime.UtcNow
                        });
                    }
                    catch
                    {
                        // Ignore audit logging errors
                    }
                }
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            // Log document deletion
            try
            {
                await _auditService.LogDeleteAsync(
                    entityType: "Document",
                    entityId: id,
                    additionalData: $"Deleted document: {documentName}, Type: {documentType}, Folder: {folderId}"
                );
            }
            catch (Exception)
            {
                // Don't throw - audit logging should not break document deletion
            }
        }
    }

    /// <summary>
    /// Searches documents using full-text search.
    /// </summary>
    public async Task<List<Document>> SearchDocumentsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<Document>();

        // Use advanced search with default parameters
        var result = await AdvancedSearchAsync(query, pageSize: 50);
        return result.Results.Select(r => r.Document).ToList();
    }

    /// <summary>
    /// Advanced search with filters, pagination, and ranking using PostgreSQL Full-Text Search.
    /// </summary>
    public async Task<PaginatedSearchResult> AdvancedSearchAsync(
        string query,
        int? folderId = null,
        string? documentType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 10,
        string sortBy = "relevance")
    {
        var result = new PaginatedSearchResult
        {
            CurrentPage = page,
            PageSize = pageSize
        };

        var queryable = _context.Documents.AsQueryable();

        // Apply filters
        if (folderId.HasValue)
            queryable = queryable.Where(d => d.FolderId == folderId);

        if (!string.IsNullOrEmpty(documentType))
            queryable = queryable.Where(d => d.DocumentType == documentType);

        if (startDate.HasValue)
            queryable = queryable.Where(d => d.UploadedAt >= startDate.Value);

        if (endDate.HasValue)
            queryable = queryable.Where(d => d.UploadedAt <= endDate.Value.AddDays(1).AddTicks(-1));

        // Apply Full-Text Search if query is provided
        if (!string.IsNullOrWhiteSpace(query))
        {
            var searchTerms = PrepareSearchQuery(query);
            if (!string.IsNullOrEmpty(searchTerms))
            {
                // Use proper PostgreSQL Full-Text Search with search_vector
                // PostgreSQL tsvector uses @@ operator: search_vector @@ to_tsquery('arabic', 'terms')
                // Since EF Core doesn't support @@ operator directly, we use raw SQL with parameterized query
                var searchQuery = query.Trim();
                
                // Get document IDs that match full-text search using raw SQL
                // Use parameterized query to prevent SQL injection
                var searchParam = new NpgsqlParameter("@searchTerms", searchTerms);
                var fullTextMatchIds = await _context.Set<Document>()
                    .FromSqlRaw(
                        @"SELECT * FROM documents 
                          WHERE search_vector @@ to_tsquery('arabic', @searchTerms)",
                        searchParam)
                    .Select(d => d.DocumentId)
                    .ToListAsync();
                
                // Combine simple text search with full-text search results
                queryable = queryable.Where(d => 
                    d.DocumentName.Contains(searchQuery) ||
                    (d.DocumentType != null && d.DocumentType.Contains(searchQuery)) ||
                    (d.Tags != null && d.Tags.Any(t => t.Contains(searchQuery))) ||
                    (fullTextMatchIds.Any() && fullTextMatchIds.Contains(d.DocumentId))
                );
            }
            else
            {
                // Fallback to simple contains if search terms preparation failed
                queryable = queryable.Where(d => 
                    d.DocumentName.Contains(query) ||
                    (d.DocumentType != null && d.DocumentType.Contains(query)));
            }
        }

        // Get total count
        result.TotalCount = await queryable.CountAsync();

        // Apply sorting
        // For relevance sorting, we'll sort by rank after calculating it
        IOrderedQueryable<Document> orderedQueryable = sortBy.ToLower() switch
        {
            "date_desc" => queryable.OrderByDescending(d => d.UploadedAt),
            "date_asc" => queryable.OrderBy(d => d.UploadedAt),
            "size_desc" => queryable.OrderByDescending(d => d.FileSize ?? 0),
            "size_asc" => queryable.OrderBy(d => d.FileSize ?? 0),
            _ => queryable.OrderByDescending(d => d.UploadedAt) // Default: newest first
        };

        // Apply pagination
        var documents = await orderedQueryable
            .Include(d => d.Folder)
            .Include(d => d.UploadedByUser)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Create search results with highlighting and ranking
        var results = documents.Select(d => new SearchResult
        {
            Document = d,
            Rank = CalculateRank(d, query),
            HighlightedName = HighlightText(d.DocumentName, query),
            HighlightedType = d.DocumentType != null ? HighlightText(d.DocumentType, query) : null,
            Snippet = ExtractSnippet(d, query)
        }).ToList();

        // Sort by relevance if requested
        if (sortBy.ToLower() == "relevance" && !string.IsNullOrWhiteSpace(query))
        {
            results = results.OrderByDescending(r => r.Rank).ThenByDescending(r => r.Document.UploadedAt).ToList();
        }

        result.Results = results;
        return result;
    }

    /// <summary>
    /// Calculates a simple relevance rank for a document.
    /// </summary>
    private double CalculateRank(Document document, string query)
    {
        if (string.IsNullOrEmpty(query))
            return 0;

        var terms = query.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var rank = 0.0;
        var docName = document.DocumentName.ToLower();
        var docType = document.DocumentType?.ToLower() ?? "";

        foreach (var term in terms)
        {
            if (docName.Contains(term))
                rank += 1.0;
            if (docType.Contains(term))
                rank += 0.5;
            if (document.Tags.Any(t => t.ToLower().Contains(term)))
                rank += 0.3;
        }

        return rank;
    }

    /// <summary>
    /// Prepares search query for PostgreSQL tsquery format.
    /// </summary>
    private string PrepareSearchQuery(string query)
    {
        // Remove special characters and split into terms
        var terms = query.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 1) // Ignore single characters
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToArray();

        if (terms.Length == 0)
            return "";

        // Join terms with & (AND) operator
        // Escape special characters for tsquery
        var escapedTerms = terms.Select(t => 
        {
            // Escape special characters: & | ! ( ) : * '
            var escaped = Regex.Replace(t, @"[&|!():*']", @"\$&");
            return escaped;
        });

        return string.Join(" & ", escapedTerms);
    }

    /// <summary>
    /// Highlights search terms in text.
    /// </summary>
    private string HighlightText(string text, string query)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
            return text;

        var terms = query.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 1)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToArray();

        if (terms.Length == 0)
            return text;

        var highlighted = text;
        foreach (var term in terms)
        {
            var pattern = Regex.Escape(term);
            highlighted = Regex.Replace(
                highlighted,
                pattern,
                match => $"<mark>{match.Value}</mark>",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return highlighted;
    }

    /// <summary>
    /// Extracts a snippet from document metadata or content.
    /// </summary>
    private string? ExtractSnippet(Document document, string query)
    {
        // Try to get snippet from metadata OCR text
        if (document.Metadata != null && 
            document.Metadata.RootElement.TryGetProperty("ocr_text", out var ocrText))
        {
            var text = ocrText.GetString();
            if (!string.IsNullOrEmpty(text))
            {
                // Find first occurrence of search term
                var terms = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in terms)
                {
                    var index = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                    {
                        var start = Math.Max(0, index - 50);
                        var length = Math.Min(200, text.Length - start);
                        var snippet = text.Substring(start, length);
                        return HighlightText(snippet, query);
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Helper class for querying documents with rank.
    /// </summary>
    private class DocumentWithRank
    {
        public int DocumentId { get; set; }
        public double Rank { get; set; }
        // Add other Document properties as needed
    }

    /// <summary>
    /// Queues a document for OCR processing.
    /// </summary>
    public async Task QueueForOcrAsync(int documentId)
    {
        if (!_ocrService.IsOcrEnabled())
        {
            throw new InvalidOperationException("OCR is not enabled.");
        }

        var document = await _context.Documents.FindAsync(documentId);
        if (document == null)
        {
            throw new ArgumentException($"Document with ID {documentId} not found.");
        }

        // Check if document is already in OCR queue
        var existingQueueItem = await _context.OcrQueue
            .FirstOrDefaultAsync(q => q.DocumentId == documentId && q.Status != "completed");

        if (existingQueueItem != null)
        {
            throw new InvalidOperationException($"Document {documentId} is already in OCR queue.");
        }

        // Check if document type supports OCR
        var fileExtension = Path.GetExtension(document.FileName ?? "").ToLower();
        var supportedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".bmp" };
        
        if (!supportedExtensions.Contains(fileExtension))
        {
            throw new InvalidOperationException($"File type {fileExtension} is not supported for OCR.");
        }

        // Create OCR queue item
        var queueItem = new OcrQueue
        {
            DocumentId = documentId,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.OcrQueue.Add(queueItem);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Uploads a document file and creates document record.
    /// </summary>
    public async Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, int? folderId, int uploadedBy)
    {
        try
        {
            // Validate file extension
            if (!_fileStorageService.IsExtensionAllowed(fileName))
            {
                throw new InvalidOperationException($"File extension is not allowed: {Path.GetExtension(fileName)}");
            }

            // Save file to storage
            var filePath = await _fileStorageService.SaveFileAsync(fileStream, fileName);

            // Get file size
            var fileSize = await _fileStorageService.GetFileSizeAsync(filePath);

            // Determine document type from extension
            var documentType = Path.GetExtension(fileName).ToLower().TrimStart('.');

            // Create document record
            var document = new Document
            {
                DocumentName = Path.GetFileNameWithoutExtension(fileName),
                FileName = fileName,
                FileGuid = Guid.NewGuid(),
                FilePath = filePath,
                FileSize = fileSize,
                DocumentType = documentType,
                FolderId = folderId,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow,
                CurrentVersion = 1,
                IsLocked = false
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Create initial version record
            var initialVersion = new DocumentVersion
            {
                DocumentId = document.DocumentId,
                VersionNumber = 1,
                FilePath = filePath,
                FileSize = fileSize,
                ChangeDescription = "Initial upload",
                CreatedBy = uploadedBy,
                CreatedAt = DateTime.UtcNow,
                IsFinal = false
            };
            _context.DocumentVersions.Add(initialVersion);
            await _context.SaveChangesAsync();

            // Queue for OCR if it's a scanned image or PDF
            var ocrSupportedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".bmp" };
            if (ocrSupportedExtensions.Contains(Path.GetExtension(fileName).ToLower()) && _ocrService.IsOcrEnabled())
            {
                try
                {
                    await QueueForOcrAsync(document.DocumentId);
                }
                catch (Exception ex)
                {
                    // Log OCR queue error but don't fail the upload
                    _logger.LogWarning(ex, "Failed to queue document {DocumentId} for OCR", document.DocumentId);
                }
            }

            return document;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Locks a document for editing (Check-out).
    /// </summary>
    public async Task CheckOutDocumentAsync(int documentId, int userId)
    {
        var document = await _context.Documents
            .Include(d => d.LockedByUser)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null)
        {
            throw new ArgumentException($"Document with ID {documentId} not found.");
        }

        if (document.IsLocked)
        {
            if (document.LockedBy == userId)
            {
                throw new InvalidOperationException("You already have this document checked out.");
            }
            else
            {
                var lockedByUser = document.LockedByUser?.FullName ?? "Unknown User";
                throw new InvalidOperationException($"Document is already locked by {lockedByUser}.");
            }
        }

        // Lock the document
        document.IsLocked = true;
        document.LockedBy = userId;
        document.LockedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // TODO: Log check-out in AuditLog
    }

    /// <summary>
    /// Unlocks a document after editing (Check-in).
    /// </summary>
    public async Task CheckInDocumentAsync(int documentId, int userId, string? changeDescription = null)
    {
        var document = await _context.Documents
            .Include(d => d.LockedByUser)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null)
        {
            throw new ArgumentException($"Document with ID {documentId} not found.");
        }

        if (!document.IsLocked)
        {
            throw new InvalidOperationException("Document is not locked.");
        }

        if (document.LockedBy != userId)
        {
            var lockedByUser = document.LockedByUser?.FullName ?? "Unknown User";
            throw new UnauthorizedAccessException($"You cannot check in this document. It is locked by {lockedByUser}.");
        }

        // Create new version if change description is provided
        if (!string.IsNullOrWhiteSpace(changeDescription))
        {
            var newVersion = new DocumentVersion
            {
                DocumentId = documentId,
                VersionNumber = document.CurrentVersion + 1,
                ChangeDescription = changeDescription,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                IsFinal = false
            };

            _context.DocumentVersions.Add(newVersion);
            document.CurrentVersion = newVersion.VersionNumber;
        }

        // Unlock the document
        document.IsLocked = false;
        document.LockedBy = null;
        document.LockedAt = null;

        await _context.SaveChangesAsync();

        // TODO: Log check-in in AuditLog
    }
}

