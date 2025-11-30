using LegalDocSystem.Data;
using LegalDocSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LegalDocSystem.Services;

/// <summary>
/// Service implementation for task management operations.
/// </summary>
public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _context;

    public TaskService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all tasks from the database.
    /// </summary>
    public async Task<List<TaskItem>> GetAllTasksAsync()
    {
        return await _context.Tasks
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .Include(t => t.Document)
            .Include(t => t.Comments)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves a task by its unique identifier.
    /// </summary>
    public async Task<TaskItem?> GetTaskByIdAsync(int id)
    {
        return await _context.Tasks
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .Include(t => t.Document)
            .Include(t => t.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(t => t.TaskId == id);
    }

    /// <summary>
    /// Creates a new task in the database.
    /// </summary>
    public async Task<TaskItem> CreateTaskAsync(TaskItem task)
    {
        // TODO: Send notification to assigned user
        // TODO: Log task creation in AuditLog
        // TODO: Schedule reminder if due_date is set

        task.CreatedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    /// <summary>
    /// Updates the status of an existing task.
    /// </summary>
    public async Task UpdateTaskStatusAsync(int id, string status)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task != null)
        {
            task.Status = status;
            task.UpdatedAt = DateTime.UtcNow;

            if (status == "completed")
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            else if (status != "completed" && task.CompletedAt.HasValue)
            {
                task.CompletedAt = null;
            }

            // TODO: Send notification if status changed
            // TODO: Log status change in AuditLog

            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Retrieves all tasks assigned to a specific user.
    /// </summary>
    public async Task<List<TaskItem>> GetTasksByUserAsync(int userId)
    {
        return await _context.Tasks
            .Where(t => t.AssignedTo == userId)
            .Include(t => t.AssignedByUser)
            .Include(t => t.Document)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }
}

