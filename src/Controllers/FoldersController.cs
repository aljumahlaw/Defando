using Defando.Models;
using Defando.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Defando.Controllers;

/// <summary>
/// API Controller for folder management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FoldersController : ControllerBase
{
    private readonly IFolderService _folderService;
    private readonly IAuthService _authService;

    public FoldersController(IFolderService folderService, IAuthService authService)
    {
        _folderService = folderService;
        _authService = authService;
    }

    /// <summary>
    /// GET: api/folders
    /// Retrieves all folders from the database.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Folder>>> GetAll()
    {
        var folders = await _folderService.GetAllFoldersAsync();
        return Ok(folders);
    }

    /// <summary>
    /// GET: api/folders/5
    /// Retrieves a specific folder by its ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Folder>> GetById(int id)
    {
        var folder = await _folderService.GetFolderByIdAsync(id);
        if (folder == null)
            return NotFound();

        return Ok(folder);
    }

    /// <summary>
    /// POST: api/folders
    /// Creates a new folder in the database.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Folder>> Create(Folder folder)
    {
        try
        {
            if (!await _authService.IsAuthenticatedAsync())
                return Unauthorized();

            var created = await _folderService.CreateFolderAsync(folder);
            return CreatedAtAction(nameof(GetById), new { id = created.FolderId }, created);
        }
        catch (Exception ex)
        {
            // Log detailed error for administrators (not exposed to client)
            // Return generic error message to client
            return StatusCode(500, "An error occurred while creating the folder. Please try again later.");
        }
    }

    /// <summary>
    /// PUT: api/folders/5
    /// Updates an existing folder in the database.
    /// </summary>
    [HttpPut("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, Folder folder)
    {
        try
        {
            if (!await _authService.IsAuthenticatedAsync())
                return Unauthorized();

            if (id != folder.FolderId)
                return BadRequest();

            await _folderService.UpdateFolderAsync(folder);
            return NoContent();
        }
        catch (Exception ex)
        {
            // Log detailed error for administrators (not exposed to client)
            // Return generic error message to client
            return StatusCode(500, "An error occurred while updating the folder. Please try again later.");
        }
    }

    /// <summary>
    /// DELETE: api/folders/5
    /// Deletes a folder from the database.
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
                return Unauthorized("Only administrators can delete folders");

            await _folderService.DeleteFolderAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            // Log detailed error for administrators (not exposed to client)
            // Return generic error message to client
            return StatusCode(500, "An error occurred while deleting the folder. Please try again later.");
        }
    }

    /// <summary>
    /// GET: api/folders/5/subfolders
    /// Retrieves all subfolders for a given parent folder.
    /// </summary>
    [HttpGet("{id}/subfolders")]
    public async Task<ActionResult<List<Folder>>> GetSubFolders(int id)
    {
        var subFolders = await _folderService.GetSubFoldersAsync(id);
        return Ok(subFolders);
    }

    /// <summary>
    /// GET: api/folders/root
    /// Retrieves all root folders (folders without parent).
    /// </summary>
    [HttpGet("root")]
    public async Task<ActionResult<List<Folder>>> GetRootFolders()
    {
        var folders = await _folderService.GetAllFoldersAsync();
        var rootFolders = folders.Where(f => f.ParentId == null).ToList();
        return Ok(rootFolders);
    }
}

