using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using Xunit;

namespace LegalDocSystem.Tests.Integration.UITests;

/// <summary>
/// UI Tests for Login page using Selenium WebDriver.
/// </summary>
public class LoginUITests : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl = "http://localhost:5001";

    public LoginUITests()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless"); // Run in headless mode for CI/CD
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        
        _driver = new ChromeDriver(options);
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
    }

    [Fact]
    public void TestLoginPageElements()
    {
        // Arrange
        _driver.Navigate().GoToUrl($"{_baseUrl}/login");

        // Act & Assert
        var usernameField = _driver.FindElement(By.Id("username"));
        Assert.NotNull(usernameField);
        Assert.True(usernameField.Displayed);

        var passwordField = _driver.FindElement(By.Id("password"));
        Assert.NotNull(passwordField);
        Assert.True(passwordField.Displayed);
        Assert.Equal("password", passwordField.GetAttribute("type"));

        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
        Assert.NotNull(loginButton);
        Assert.True(loginButton.Displayed);
    }

    [Fact]
    public void TestLoginSuccess()
    {
        // Arrange
        _driver.Navigate().GoToUrl($"{_baseUrl}/login");

        // Act
        var usernameField = _driver.FindElement(By.Id("username"));
        var passwordField = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));

        usernameField.SendKeys("admin");
        passwordField.SendKeys("AdminPassword123");
        loginButton.Click();

        // Assert
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => !d.Url.Contains("/login") && d.Url.Contains("/"));

        Assert.DoesNotContain("/login", _driver.Url);
    }

    [Fact]
    public void TestLoginFailure()
    {
        // Arrange
        _driver.Navigate().GoToUrl($"{_baseUrl}/login");

        // Act
        var usernameField = _driver.FindElement(By.Id("username"));
        var passwordField = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));

        usernameField.SendKeys("wronguser");
        passwordField.SendKeys("wrongpass");
        loginButton.Click();

        // Assert
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        var errorMessage = wait.Until(d =>
        {
            try
            {
                return d.FindElement(By.CssSelector(".alert-danger"));
            }
            catch
            {
                return null;
            }
        });

        Assert.NotNull(errorMessage);
        Assert.True(errorMessage.Displayed);
        Assert.Contains("اسم المستخدم أو كلمة المرور", errorMessage.Text);
        Assert.DoesNotContain("Stack Trace", errorMessage.Text);
        Assert.DoesNotContain("Exception", errorMessage.Text);
    }

    [Fact]
    public void TestClientSideValidation()
    {
        // Arrange
        _driver.Navigate().GoToUrl($"{_baseUrl}/login");

        // Act - Try to submit without filling fields
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
        loginButton.Click();

        // Assert - Should stay on login page
        Assert.Contains("/login", _driver.Url);

        // Check if validation messages appear (if using HTML5 validation)
        var usernameField = _driver.FindElement(By.Id("username"));
        var isRequired = usernameField.GetAttribute("required");
        Assert.NotNull(isRequired);
    }

    [Fact]
    public void TestResponsiveDesign()
    {
        // Arrange
        _driver.Navigate().GoToUrl($"{_baseUrl}/login");

        // Test Mobile size
        _driver.Manage().Window.Size = new System.Drawing.Size(375, 667);
        var container = _driver.FindElement(By.CssSelector(".container"));
        Assert.True(container.Displayed);

        // Test Tablet size
        _driver.Manage().Window.Size = new System.Drawing.Size(768, 1024);
        Assert.True(container.Displayed);

        // Test Desktop size
        _driver.Manage().Window.Size = new System.Drawing.Size(1920, 1080);
        Assert.True(container.Displayed);
    }

    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }
}

