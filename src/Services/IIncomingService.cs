using LegalDocSystem.Models;

namespace LegalDocSystem.Services;

/// <summary>
/// Service interface for managing incoming correspondence records.
/// </summary>
public interface IIncomingService
{
    /// <summary>
    /// Retrieves all incoming records.
    /// </summary>
    Task<List<Incoming>> GetAllIncomingAsync();

    /// <summary>
    /// Retrieves an incoming record by its ID.
    /// </summary>
    Task<Incoming?> GetIncomingByIdAsync(int id);

    /// <summary>
    /// Creates a new incoming record.
    /// </summary>
    Task<Incoming> CreateIncomingAsync(Incoming incoming);

    /// <summary>
    /// Updates an existing incoming record.
    /// </summary>
    Task UpdateIncomingAsync(Incoming incoming);

    /// <summary>
    /// Deletes an incoming record by its ID.
    /// </summary>
    Task DeleteIncomingAsync(int id);

    /// <summary>
    /// Searches incoming records by various criteria.
    /// </summary>
    Task<List<Incoming>> SearchIncomingAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? senderName = null,
        string? priority = null);

    /// <summary>
    /// Generates the next incoming number for the current year.
    /// </summary>
    Task<string> GenerateIncomingNumberAsync();
}

