using TaskManager.Application;

namespace TaskManager.Application.Tests;

public sealed class UserValidationTests
{
    [Fact]
    public void ValidateRegister_throws_when_email_empty()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            UserValidation.ValidateRegister(" ", "password1", "Name"));
        Assert.Contains("Email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateRegister_throws_when_email_invalid()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            UserValidation.ValidateRegister("not-an-email", "password1", "Name"));
        Assert.Contains("Email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateRegister_throws_when_password_too_short()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            UserValidation.ValidateRegister("a@b.com", "short", "Name"));
        Assert.Contains("Password", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateRegister_throws_when_display_name_empty()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            UserValidation.ValidateRegister("a@b.com", "password1", "  "));
        Assert.Contains("Display", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateRegister_throws_when_display_name_too_long()
    {
        var longName = new string('x', UserValidation.MaxDisplayNameLength + 1);
        var ex = Assert.Throws<ValidationException>(() =>
            UserValidation.ValidateRegister("a@b.com", "password1", longName));
        Assert.Contains("Display", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateRegister_accepts_valid_input()
    {
        var exception = Record.Exception(() =>
            UserValidation.ValidateRegister("user@example.com", "password1", "Display"));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("", "p")]
    [InlineData("a@b.com", "")]
    public void ValidateLogin_throws_when_email_or_password_missing(string email, string password)
    {
        Assert.Throws<ValidationException>(() => UserValidation.ValidateLogin(email, password));
    }

    [Fact]
    public void ValidateLogin_accepts_valid_input()
    {
        var exception = Record.Exception(() =>
            UserValidation.ValidateLogin("user@example.com", "secret"));
        Assert.Null(exception);
    }
}
