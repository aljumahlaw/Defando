using Defando.Data;
using Defando.Models;
using Microsoft.EntityFrameworkCore;

namespace Defando.Services;

/// <summary>
/// Service implementation for folder management operations.
/// </summary>
public class FolderService : IFolderService
{
    private readonly ApplicationDbContext _context;

    public FolderService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all folders from the database.
    /// </summary>
    public async Task<List<Folder>> GetAllFoldersAsync()
    {
        return await _context.Folders
            .Include(f => f.ParentFolder)
            .Include(f => f.CreatedByUser)
            .OrderBy(f => f.FolderPath)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves a folder by its unique identifier.
    /// </summary>
    public async Task<Folder?> GetFolderByIdAsync(int id)
    {
        return await _context.Folders
            .Include(f => f.ParentFolder)
            .Include(f => f.CreatedByUser)
            .Include(f => f.Documents)
            .FirstOrDefaultAsync(f => f.FolderId == id);
    }

    /// <summary>
    /// Creates a new folder in the database.
    /// </summary>
    public async Task<Folder> CreateFolderAsync(Folder folder)
    {
        // Validate folder name uniqueness within the same parent
        var duplicateExists = await _context.Folders
            .AnyAsync(f => f.ParentId == folder.ParentId && 
                          f.FolderName == folder.FolderName);
        
        if (duplicateExists)
        {
            throw new InvalidOperationException(
                $"A folder with the name '{folder.FolderName}' already exists in the same parent folder.");
        }

        // TODO: Build folder_path from parent hierarchy if not provided
        // TODO: Log creation in AuditLog

        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();
        return folder;
    }

    /// <summary>
    /// Updates an existing folder in the database.
    /// </summary>
    public async Task UpdateFolderAsync(Folder folder)
    {
        // TODO: Update folder_path for all subfolders if parent changed
        // TODO: Log update in AuditLog

        _context.Folders.Update(folder);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a folder from the database by its ID.
    /// </summary>
    public async Task DeleteFolderAsync(int id)
    {
        var folder = await _context.Folders
            .Include(f => f.ChildFolders)
            .Include(f => f.Documents)
            .FirstOrDefaultAsync(f => f.FolderId == id);

        if (folder != null)
        {
            // Prevent deletion if folder contains child folders
            if (folder.ChildFolders != null && folder.ChildFolders.Any())
            {
                throw new InvalidOperationException(
                    $"Cannot delete folder '{folder.FolderName}' because it contains {folder.ChildFolders.Count} subfolder(s). " +
                    "Please delete or move all subfolders first.");
            }

            // Prevent deletion if folder contains documents
            if (folder.Documents != null && folder.Documents.Any())
            {
                throw new InvalidOperationException(
                    $"Cannot delete folder '{folder.FolderName}' because it contains {folder.Documents.Count} document(s). " +
                    "Please delete or move all documents first.");
            }

            // TODO: Log deletion in AuditLog

            _context.Folders.Remove(folder);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Retrieves all subfolders for a given parent folder.
    /// </summary>
    public async Task<List<Folder>> GetSubFoldersAsync(int? parentId)
    {
        return await _context.Folders
            .Where(f => f.ParentId == parentId)
            .Include(f => f.CreatedByUser)
            .OrderBy(f => f.FolderName)
            .ToListAsync();
    }
}

