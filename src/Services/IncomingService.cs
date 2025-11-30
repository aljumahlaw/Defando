using LegalDocSystem.Data;
using LegalDocSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LegalDocSystem.Services;

/// <summary>
/// Service implementation for managing incoming correspondence records.
/// </summary>
public class IncomingService : IIncomingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IncomingService> _logger;

    public IncomingService(ApplicationDbContext context, ILogger<IncomingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all incoming records with related entities.
    /// </summary>
    public async Task<List<Incoming>> GetAllIncomingAsync()
    {
        return await _context.IncomingRecords
            .Include(i => i.Document)
            .Include(i => i.CreatedByUser)
            .OrderByDescending(i => i.ReceivedDate)
            .ThenByDescending(i => i.IncomingId)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves an incoming record by its ID.
    /// </summary>
    public async Task<Incoming?> GetIncomingByIdAsync(int id)
    {
        return await _context.IncomingRecords
            .Include(i => i.Document)
            .Include(i => i.CreatedByUser)
            .FirstOrDefaultAsync(i => i.IncomingId == id);
    }

    /// <summary>
    /// Creates a new incoming record.
    /// </summary>
    public async Task<Incoming> CreateIncomingAsync(Incoming incoming)
    {
        // Generate incoming number if not provided
        if (string.IsNullOrEmpty(incoming.IncomingNumber))
        {
            incoming.IncomingNumber = await GenerateIncomingNumberAsync();
        }

        _context.IncomingRecords.Add(incoming);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created incoming record {incoming.IncomingId} with number {incoming.IncomingNumber}");
        return incoming;
    }

    /// <summary>
    /// Updates an existing incoming record.
    /// </summary>
    public async Task UpdateIncomingAsync(Incoming incoming)
    {
        _context.IncomingRecords.Update(incoming);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Updated incoming record {incoming.IncomingId}");
    }

    /// <summary>
    /// Deletes an incoming record by its ID.
    /// </summary>
    public async Task DeleteIncomingAsync(int id)
    {
        var incoming = await _context.IncomingRecords.FindAsync(id);
        if (incoming != null)
        {
            _context.IncomingRecords.Remove(incoming);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted incoming record {id}");
        }
    }

    /// <summary>
    /// Searches incoming records by various criteria.
    /// </summary>
    public async Task<List<Incoming>> SearchIncomingAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? senderName = null,
        string? priority = null)
    {
        var query = _context.IncomingRecords
            .Include(i => i.Document)
            .Include(i => i.CreatedByUser)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(i => i.ReceivedDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(i => i.ReceivedDate <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(senderName))
        {
            query = query.Where(i => i.SenderName.Contains(senderName));
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(i => i.Priority == priority);
        }

        return await query
            .OrderByDescending(i => i.ReceivedDate)
            .ThenByDescending(i => i.IncomingId)
            .ToListAsync();
    }

    /// <summary>
    /// Generates the next incoming number for the current year.
    /// Format: ORG-IN-YYYY-XXXX
    /// </summary>
    public async Task<string> GenerateIncomingNumberAsync()
    {
        var year = DateTime.Now.Year;
        var prefix = $"ORG-IN-{year}-";

        // Get the last number for this year
        var lastNumber = await _context.IncomingRecords
            .Where(i => i.IncomingNumber.StartsWith(prefix))
            .OrderByDescending(i => i.IncomingId)
            .Select(i => i.IncomingNumber)
            .FirstOrDefaultAsync();

        int nextNum = 1;
        if (!string.IsNullOrEmpty(lastNumber))
        {
            // Extract number from "ORG-IN-2025-0123"
            var numPart = lastNumber.Split('-').Last();
            if (int.TryParse(numPart, out var lastNum))
            {
                nextNum = lastNum + 1;
            }
        }

        return $"{prefix}{nextNum:D4}"; // D4 = 4 digits (0001, 0002, ...)
    }
}

