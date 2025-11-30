using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace LegalDocSystem.Tests.Helpers;

/// <summary>
/// Helper class for creating Mock IHttpContextAccessor instances for testing.
/// </summary>
public static class MockHttpContextAccessor
{
    /// <summary>
    /// Creates a Mock IHttpContextAccessor with an authenticated user.
    /// </summary>
    /// <param name="userId">The user ID (default: 1).</param>
    /// <param name="username">The username (default: "testuser").</param>
    /// <param name="role">The user role (default: "user").</param>
    /// <returns>A Mock IHttpContextAccessor configured with the authenticated user.</returns>
    public static Mock<IHttpContextAccessor> CreateMockHttpContext(
        int? userId = null,
        string? username = null,
        string? role = null)
    {
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var session = new Mock<ISession>();

        // Setup authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, (userId ?? 1).ToString()),
            new Claim(ClaimTypes.Name, username ?? "testuser"),
            new Claim(ClaimTypes.Role, role ?? "user")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        httpContext.User = new ClaimsPrincipal(identity);

        // Setup Session
        var sessionId = Guid.NewGuid().ToString();
        session.Setup(s => s.Id).Returns(sessionId);
        session.Setup(s => s.IsAvailable).Returns(true);
        session.Setup(s => s.Keys).Returns(new List<string>());

        // Setup Session Get/Set methods
        var sessionData = new Dictionary<string, byte[]>();
        session.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((key, value) => sessionData[key] = value);
        session.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny))
            .Returns((string key, out byte[] value) =>
            {
                if (sessionData.TryGetValue(key, out value!))
                    return true;
                value = null!;
                return false;
            });

        // Setup Session GetInt32
        session.Setup(s => s.GetInt32(It.IsAny<string>()))
            .Returns<string>(key =>
            {
                if (sessionData.TryGetValue(key, out var bytes) && bytes.Length == 4)
                {
                    return BitConverter.ToInt32(bytes, 0);
                }
                return null;
            });

        // Setup Session GetString
        session.Setup(s => s.GetString(It.IsAny<string>()))
            .Returns<string>(key =>
            {
                if (sessionData.TryGetValue(key, out var bytes))
                {
                    return System.Text.Encoding.UTF8.GetString(bytes);
                }
                return null;
            });

        httpContext.Session = session.Object;

        // Setup ServiceProvider for Authentication
        var serviceProvider = new Mock<IServiceProvider>();
        var authenticationService = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        
        serviceProvider
            .Setup(sp => sp.GetService(typeof(Microsoft.AspNetCore.Authentication.IAuthenticationService)))
            .Returns(authenticationService.Object);

        httpContext.RequestServices = serviceProvider.Object;

        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        return mockHttpContextAccessor;
    }

    /// <summary>
    /// Creates a Mock IHttpContextAccessor with an unauthenticated user.
    /// </summary>
    /// <returns>A Mock IHttpContextAccessor configured with no authenticated user.</returns>
    public static Mock<IHttpContextAccessor> CreateUnauthenticatedHttpContext()
    {
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var session = new Mock<ISession>();

        // No authenticated user
        httpContext.User = new ClaimsPrincipal();

        // Setup Session
        session.Setup(s => s.Id).Returns(Guid.NewGuid().ToString());
        session.Setup(s => s.IsAvailable).Returns(true);
        session.Setup(s => s.Keys).Returns(new List<string>());

        httpContext.Session = session.Object;

        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        return mockHttpContextAccessor;
    }
}

