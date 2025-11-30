using LegalDocSystem.Models;

namespace LegalDocSystem.Services;

/// <summary>
/// Service interface for folder management operations.
/// </summary>
public interface IFolderService
{
    /// <summary>
    /// Retrieves all folders from the database.
    /// </summary>
    Task<List<Folder>> GetAllFoldersAsync();

    /// <summary>
    /// Retrieves a folder by its unique identifier.
    /// </summary>
    Task<Folder?> GetFolderByIdAsync(int id);

    /// <summary>
    /// Creates a new folder in the database.
    /// </summary>
    Task<Folder> CreateFolderAsync(Folder folder);

    /// <summary>
    /// Updates an existing folder in the database.
    /// </summary>
    Task UpdateFolderAsync(Folder folder);

    /// <summary>
    /// Deletes a folder from the database by its ID.
    /// </summary>
    Task DeleteFolderAsync(int id);

    /// <summary>
    /// Retrieves all subfolders for a given parent folder.
    /// </summary>
    Task<List<Folder>> GetSubFoldersAsync(int? parentId);
}

