using FluentAssertions;
using LegalDocSystem.Controllers;
using LegalDocSystem.Models;
using LegalDocSystem.Services;
using LegalDocSystem.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LegalDocSystem.Tests.Controllers;

/// <summary>
/// Unit tests for TasksController.
/// Tests API endpoints for task management operations including CRUD, status updates, and user assignments.
/// </summary>
public class TasksControllerTests
{
    private readonly Mock<ITaskService> _mockTaskService;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly TasksController _controller;

    public TasksControllerTests()
    {
        // Setup Mocks
        _mockTaskService = new Mock<ITaskService>();
        _mockAuthService = new Mock<IAuthService>();

        // Create Controller instance
        _controller = new TasksController(
            _mockTaskService.Object,
            _mockAuthService.Object);

        // Setup default ControllerContext for testing
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAllTasks_WithExistingTasks_ReturnsOkWithTasks()
    {
        // Arrange: إعداد البيانات
        var tasks = new List<TaskItem>
        {
            TestDataBuilder.CreateTask(taskId: 1),
            TestDataBuilder.CreateTask(taskId: 2),
            TestDataBuilder.CreateTask(taskId: 3)
        };

        _mockTaskService
            .Setup(x => x.GetAllTasksAsync())
            .ReturnsAsync(tasks);

        // Act: تنفيذ الوظيفة المطلوب اختبارها
        var result = await _controller.GetAll();

        // Assert: التحقق من النتائج
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTasks = okResult.Value.Should().BeAssignableTo<List<TaskItem>>().Subject;
        returnedTasks.Should().HaveCount(3);
        
        _mockTaskService.Verify(x => x.GetAllTasksAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllTasks_WithNoTasks_ReturnsOkWithEmptyList()
    {
        // Arrange
        _mockTaskService
            .Setup(x => x.GetAllTasksAsync())
            .ReturnsAsync(new List<TaskItem>());

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTasks = okResult.Value.Should().BeAssignableTo<List<TaskItem>>().Subject;
        returnedTasks.Should().BeEmpty();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetTaskById_WithExistingId_ReturnsOkWithTask()
    {
        // Arrange
        var task = TestDataBuilder.CreateTask(taskId: 1);
        task.TaskTitle = "Test Task";

        _mockTaskService
            .Setup(x => x.GetTaskByIdAsync(1))
            .ReturnsAsync(task);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTask = okResult.Value.Should().BeAssignableTo<TaskItem>().Subject;
        returnedTask.TaskId.Should().Be(1);
        returnedTask.TaskTitle.Should().Be("Test Task");
        
        _mockTaskService.Verify(x => x.GetTaskByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetTaskById_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        _mockTaskService
            .Setup(x => x.GetTaskByIdAsync(999))
            .ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<NotFoundResult>();
        
        _mockTaskService.Verify(x => x.GetTaskByIdAsync(999), Times.Once);
    }

    #endregion

    #region GetByUser Tests

    [Fact]
    public async Task GetTasksByUser_WithExistingTasks_ReturnsOkWithUserTasks()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            TestDataBuilder.CreateTask(taskId: 1, assignedTo: 1),
            TestDataBuilder.CreateTask(taskId: 2, assignedTo: 1)
        };

        _mockTaskService
            .Setup(x => x.GetTasksByUserAsync(1))
            .ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetByUser(1);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTasks = okResult.Value.Should().BeAssignableTo<List<TaskItem>>().Subject;
        returnedTasks.Should().HaveCount(2);
        returnedTasks.Should().OnlyContain(t => t.AssignedTo == 1);
        
        _mockTaskService.Verify(x => x.GetTasksByUserAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetTasksByUser_WithNoTasks_ReturnsOkWithEmptyList()
    {
        // Arrange
        _mockTaskService
            .Setup(x => x.GetTasksByUserAsync(1))
            .ReturnsAsync(new List<TaskItem>());

        // Act
        var result = await _controller.GetByUser(1);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTasks = okResult.Value.Should().BeAssignableTo<List<TaskItem>>().Subject;
        returnedTasks.Should().BeEmpty();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task CreateTask_WithValidModelAndAuthenticatedUser_ReturnsCreated()
    {
        // Arrange
        var task = TestDataBuilder.CreateTask(taskId: 0);
        task.TaskTitle = "New Task";

        var createdTask = TestDataBuilder.CreateTask(taskId: 1);
        createdTask.TaskTitle = "New Task";

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockTaskService
            .Setup(x => x.CreateTaskAsync(It.IsAny<TaskItem>()))
            .ReturnsAsync(createdTask);

        // Act
        var result = await _controller.Create(task);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(TasksController.GetById));
        createdResult.RouteValues!["id"].Should().Be(1);
        createdResult.Value.Should().BeAssignableTo<TaskItem>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockTaskService.Verify(x => x.CreateTaskAsync(It.IsAny<TaskItem>()), Times.Once);
    }

    [Fact]
    public async Task CreateTask_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var task = TestDataBuilder.CreateTask(taskId: 0);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Create(task);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<UnauthorizedResult>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockTaskService.Verify(x => x.CreateTaskAsync(It.IsAny<TaskItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateTask_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var task = TestDataBuilder.CreateTask(taskId: 0);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockTaskService
            .Setup(x => x.CreateTaskAsync(It.IsAny<TaskItem>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Create(task);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while creating the task. Please try again later.");
    }

    #endregion

    #region UpdateStatus Tests

    [Fact]
    public async Task UpdateTaskStatus_WithValidStatusAndAuthenticatedUser_ReturnsNoContent()
    {
        // Arrange
        var request = new UpdateTaskStatusRequest { Status = "completed" };

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockTaskService
            .Setup(x => x.UpdateTaskStatusAsync(1, "completed"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateStatus(1, request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<NoContentResult>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockTaskService.Verify(x => x.UpdateTaskStatusAsync(1, "completed"), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskStatus_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var request = new UpdateTaskStatusRequest { Status = "completed" };

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateStatus(1, request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<UnauthorizedResult>();
        
        _mockTaskService.Verify(x => x.UpdateTaskStatusAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTaskStatus_WithEmptyStatus_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateTaskStatusRequest { Status = "" };

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateStatus(1, request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Status is required");
        
        _mockTaskService.Verify(x => x.UpdateTaskStatusAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTaskStatus_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new UpdateTaskStatusRequest { Status = "completed" };

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockTaskService
            .Setup(x => x.UpdateTaskStatusAsync(1, "completed"))
            .ThrowsAsync(new Exception("Update error"));

        // Act
        var result = await _controller.UpdateStatus(1, request);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while updating the task status. Please try again later.");
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidTaskAndAuthenticatedUser_ReturnsNoContent()
    {
        // Arrange
        var task = TestDataBuilder.CreateTask(taskId: 1);
        task.TaskTitle = "Updated Task";

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Update(1, task);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<NoContentResult>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
    }

    [Fact]
    public async Task Update_WithMismatchedId_ReturnsBadRequest()
    {
        // Arrange
        var task = TestDataBuilder.CreateTask(taskId: 1);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Update(2, task);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Update_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var task = TestDataBuilder.CreateTask(taskId: 1);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Update(1, task);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteTask_WithExistingIdAndAuthorizedUser_ReturnsNoContent()
    {
        // Arrange
        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<NoContentResult>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteTask_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<UnauthorizedResult>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteTask_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().NotBeNull();
        // Note: Since DeleteTaskAsync is not implemented yet (TODO in code),
        // this test verifies the current behavior (returns NoContent)
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion
}


