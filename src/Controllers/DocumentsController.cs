using Defando.Models;
using Defando.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Defando.Controllers;

/// <summary>
/// API Controller for document management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IAuthService _authService;

    public DocumentsController(IDocumentService documentService, IAuthService authService)
    {
        _documentService = documentService;
        _authService = authService;
    }

    /// <summary>
    /// GET: api/documents
    /// Retrieves all documents from the database.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Document>>> GetAll()
    {
        var documents = await _documentService.GetAllDocumentsAsync();
        return Ok(documents);
    }

    /// <summary>
    /// GET: api/documents/5
    /// Retrieves a specific document by its ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Document>> GetById(int id)
    {
        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document == null)
            return NotFound();

        return Ok(document);
    }

    /// <summary>
    /// POST: api/documents
    /// Creates a new document in the database.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Document>> Create(Document document)
    {
        try
        {
            if (!await _authService.IsAuthenticatedAsync())
                return Unauthorized();

            var created = await _documentService.CreateDocumentAsync(document);
            return CreatedAtAction(nameof(GetById), new { id = created.DocumentId }, created);
        }
        catch (Exception ex)
        {
            // Log detailed error for administrators (not exposed to client)
            // In production, use a proper logging framework (e.g., Serilog, NLog)
            // _logger.LogError(ex, "Error creating document. User: {UserId}, Document: {DocumentName}", 
            //     await _authService.GetCurrentUserId(), document?.DocumentName);
            
            // Return generic error message to client
            return StatusCode(500, "An error occurred while creating the document. Please try again later.");
        }
    }

    /// <summary>
    /// PUT: api/documents/5
    /// Updates an existing document in the database.
    /// </summary>
    [HttpPut("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, Document document)
    {
        try
        {
            if (!await _authService.IsAuthenticatedAsync())
                return Unauthorized();

            if (id != document.DocumentId)
                return BadRequest();

            await _documentService.UpdateDocumentAsync(document);
            return NoContent();
        }
        catch (Exception ex)
        {
            // Log detailed error for administrators (not exposed to client)
            // Return generic error message to client
            return StatusCode(500, "An error occurred while updating the document. Please try again later.");
        }
    }

    /// <summary>
    /// DELETE: api/documents/5
    /// Deletes a document from the database.
    /// </summary>
    [HttpDelete("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            if (!await _authService.IsAuthenticatedAsync())
                return Unauthorized();

            // Check if user has permission to delete (admin only)
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser?.Role != "admin")
                return Unauthorized("Only administrators can delete documents");

            await _documentService.DeleteDocumentAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            // Log detailed error for administrators (not exposed to client)
            // Return generic error message to client
            return StatusCode(500, "An error occurred while deleting the document. Please try again later.");
        }
    }

    /// <summary>
    /// GET: api/documents/search?query=test
    /// Searches documents using full-text search.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<Document>>> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query parameter is required");

        var results = await _documentService.SearchDocumentsAsync(query);
        return Ok(results);
    }
}

