using LegalDocSystem.Data;
using LegalDocSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LegalDocSystem.Services;

/// <summary>
/// Service implementation for managing outgoing correspondence records.
/// </summary>
public class OutgoingService : IOutgoingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OutgoingService> _logger;

    public OutgoingService(ApplicationDbContext context, ILogger<OutgoingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all outgoing records with related entities.
    /// </summary>
    public async Task<List<Outgoing>> GetAllOutgoingAsync()
    {
        return await _context.OutgoingRecords
            .Include(o => o.Document)
            .Include(o => o.CreatedByUser)
            .OrderByDescending(o => o.SendDate)
            .ThenByDescending(o => o.OutgoingId)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves an outgoing record by its ID.
    /// </summary>
    public async Task<Outgoing?> GetOutgoingByIdAsync(int id)
    {
        return await _context.OutgoingRecords
            .Include(o => o.Document)
            .Include(o => o.CreatedByUser)
            .FirstOrDefaultAsync(o => o.OutgoingId == id);
    }

    /// <summary>
    /// Creates a new outgoing record.
    /// </summary>
    public async Task<Outgoing> CreateOutgoingAsync(Outgoing outgoing)
    {
        // Generate outgoing number if not provided
        if (string.IsNullOrEmpty(outgoing.OutgoingNumber))
        {
            outgoing.OutgoingNumber = await GenerateOutgoingNumberAsync();
        }

        _context.OutgoingRecords.Add(outgoing);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created outgoing record {outgoing.OutgoingId} with number {outgoing.OutgoingNumber}");
        return outgoing;
    }

    /// <summary>
    /// Updates an existing outgoing record.
    /// </summary>
    public async Task UpdateOutgoingAsync(Outgoing outgoing)
    {
        _context.OutgoingRecords.Update(outgoing);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Updated outgoing record {outgoing.OutgoingId}");
    }

    /// <summary>
    /// Deletes an outgoing record by its ID.
    /// </summary>
    public async Task DeleteOutgoingAsync(int id)
    {
        var outgoing = await _context.OutgoingRecords.FindAsync(id);
        if (outgoing != null)
        {
            _context.OutgoingRecords.Remove(outgoing);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted outgoing record {id}");
        }
    }

    /// <summary>
    /// Searches outgoing records by various criteria.
    /// </summary>
    public async Task<List<Outgoing>> SearchOutgoingAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? recipientName = null,
        string? deliveryMethod = null)
    {
        var query = _context.OutgoingRecords
            .Include(o => o.Document)
            .Include(o => o.CreatedByUser)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(o => o.SendDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(o => o.SendDate <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(recipientName))
        {
            query = query.Where(o => o.RecipientName.Contains(recipientName));
        }

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
        {
            query = query.Where(o => o.DeliveryMethod == deliveryMethod);
        }

        return await query
            .OrderByDescending(o => o.SendDate)
            .ThenByDescending(o => o.OutgoingId)
            .ToListAsync();
    }

    /// <summary>
    /// Generates the next outgoing number for the current year.
    /// Format: ORG-OUT-YYYY-XXXX
    /// </summary>
    public async Task<string> GenerateOutgoingNumberAsync()
    {
        var year = DateTime.Now.Year;
        var prefix = $"ORG-OUT-{year}-";

        // Get the last number for this year
        var lastNumber = await _context.OutgoingRecords
            .Where(o => o.OutgoingNumber.StartsWith(prefix))
            .OrderByDescending(o => o.OutgoingId)
            .Select(o => o.OutgoingNumber)
            .FirstOrDefaultAsync();

        int nextNum = 1;
        if (!string.IsNullOrEmpty(lastNumber))
        {
            // Extract number from "ORG-OUT-2025-0123"
            var numPart = lastNumber.Split('-').Last();
            if (int.TryParse(numPart, out var lastNum))
            {
                nextNum = lastNum + 1;
            }
        }

        return $"{prefix}{nextNum:D4}"; // D4 = 4 digits (0001, 0002, ...)
    }
}

