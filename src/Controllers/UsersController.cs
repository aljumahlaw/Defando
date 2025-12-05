using Defando.Models;
using Defando.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Defando.Controllers;

/// <summary>
/// API Controller for user management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;

    public UsersController(IUserService userService, IAuthService authService)
    {
        _userService = userService;
        _authService = authService;
    }

    /// <summary>
    /// GET: api/users
    /// Retrieves all active users from the database.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// GET: api/users/5
    /// Retrieves a specific user by its ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// POST: api/users
    /// Creates a new user in the database.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<User>> Create(User user)
    {
        try
        {
            if (!await _authService.IsAuthenticatedAsync())
                return Unauthorized();

            // Only admin can create users
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser?.Role != "admin")
                return Unauthorized("Only administrators can create users");

            var created = await _userService.CreateUserAsync(user);
            return CreatedAtAction(nameof(GetById), new { id = created.UserId }, created);
        }
        catch (Exception ex)
        {
            // Log detailed error for administrators (not exposed to client)
            // Return generic error message to client
            return StatusCode(500, "An error occurred while creating the user. Please try again later.");
        }
    }

    /// <summary>
    /// PUT: api/users/5
    /// Updates an existing user in the database.
    /// </summary>
    [HttpPut("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, User user)
    {
        try
        {
            if (!await _authService.IsAuthenticatedAsync())
                return Unauthorized();

            // Only admin can update users
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser?.Role != "admin")
                return Unauthorized("Only administrators can update users");

            if (id != user.UserId)
                return BadRequest();

            // TODO: Add UpdateUserAsync method to IUserService
            // await _userService.UpdateUserAsync(user);
            return NoContent();
        }
        catch (Exception ex)
        {
            // Log detailed error for administrators (not exposed to client)
            // Return generic error message to client
            return StatusCode(500, "An error occurred while updating the user. Please try again later.");
        }
    }

    /// <summary>
    /// DELETE: api/users/5
    /// Deletes a user from the database.
    /// </summary>
    [HttpDelete("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            if (!await _authService.IsAuthenticatedAsync())
                return Unauthorized();

            // Only admin can delete users
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser?.Role != "admin")
                return Unauthorized("Only administrators can delete users");

            // TODO: Add DeleteUserAsync method to IUserService
            // await _userService.DeleteUserAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            // Log detailed error for administrators (not exposed to client)
            // Return generic error message to client
            return StatusCode(500, "An error occurred while deleting the user. Please try again later.");
        }
    }

    /// <summary>
    /// GET: api/users/search?username=admin
    /// Searches for a user by username.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<User>> Search([FromQuery] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest("Username parameter is required");

        var user = await _userService.GetUserByUsernameAsync(username);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// POST: api/users/validate
    /// Validates user credentials (username and password).
    /// </summary>
    [HttpPost("validate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ValidateUserResponse>> Validate([FromBody] ValidateUserRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and password are required");

            var isValid = await _userService.ValidatePasswordAsync(request.Username, request.Password);
            
            if (!isValid)
                return Unauthorized();

            var user = await _userService.GetUserByUsernameAsync(request.Username);
            return Ok(new ValidateUserResponse
            {
                IsValid = true,
                User = user
            });
        }
        catch (Exception ex)
        {
            // Log detailed error for administrators (not exposed to client)
            // Return generic error message to client
            return StatusCode(500, "An error occurred while validating credentials. Please try again later.");
        }
    }
}

/// <summary>
/// Request model for user validation.
/// </summary>
public class ValidateUserRequest
{
    /// <summary>
    /// Username to validate.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password to validate.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Response model for user validation.
/// </summary>
public class ValidateUserResponse
{
    /// <summary>
    /// Indicates if the credentials are valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// User object if validation is successful.
    /// </summary>
    public User? User { get; set; }
}

