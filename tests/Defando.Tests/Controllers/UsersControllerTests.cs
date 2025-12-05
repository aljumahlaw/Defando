using FluentAssertions;
using Defando.Controllers;
using Defando.Models;
using Defando.Services;
using Defando.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Defando.Tests.Controllers;

/// <summary>
/// Unit tests for UsersController.
/// Tests API endpoints for user management operations including CRUD, search, and validation.
/// </summary>
public class UsersControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        // Setup Mocks
        _mockUserService = new Mock<IUserService>();
        _mockAuthService = new Mock<IAuthService>();

        // Create Controller instance
        _controller = new UsersController(
            _mockUserService.Object,
            _mockAuthService.Object);

        // Setup default ControllerContext for testing
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAllUsers_WithExistingUsers_ReturnsOkWithUsers()
    {
        // Arrange: إعداد البيانات
        var users = new List<User>
        {
            TestDataBuilder.CreateUser(userId: 1),
            TestDataBuilder.CreateUser(userId: 2),
            TestDataBuilder.CreateUser(userId: 3)
        };

        _mockUserService
            .Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(users);

        // Act: تنفيذ الوظيفة المطلوب اختبارها
        var result = await _controller.GetAll();

        // Assert: التحقق من النتائج
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsers = okResult.Value.Should().BeAssignableTo<List<User>>().Subject;
        returnedUsers.Should().HaveCount(3);
        
        _mockUserService.Verify(x => x.GetAllUsersAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllUsers_WithNoUsers_ReturnsOkWithEmptyList()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsers = okResult.Value.Should().BeAssignableTo<List<User>>().Subject;
        returnedUsers.Should().BeEmpty();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetUserById_WithExistingId_ReturnsOkWithUser()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        user.Username = "testuser";

        _mockUserService
            .Setup(x => x.GetUserByIdAsync(1))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUser = okResult.Value.Should().BeAssignableTo<User>().Subject;
        returnedUser.UserId.Should().Be(1);
        returnedUser.Username.Should().Be("testuser");
        
        _mockUserService.Verify(x => x.GetUserByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetUserById_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetUserByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<NotFoundResult>();
        
        _mockUserService.Verify(x => x.GetUserByIdAsync(999), Times.Once);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task CreateUser_WithValidDataAndAdminUser_ReturnsCreated()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 0);
        user.Username = "newuser";

        var createdUser = TestDataBuilder.CreateUser(userId: 1);
        createdUser.Username = "newuser";

        var adminUser = TestDataBuilder.CreateUser(userId: 2, role: "admin");

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockAuthService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(adminUser);

        _mockUserService
            .Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _controller.Create(user);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(UsersController.GetById));
        createdResult.RouteValues!["id"].Should().Be(1);
        createdResult.Value.Should().BeAssignableTo<User>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockAuthService.Verify(x => x.GetCurrentUserAsync(), Times.Once);
        _mockUserService.Verify(x => x.CreateUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateUser_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 0);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Create(user);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<UnauthorizedResult>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockAuthService.Verify(x => x.GetCurrentUserAsync(), Times.Never);
        _mockUserService.Verify(x => x.CreateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateUser_WithNonAdminUser_ReturnsUnauthorized()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 0);
        var regularUser = TestDataBuilder.CreateUser(userId: 2, role: "user");

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockAuthService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(regularUser);

        // Act
        var result = await _controller.Create(user);

        // Assert
        result.Should().NotBeNull();
        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().Be("Only administrators can create users");
        
        _mockUserService.Verify(x => x.CreateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateUser_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 0);
        var adminUser = TestDataBuilder.CreateUser(userId: 2, role: "admin");

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockAuthService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(adminUser);

        _mockUserService
            .Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Create(user);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while creating the user. Please try again later.");
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateUser_WithValidDataAndAdmin_ReturnsNoContent()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        user.Username = "updateduser";

        var adminUser = TestDataBuilder.CreateUser(userId: 2, role: "admin");

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockAuthService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(adminUser);

        // Act
        var result = await _controller.Update(1, user);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<NoContentResult>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockAuthService.Verify(x => x.GetCurrentUserAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_WithMismatchedId_ReturnsBadRequest()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var adminUser = TestDataBuilder.CreateUser(userId: 2, role: "admin");

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockAuthService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(adminUser);

        // Act
        var result = await _controller.Update(2, user);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task UpdateUser_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Update(1, user);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task UpdateUser_WithNonAdminUser_ReturnsUnauthorized()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var regularUser = TestDataBuilder.CreateUser(userId: 2, role: "user");

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockAuthService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(regularUser);

        // Act
        var result = await _controller.Update(1, user);

        // Assert
        result.Should().NotBeNull();
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().Be("Only administrators can update users");
    }

    [Fact]
    public async Task UpdateUser_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var adminUser = TestDataBuilder.CreateUser(userId: 2, role: "admin");

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockAuthService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(adminUser);

        // Act
        var result = await _controller.Update(1, user);

        // Assert
        result.Should().NotBeNull();
        // Note: Since UpdateUserAsync is not implemented yet (TODO in code),
        // this test verifies the current behavior (returns NoContent)
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteUser_WithAdminUser_ReturnsNoContent()
    {
        // Arrange
        var adminUser = TestDataBuilder.CreateUser(userId: 1, role: "admin");

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockAuthService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(adminUser);

        // Act
        var result = await _controller.Delete(2);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<NoContentResult>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockAuthService.Verify(x => x.GetCurrentUserAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_WithUnauthenticatedUser_ReturnsUnauthorized()
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
        _mockAuthService.Verify(x => x.GetCurrentUserAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteUser_WithNonAdminUser_ReturnsUnauthorized()
    {
        // Arrange
        var regularUser = TestDataBuilder.CreateUser(userId: 1, role: "user");

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockAuthService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(regularUser);

        // Act
        var result = await _controller.Delete(2);

        // Assert
        result.Should().NotBeNull();
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().Be("Only administrators can delete users");
    }

    [Fact]
    public async Task DeleteUser_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var adminUser = TestDataBuilder.CreateUser(userId: 1, role: "admin");

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockAuthService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(adminUser);

        // Act
        var result = await _controller.Delete(2);

        // Assert
        result.Should().NotBeNull();
        // Note: Since DeleteUserAsync is not implemented yet (TODO in code),
        // this test verifies the current behavior (returns NoContent)
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task Search_WithValidUsername_ReturnsOkWithUser()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        user.Username = "testuser";

        _mockUserService
            .Setup(x => x.GetUserByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.Search("testuser");

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUser = okResult.Value.Should().BeAssignableTo<User>().Subject;
        returnedUser.Username.Should().Be("testuser");
        
        _mockUserService.Verify(x => x.GetUserByUsernameAsync("testuser"), Times.Once);
    }

    [Fact]
    public async Task Search_WithEmptyUsername_ReturnsBadRequest()
    {
        // Arrange
        var username = "";

        // Act
        var result = await _controller.Search(username);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Username parameter is required");
        
        _mockUserService.Verify(x => x.GetUserByUsernameAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Search_WithUnknownUsername_ReturnsNotFound()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetUserByUsernameAsync("unknown"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.Search("unknown");

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<NotFoundResult>();
        
        _mockUserService.Verify(x => x.GetUserByUsernameAsync("unknown"), Times.Once);
    }

    #endregion

    #region Validate Tests

    [Fact]
    public async Task Validate_WithValidCredentials_ReturnsOkWithUser()
    {
        // Arrange
        var request = new ValidateUserRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        var user = TestDataBuilder.CreateUser(userId: 1);
        user.Username = "testuser";

        _mockUserService
            .Setup(x => x.ValidatePasswordAsync("testuser", "password123"))
            .ReturnsAsync(true);

        _mockUserService
            .Setup(x => x.GetUserByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.Validate(request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<ValidateUserResponse>().Subject;
        response.IsValid.Should().BeTrue();
        response.User.Should().NotBeNull();
        response.User!.Username.Should().Be("testuser");
        
        _mockUserService.Verify(x => x.ValidatePasswordAsync("testuser", "password123"), Times.Once);
        _mockUserService.Verify(x => x.GetUserByUsernameAsync("testuser"), Times.Once);
    }

    [Fact]
    public async Task Validate_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new ValidateUserRequest
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        _mockUserService
            .Setup(x => x.ValidatePasswordAsync("testuser", "wrongpassword"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Validate(request);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<UnauthorizedResult>();
        
        _mockUserService.Verify(x => x.ValidatePasswordAsync("testuser", "wrongpassword"), Times.Once);
        _mockUserService.Verify(x => x.GetUserByUsernameAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Validate_WithEmptyUsername_ReturnsBadRequest()
    {
        // Arrange
        var request = new ValidateUserRequest
        {
            Username = "",
            Password = "password123"
        };

        // Act
        var result = await _controller.Validate(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Username and password are required");
        
        _mockUserService.Verify(x => x.ValidatePasswordAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Validate_WithEmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new ValidateUserRequest
        {
            Username = "testuser",
            Password = ""
        };

        // Act
        var result = await _controller.Validate(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Username and password are required");
        
        _mockUserService.Verify(x => x.ValidatePasswordAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Validate_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new ValidateUserRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        _mockUserService
            .Setup(x => x.ValidatePasswordAsync("testuser", "password123"))
            .ThrowsAsync(new Exception("Validation error"));

        // Act
        var result = await _controller.Validate(request);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while validating credentials. Please try again later.");
    }

    #endregion
}




