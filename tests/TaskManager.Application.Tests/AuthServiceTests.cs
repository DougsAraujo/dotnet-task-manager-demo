using Moq;
using TaskManager.Application;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Contracts;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_creates_user_and_returns_token()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        users.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("HASH");

        var jwt = new Mock<IJwtTokenGenerator>();
        jwt.Setup(x => x.CreateToken(It.IsAny<User>())).Returns("TOKEN");

        var sut = new AuthService(users.Object, hasher.Object, jwt.Object);
        var result = await sut.RegisterAsync(new RegisterRequest("new@example.com", "longenough", "Name"));

        Assert.Equal("TOKEN", result.Token);
        Assert.Equal("new@example.com", result.Email);
        users.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        jwt.Verify(x => x.CreateToken(It.Is<User>(u => u.Email == "new@example.com")), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_throws_when_email_exists()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Email = "a@b.com", PasswordHash = "x", DisplayName = "x" });

        var sut = new AuthService(users.Object, Mock.Of<IPasswordHasher>(), Mock.Of<IJwtTokenGenerator>());
        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RegisterAsync(new RegisterRequest("a@b.com", "longenough", "Name")));
    }

    [Fact]
    public async Task LoginAsync_returns_token_when_credentials_valid()
    {
        var email = "login@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "STORED_HASH",
            DisplayName = "Login User"
        };

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("correct-password", "STORED_HASH")).Returns(true);

        var jwt = new Mock<IJwtTokenGenerator>();
        jwt.Setup(x => x.CreateToken(user)).Returns("JWT");

        var sut = new AuthService(users.Object, hasher.Object, jwt.Object);
        var result = await sut.LoginAsync(new LoginRequest(email, "correct-password"));

        Assert.Equal("JWT", result.Token);
        Assert.Equal(email, result.Email);
        jwt.Verify(x => x.CreateToken(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_throws_when_user_not_found()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByEmailAsync("missing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = new AuthService(users.Object, Mock.Of<IPasswordHasher>(), Mock.Of<IJwtTokenGenerator>());
        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.LoginAsync(new LoginRequest("missing@example.com", "any-password")));
    }

    [Fact]
    public async Task LoginAsync_throws_when_password_invalid()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "u@example.com",
            PasswordHash = "HASH",
            DisplayName = "U"
        };
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByEmailAsync("u@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("wrong", "HASH")).Returns(false);

        var sut = new AuthService(users.Object, hasher.Object, Mock.Of<IJwtTokenGenerator>());
        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.LoginAsync(new LoginRequest("u@example.com", "wrong")));
    }
}
