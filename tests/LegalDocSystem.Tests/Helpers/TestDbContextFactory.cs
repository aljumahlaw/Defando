using LegalDocSystem.Data;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace LegalDocSystem.Tests.Helpers;

/// <summary>
/// Factory for creating In-Memory DbContext instances for testing.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a new In-Memory DbContext for testing.
    /// Each call creates a fresh database with a unique name.
    /// </summary>
    /// <returns>A new ApplicationDbContext instance configured for in-memory testing.</returns>
    public static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Creates a new In-Memory DbContext with seeded test data.
    /// </summary>
    /// <returns>A new ApplicationDbContext instance with test data.</returns>
    public static ApplicationDbContext CreateContextWithSeed()
    {
        var context = CreateContext();
        SeedTestData(context);
        return context;
    }

    /// <summary>
    /// Seeds the context with basic test data.
    /// </summary>
    /// <param name="context">The DbContext to seed.</param>
    public static void SeedTestData(ApplicationDbContext context)
    {
        // Add test users
        var testUser = new LegalDocSystem.Models.User
        {
            UserId = 1,
            Username = "testuser",
            PasswordHash = BCrypt.HashPassword("TestPassword123"),
            FullName = "Test User",
            Email = "test@example.com",
            Role = "user",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var adminUser = new LegalDocSystem.Models.User
        {
            UserId = 2,
            Username = "admin",
            PasswordHash = BCrypt.HashPassword("AdminPassword123"),
            FullName = "Admin User",
            Email = "admin@example.com",
            Role = "admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(testUser, adminUser);
        context.SaveChanges();
    }

    /// <summary>
    /// Cleans up the context by disposing it.
    /// </summary>
    /// <param name="context">The DbContext to clean up.</param>
    public static void Cleanup(ApplicationDbContext context)
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }
}

