using LegalDocSystem.Models;
using LegalDocSystem.ViewModels;

namespace LegalDocSystem.Services;

/// <summary>
/// Service interface for document management operations.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Retrieves all documents from the database.
    /// </summary>
    Task<List<Document>> GetAllDocumentsAsync();

    /// <summary>
    /// Retrieves a document by its unique identifier.
    /// </summary>
    Task<Document?> GetDocumentByIdAsync(int id);

    /// <summary>
    /// Creates a new document in the database.
    /// </summary>
    Task<Document> CreateDocumentAsync(Document document);

    /// <summary>
    /// Updates an existing document in the database.
    /// </summary>
    Task UpdateDocumentAsync(Document document);

    /// <summary>
    /// Deletes a document from the database by its ID.
    /// </summary>
    Task DeleteDocumentAsync(int id);

    /// <summary>
    /// Searches documents using full-text search.
    /// </summary>
    Task<List<Document>> SearchDocumentsAsync(string query);

    /// <summary>
    /// Advanced search with filters, pagination, and ranking.
    /// </summary>
    Task<PaginatedSearchResult> AdvancedSearchAsync(
        string query,
        int? folderId = null,
        string? documentType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 10,
        string sortBy = "relevance");

    /// <summary>
    /// Queues a document for OCR processing.
    /// </summary>
    Task QueueForOcrAsync(int documentId);

    /// <summary>
    /// Uploads a document file and creates document record.
    /// </summary>
    Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, int? folderId, int uploadedBy);

    /// <summary>
    /// Locks a document for editing (Check-out).
    /// </summary>
    Task CheckOutDocumentAsync(int documentId, int userId);

    /// <summary>
    /// Unlocks a document after editing (Check-in).
    /// </summary>
    Task CheckInDocumentAsync(int documentId, int userId, string? changeDescription = null);
}

