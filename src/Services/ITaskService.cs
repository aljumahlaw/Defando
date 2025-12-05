using Defando.Models;

namespace Defando.Services;

/// <summary>
/// Service interface for task management operations.
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Retrieves all tasks from the database.
    /// </summary>
    Task<List<TaskItem>> GetAllTasksAsync();

    /// <summary>
    /// Retrieves a task by its unique identifier.
    /// </summary>
    Task<TaskItem?> GetTaskByIdAsync(int id);

    /// <summary>
    /// Creates a new task in the database.
    /// </summary>
    Task<TaskItem> CreateTaskAsync(TaskItem task);

    /// <summary>
    /// Updates the status of an existing task.
    /// </summary>
    Task UpdateTaskStatusAsync(int id, string status);

    /// <summary>
    /// Retrieves all tasks assigned to a specific user.
    /// </summary>
    Task<List<TaskItem>> GetTasksByUserAsync(int userId);
}

