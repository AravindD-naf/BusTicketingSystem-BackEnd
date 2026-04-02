using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using BusTicketingSystem.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace BusTicketingSystem.Tests.Services;

public class AuthServiceTests
{
    // ── Shared mocks & SUT ────────────────────────────────────────────────────
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly JwtHelper             _jwtHelper;
    private readonly AuthService           _sut;

    public AuthServiceTests()
    {
        // Build a minimal IConfiguration that satisfies JwtHelper
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"]            = "SuperSecretTestKeyThatIs32CharsLong!!",
                ["JwtSettings:Issuer"]         = "TestIssuer",
                ["JwtSettings:Audience"]       = "TestAudience",
                ["JwtSettings:ExpiryMinutes"]  = "60"
            })
            .Build();

        _jwtHelper = new JwtHelper(config);
        _sut = new AuthService(_userRepoMock.Object, _jwtHelper);
    }

    // ── RegisterAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_NewEmail_CreatesUserAndReturnsToken()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName    = "Jane Doe",
            Email       = "jane@test.com",
            PhoneNumber = "9876543210",
            Password    = "Pass@123"
        };

        _userRepoMock.Setup(r => r.EmailExistsAsync("jane@test.com")).ReturnsAsync(false);

        User? capturedUser = null;
        _userRepoMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        _userRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // GetByIdWithRoleAsync returns the user with a role so JWT can be generated
        _userRepoMock
            .Setup(r => r.GetByIdWithRoleAsync(It.IsAny<int>()))
            .ReturnsAsync(() => new User
            {
                UserId   = 5,
                FullName = request.FullName,
                Email    = request.Email.ToLower(),
                RoleId   = 2,
                Role     = new Role { RoleId = 2, Name = "Customer" }
            });

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.Expiry.Should().BeAfter(DateTime.UtcNow);

        capturedUser.Should().NotBeNull();
        capturedUser!.Email.Should().Be("jane@test.com");
        capturedUser.RoleId.Should().Be(2);
        BCrypt.Net.BCrypt.Verify("Pass@123", capturedUser.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsBadRequestException()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var request = new RegisterRequest
        {
            FullName = "Dup", Email = "dup@test.com",
            PhoneNumber = "111", Password = "pwd"
        };

        // Act
        Func<Task> act = () => _sut.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Email already exists*");
    }

    [Fact]
    public async Task RegisterAsync_NullRequest_ThrowsValidationException()
    {
        // Act & Assert
        Func<Task> act = () => _sut.RegisterAsync(null!);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RegisterAsync_EmailIsTrimmedAndLowercased()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Test", Email = "  Upper@Test.COM  ",
            PhoneNumber = "123", Password = "pwd"
        };

        _userRepoMock.Setup(r => r.EmailExistsAsync("upper@test.com")).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _userRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _userRepoMock.Setup(r => r.GetByIdWithRoleAsync(It.IsAny<int>()))
            .ReturnsAsync(new User
            {
                UserId = 6, Email = "upper@test.com",
                Role = new Role { RoleId = 2, Name = "Customer" }
            });

        // Act
        await _sut.RegisterAsync(request);

        // Assert: EmailExistsAsync was called with the normalised email
        _userRepoMock.Verify(r => r.EmailExistsAsync("upper@test.com"), Times.Once);
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var user = TestDataBuilder.CustomerUser();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123");

        _userRepoMock
            .Setup(r => r.GetByEmailWithRoleAsync(user.Email))
            .ReturnsAsync(user);

        var request = new LoginRequest { Email = user.Email, Password = "Customer@123" };

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsBadRequestException()
    {
        // Arrange
        var user = TestDataBuilder.CustomerUser();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword");

        _userRepoMock
            .Setup(r => r.GetByEmailWithRoleAsync(user.Email))
            .ReturnsAsync(user);

        var request = new LoginRequest { Email = user.Email, Password = "WrongPassword" };

        // Act
        Func<Task> act = () => _sut.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Invalid credentials*");
    }

    [Fact]
    public async Task LoginAsync_NonExistentUser_ThrowsBadRequestException()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.GetByEmailWithRoleAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var request = new LoginRequest { Email = "ghost@test.com", Password = "whatever" };

        // Act
        Func<Task> act = () => _sut.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }
}
