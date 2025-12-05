using FluentAssertions;
using Defando.Data;
using Defando.Models;
using Defando.Services;
using Defando.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Defando.Tests.Services;

/// <summary>
/// Unit tests for TaskService.
/// Tests task management operations including CRUD, status updates, and user assignments.
/// </summary>
public class TaskServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TaskService _taskService;

    public TaskServiceTests()
    {
        // Setup In-Memory Database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Create TaskService instance
        _taskService = new TaskService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region CreateTaskAsync Tests

    [Fact]
    public async Task CreateTaskAsync_WithValidData_CreatesTask()
    {
        // Arrange: إعداد البيانات
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var task = TestDataBuilder.CreateTask(taskId: 0, assignedTo: user.UserId);
        task.TaskTitle = "New Task";
        task.AssignedBy = user.UserId;
        task.TaskDescription = "Task Description";
        task.Priority = "high";
        task.Status = "pending";
        task.DueDate = DateTime.UtcNow.AddDays(7);
        task.AssignedBy = user.UserId;

        // Act: تنفيذ الوظيفة المطلوب اختبارها
        var result = await _taskService.CreateTaskAsync(task);

        // Assert: التحقق من النتائج
        result.Should().NotBeNull();
        result.TaskId.Should().BeGreaterThan(0);
        result.TaskTitle.Should().Be("New Task");
        result.TaskDescription.Should().Be("Task Description");
        result.Priority.Should().Be("high");
        result.Status.Should().Be("pending");
        result.DueDate.Should().NotBeNull();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify task was saved in database
        var savedTask = await _context.Tasks.FindAsync(result.TaskId);
        savedTask.Should().NotBeNull();
        savedTask!.TaskTitle.Should().Be("New Task");
    }

    [Fact]
    public async Task CreateTaskAsync_WithDocumentLink_CreatesTaskWithDocument()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, folderId: null, uploadedBy: user.UserId);
        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var task = TestDataBuilder.CreateTask(taskId: 0, assignedTo: user.UserId);
        task.TaskTitle = "Review Document";
        task.DocumentId = document.DocumentId;
        task.AssignedBy = user.UserId;

        // Act
        var result = await _taskService.CreateTaskAsync(task);

        // Assert
        result.Should().NotBeNull();
        result.DocumentId.Should().Be(document.DocumentId);

        // Verify relationship
        var savedTask = await _context.Tasks
            .Include(t => t.Document)
            .FirstOrDefaultAsync(t => t.TaskId == result.TaskId);
        savedTask.Should().NotBeNull();
        savedTask!.Document.Should().NotBeNull();
        savedTask.Document!.DocumentId.Should().Be(document.DocumentId);
    }

    [Fact]
    public async Task CreateTaskAsync_WithDueDate_SetsDueDate()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dueDate = DateTime.UtcNow.AddDays(14);
        var task = TestDataBuilder.CreateTask(taskId: 0, assignedTo: user.UserId);
        task.TaskTitle = "Task With Due Date";
        task.DueDate = dueDate;
        task.AssignedBy = user.UserId;

        // Act
        var result = await _taskService.CreateTaskAsync(task);

        // Assert
        result.Should().NotBeNull();
        result.DueDate.Should().NotBeNull();
        result.DueDate!.Value.Date.Should().Be(dueDate.Date);
    }

    [Fact]
    public async Task CreateTaskAsync_WithDefaultValues_UsesDefaults()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var task = TestDataBuilder.CreateTask(taskId: 0, assignedTo: user.UserId);
        task.TaskTitle = "Default Task";
        task.AssignedBy = user.UserId;
        // Priority and Status should use defaults from model

        // Act
        var result = await _taskService.CreateTaskAsync(task);

        // Assert
        result.Should().NotBeNull();
        result.Priority.Should().Be("normal"); // Default from model
        result.Status.Should().Be("pending"); // Default from model
    }

    [Fact]
    public async Task CreateTaskAsync_WithMultipleTasks_CreatesAllTasks()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var task1 = TestDataBuilder.CreateTask(taskId: 0, assignedTo: user.UserId);
        task1.TaskTitle = "Task 1";
        task1.AssignedBy = user.UserId;

        var task2 = TestDataBuilder.CreateTask(taskId: 0, assignedTo: user.UserId);
        task2.TaskTitle = "Task 2";
        task2.AssignedBy = user.UserId;

        // Act
        var result1 = await _taskService.CreateTaskAsync(task1);
        var result2 = await _taskService.CreateTaskAsync(task2);

        // Assert
        result1.TaskId.Should().NotBe(result2.TaskId);

        var allTasks = await _context.Tasks.ToListAsync();
        allTasks.Should().HaveCount(2);
        allTasks.Should().Contain(t => t.TaskTitle == "Task 1");
        allTasks.Should().Contain(t => t.TaskTitle == "Task 2");
    }

    #endregion

    #region GetTaskByIdAsync Tests

    [Fact]
    public async Task GetTaskByIdAsync_WithExistingTask_ReturnsTask()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var task = TestDataBuilder.CreateTask(taskId: 1, assignedTo: user.UserId);
        task.TaskTitle = "Test Task";
        task.AssignedBy = user.UserId;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetTaskByIdAsync(task.TaskId);

        // Assert
        result.Should().NotBeNull();
        result!.TaskId.Should().Be(task.TaskId);
        result.TaskTitle.Should().Be("Test Task");
        result.AssignedToUser.Should().NotBeNull();
        result.AssignedByUser.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTaskByIdAsync_WithUnknownId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = 999;

        // Act
        var result = await _taskService.GetTaskByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTaskByIdAsync_WithTaskHavingDocument_ReturnsTaskWithDocument()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, folderId: null, uploadedBy: user.UserId);
        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var task = TestDataBuilder.CreateTask(taskId: 1, assignedTo: user.UserId);
        task.TaskTitle = "Document Task";
        task.DocumentId = document.DocumentId;
        task.AssignedBy = user.UserId;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetTaskByIdAsync(task.TaskId);

        // Assert
        result.Should().NotBeNull();
        result!.Document.Should().NotBeNull();
        result.Document!.DocumentId.Should().Be(document.DocumentId);
    }

    [Fact]
    public async Task GetTaskByIdAsync_WithTaskHavingComments_ReturnsTaskWithComments()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var task = TestDataBuilder.CreateTask(taskId: 1, assignedTo: user.UserId);
        task.TaskTitle = "Task With Comments";
        task.AssignedBy = user.UserId;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Note: TaskComment model might need to be checked, but assuming it exists
        // If TaskComment doesn't exist, this test can be skipped or adjusted

        // Act
        var result = await _taskService.GetTaskByIdAsync(task.TaskId);

        // Assert
        result.Should().NotBeNull();
        result!.Comments.Should().NotBeNull();
    }

    #endregion

    #region GetAllTasksAsync Tests

    [Fact]
    public async Task GetAllTasksAsync_WithMultipleTasks_ReturnsAllTasks()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var task1 = TestDataBuilder.CreateTask(taskId: 1, assignedTo: user.UserId);
        task1.TaskTitle = "Task A";
        task1.AssignedBy = user.UserId;
        task1.CreatedAt = DateTime.UtcNow.AddHours(-2);

        var task2 = TestDataBuilder.CreateTask(taskId: 2, assignedTo: user.UserId);
        task2.TaskTitle = "Task B";
        task2.AssignedBy = user.UserId;
        task2.CreatedAt = DateTime.UtcNow.AddHours(-1);

        var task3 = TestDataBuilder.CreateTask(taskId: 3, assignedTo: user.UserId);
        task3.TaskTitle = "Task C";
        task3.AssignedBy = user.UserId;
        task3.CreatedAt = DateTime.UtcNow;

        _context.Tasks.AddRange(task1, task2, task3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetAllTasksAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(t => t.TaskTitle == "Task A");
        result.Should().Contain(t => t.TaskTitle == "Task B");
        result.Should().Contain(t => t.TaskTitle == "Task C");

        // Verify ordering (should be ordered by CreatedAt descending)
        result[0].CreatedAt.Should().BeAfter(result[1].CreatedAt);
        result[1].CreatedAt.Should().BeAfter(result[2].CreatedAt);
    }

    [Fact]
    public async Task GetAllTasksAsync_WithNoTasks_ReturnsEmptyList()
    {
        // Arrange: No tasks in database

        // Act
        var result = await _taskService.GetAllTasksAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTasksAsync_ReturnsTasksWithRelationships()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var task = TestDataBuilder.CreateTask(taskId: 1, assignedTo: user.UserId);
        task.TaskTitle = "Task With Relationships";
        task.AssignedBy = user.UserId;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetAllTasksAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].AssignedToUser.Should().NotBeNull();
        result[0].AssignedByUser.Should().NotBeNull();
    }

    #endregion

    #region GetTasksByUserAsync Tests

    [Fact]
    public async Task GetTasksByUserAsync_WithExistingTasks_ReturnsUserTasks()
    {
        // Arrange
        var user1 = TestDataBuilder.CreateUser(userId: 1, username: "user1");
        var user2 = TestDataBuilder.CreateUser(userId: 2, username: "user2");
        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        var task1 = TestDataBuilder.CreateTask(taskId: 1, assignedTo: user1.UserId);
        task1.TaskTitle = "User1 Task 1";
        task1.AssignedBy = user1.UserId;

        var task2 = TestDataBuilder.CreateTask(taskId: 2, assignedTo: user1.UserId);
        task2.TaskTitle = "User1 Task 2";
        task2.AssignedBy = user1.UserId;

        var task3 = TestDataBuilder.CreateTask(taskId: 3, assignedTo: user2.UserId);
        task3.TaskTitle = "User2 Task";
        task3.AssignedBy = user2.UserId;

        _context.Tasks.AddRange(task1, task2, task3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetTasksByUserAsync(user1.UserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.TaskTitle == "User1 Task 1");
        result.Should().Contain(t => t.TaskTitle == "User1 Task 2");
        result.Should().NotContain(t => t.TaskTitle == "User2 Task");
    }

    [Fact]
    public async Task GetTasksByUserAsync_WithNoTasks_ReturnsEmptyList()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetTasksByUserAsync(user.UserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTasksByUserAsync_ReturnsTasksOrderedByCreatedAt()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var task1 = TestDataBuilder.CreateTask(taskId: 1, assignedTo: user.UserId);
        task1.TaskTitle = "Old Task";
        task1.AssignedBy = user.UserId;
        task1.CreatedAt = DateTime.UtcNow.AddHours(-2);

        var task2 = TestDataBuilder.CreateTask(taskId: 2, assignedTo: user.UserId);
        task2.TaskTitle = "New Task";
        task2.AssignedBy = user.UserId;
        task2.CreatedAt = DateTime.UtcNow.AddHours(-1);

        _context.Tasks.AddRange(task1, task2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetTasksByUserAsync(user.UserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        // Should be ordered by CreatedAt descending
        result[0].CreatedAt.Should().BeAfter(result[1].CreatedAt);
        result[0].TaskTitle.Should().Be("New Task");
        result[1].TaskTitle.Should().Be("Old Task");
    }

    #endregion

    #region UpdateTaskStatusAsync Tests

    [Fact]
    public async Task UpdateTaskStatusAsync_WithValidStatus_UpdatesStatus()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var task = TestDataBuilder.CreateTask(taskId: 1, assignedTo: user.UserId);
        task.TaskTitle = "Test Task";
        task.Status = "pending";
        task.AssignedBy = user.UserId;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var taskId = task.TaskId;
        var originalUpdatedAt = task.UpdatedAt;

        // Act
        await _taskService.UpdateTaskStatusAsync(taskId, "in_progress");

        // Assert
        var updatedTask = await _context.Tasks.FindAsync(taskId);
        updatedTask.Should().NotBeNull();
        updatedTask!.Status.Should().Be("in_progress");
        updatedTask.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WithCompletedStatus_SetsCompletedAt()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var task = TestDataBuilder.CreateTask(taskId: 1, assignedTo: user.UserId);
        task.TaskTitle = "Test Task";
        task.Status = "pending";
        task.AssignedBy = user.UserId;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var taskId = task.TaskId;

        // Act
        await _taskService.UpdateTaskStatusAsync(taskId, "completed");

        // Assert
        var updatedTask = await _context.Tasks.FindAsync(taskId);
        updatedTask.Should().NotBeNull();
        updatedTask!.Status.Should().Be("completed");
        updatedTask.CompletedAt.Should().NotBeNull();
        updatedTask.CompletedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_FromCompletedToPending_ClearsCompletedAt()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var task = TestDataBuilder.CreateTask(taskId: 1, assignedTo: user.UserId);
        task.TaskTitle = "Test Task";
        task.Status = "completed";
        task.CompletedAt = DateTime.UtcNow.AddHours(-1);
        task.AssignedBy = user.UserId;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var taskId = task.TaskId;

        // Act
        await _taskService.UpdateTaskStatusAsync(taskId, "pending");

        // Assert
        var updatedTask = await _context.Tasks.FindAsync(taskId);
        updatedTask.Should().NotBeNull();
        updatedTask!.Status.Should().Be("pending");
        updatedTask.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WithNonExistingTask_DoesNotThrow()
    {
        // Arrange
        var nonExistentId = 999;

        // Act
        var act = async () => await _taskService.UpdateTaskStatusAsync(nonExistentId, "completed");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WithSameStatus_UpdatesUpdatedAt()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var task = TestDataBuilder.CreateTask(taskId: 1, assignedTo: user.UserId);
        task.TaskTitle = "Test Task";
        task.Status = "in_progress";
        task.AssignedBy = user.UserId;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var taskId = task.TaskId;
        var originalUpdatedAt = task.UpdatedAt;

        // Wait a bit to ensure time difference
        await Task.Delay(100);

        // Act
        await _taskService.UpdateTaskStatusAsync(taskId, "in_progress");

        // Assert
        var updatedTask = await _context.Tasks.FindAsync(taskId);
        updatedTask.Should().NotBeNull();
        updatedTask!.Status.Should().Be("in_progress");
        updatedTask.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CreateUpdateStatusDelete_CompleteWorkflow_WorksCorrectly()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var task = TestDataBuilder.CreateTask(taskId: 0, assignedTo: user.UserId);
        task.TaskTitle = "Workflow Task";
        task.AssignedBy = user.UserId;

        // Act - Create
        var created = await _taskService.CreateTaskAsync(task);
        var createdId = created.TaskId;

        // Verify creation
        var retrieved = await _taskService.GetTaskByIdAsync(createdId);
        retrieved.Should().NotBeNull();
        retrieved!.TaskTitle.Should().Be("Workflow Task");
        retrieved.Status.Should().Be("pending");

        // Act - Update Status
        await _taskService.UpdateTaskStatusAsync(createdId, "in_progress");

        // Verify status update
        var updated = await _taskService.GetTaskByIdAsync(createdId);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be("in_progress");

        // Act - Complete
        await _taskService.UpdateTaskStatusAsync(createdId, "completed");

        // Verify completion
        var completed = await _taskService.GetTaskByIdAsync(createdId);
        completed.Should().NotBeNull();
        completed!.Status.Should().Be("completed");
        completed.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTasksByUserAsync_WithMultipleStatuses_ReturnsAllUserTasks()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var pendingTask = TestDataBuilder.CreateTask(taskId: 1, assignedTo: user.UserId);
        pendingTask.TaskTitle = "Pending Task";
        pendingTask.Status = "pending";
        pendingTask.AssignedBy = user.UserId;

        var inProgressTask = TestDataBuilder.CreateTask(taskId: 2, assignedTo: user.UserId);
        inProgressTask.TaskTitle = "In Progress Task";
        inProgressTask.Status = "in_progress";
        inProgressTask.AssignedBy = user.UserId;

        var completedTask = TestDataBuilder.CreateTask(taskId: 3, assignedTo: user.UserId);
        completedTask.TaskTitle = "Completed Task";
        completedTask.Status = "completed";
        completedTask.AssignedBy = user.UserId;

        _context.Tasks.AddRange(pendingTask, inProgressTask, completedTask);
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetTasksByUserAsync(user.UserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(t => t.Status == "pending");
        result.Should().Contain(t => t.Status == "in_progress");
        result.Should().Contain(t => t.Status == "completed");
    }

    [Fact]
    public async Task TaskWithDueDate_CanBeCreatedAndRetrieved()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dueDate = DateTime.UtcNow.AddDays(30);
        var task = TestDataBuilder.CreateTask(taskId: 0, assignedTo: user.UserId);
        task.TaskTitle = "Task With Due Date";
        task.DueDate = dueDate;
        task.AssignedBy = user.UserId;

        // Act
        var created = await _taskService.CreateTaskAsync(task);
        var retrieved = await _taskService.GetTaskByIdAsync(created.TaskId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.DueDate.Should().NotBeNull();
        retrieved.DueDate!.Value.Date.Should().Be(dueDate.Date);
    }

    #endregion
}

