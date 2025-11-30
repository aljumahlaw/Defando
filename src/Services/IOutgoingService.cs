using LegalDocSystem.Models;

namespace LegalDocSystem.Services;

/// <summary>
/// Service interface for managing outgoing correspondence records.
/// </summary>
public interface IOutgoingService
{
    /// <summary>
    /// Retrieves all outgoing records.
    /// </summary>
    Task<List<Outgoing>> GetAllOutgoingAsync();

    /// <summary>
    /// Retrieves an outgoing record by its ID.
    /// </summary>
    Task<Outgoing?> GetOutgoingByIdAsync(int id);

    /// <summary>
    /// Creates a new outgoing record.
    /// </summary>
    Task<Outgoing> CreateOutgoingAsync(Outgoing outgoing);

    /// <summary>
    /// Updates an existing outgoing record.
    /// </summary>
    Task UpdateOutgoingAsync(Outgoing outgoing);

    /// <summary>
    /// Deletes an outgoing record by its ID.
    /// </summary>
    Task DeleteOutgoingAsync(int id);

    /// <summary>
    /// Searches outgoing records by various criteria.
    /// </summary>
    Task<List<Outgoing>> SearchOutgoingAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? recipientName = null,
        string? deliveryMethod = null);

    /// <summary>
    /// Generates the next outgoing number for the current year.
    /// </summary>
    Task<string> GenerateOutgoingNumberAsync();
}

