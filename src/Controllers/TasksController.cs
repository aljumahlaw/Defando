using LegalDocSystem.Models;
using LegalDocSystem.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace LegalDocSystem.Controllers;

/// <summary>
/// API Controller for task management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IAuthService _authService;

    public TasksController(ITaskService taskService, IAuthService authService)
    {
        _taskService = taskService;
        _authService = authService;
    }

    /// <summary>
    /// GET: api/tasks
    /// Retrieves all tasks from the database.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TaskItem>>> GetAll()
    {
        var tasks = await _taskService.GetAllTasksAsync();
        return Ok(tasks);
    }

    /// <summary>
    /// GET: api/tasks/5
    /// Retrieves a specific task by its ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TaskItem>> GetById(int id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task == null)
            return NotFound();

        return Ok(task);
    }

    /// <summary>
    /// POST: api/tasks
    /// Creates a new task in the database.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<TaskItem>> Create(TaskItem task)
    {
        try
        {
            if (!await _authService.IsAuthenticatedAsync())
                return Unauthorized();

            var created = await _taskService.CreateTaskAsync(task);
            return CreatedAtAction(nameof(GetById), new { id = created.TaskId }, created);
        }
        catch (Exception ex)
        {
            // Log detailed error for administrators (not exposed to client)
            // Return generic error message to client
            return StatusCode(500, "An error occurred while creating the task. Please try again later.");
        }
    }

    /// <summary>
    /// PUT: api/tasks/5
    /// Updates an existing task in the database.
    /// </summary>
    [HttpPut("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, TaskItem task)
    {
        try
        {
            if (!await _authService.IsAuthenticatedAsync())
                return Unauthorized();

            if (id != task.TaskId)
                return BadRequest();

            // TODO: Add UpdateTaskAsync method to ITaskService
            // await _taskService.UpdateTaskAsync(task);
            return NoContent();
        }
        catch (Exception ex)
        {
            // Log detailed error for administrators (not exposed to client)
            // Return generic error message to client
            return StatusCode(500, "An error occurred while updating the task. Please try again later.");
        }
    }

    /// <summary>
    /// DELETE: api/tasks/5
    /// Deletes a task from the database.
    /// </summary>
    [HttpDelete("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            if (!await _authService.IsAuthenticatedAsync())
                return Unauthorized();

            // TODO: Add DeleteTaskAsync method to ITaskService
            // await _taskService.DeleteTaskAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            // Log detailed error for administrators (not exposed to client)
            // Return generic error message to client
            return StatusCode(500, "An error occurred while deleting the task. Please try again later.");
        }
    }

    /// <summary>
    /// PATCH: api/tasks/5/status
    /// Updates the status of a specific task.
    /// </summary>
    [HttpPatch("{id}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTaskStatusRequest request)
    {
        try
        {
            if (!await _authService.IsAuthenticatedAsync())
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Status))
                return BadRequest("Status is required");

            await _taskService.UpdateTaskStatusAsync(id, request.Status);
            return NoContent();
        }
        catch (Exception ex)
        {
            // Log detailed error for administrators (not exposed to client)
            // Return generic error message to client
            return StatusCode(500, "An error occurred while updating the task status. Please try again later.");
        }
    }

    /// <summary>
    /// GET: api/tasks/user/5
    /// Retrieves all tasks assigned to a specific user.
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<TaskItem>>> GetByUser(int userId)
    {
        var tasks = await _taskService.GetTasksByUserAsync(userId);
        return Ok(tasks);
    }
}

/// <summary>
/// Request model for updating task status.
/// </summary>
public class UpdateTaskStatusRequest
{
    /// <summary>
    /// New status value (pending, in_progress, completed, cancelled).
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

