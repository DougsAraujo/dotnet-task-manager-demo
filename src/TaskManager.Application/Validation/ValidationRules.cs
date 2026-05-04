using System.Net.Mail;
using System.Text.RegularExpressions;
using TaskManager.Domain;

namespace TaskManager.Application;

public static class TaskValidation
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 4000;

    public static void ValidateTaskInput(string title, string description, DateTime? dueDate)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Title is required.");
        }

        if (title.Length > MaxTitleLength)
        {
            throw new ValidationException($"Title must be at most {MaxTitleLength} characters.");
        }

        if (description.Length > MaxDescriptionLength)
        {
            throw new ValidationException($"Description must be at most {MaxDescriptionLength} characters.");
        }

        _ = dueDate;
    }

    public static void EnsureDefinedStatus(TaskItemStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ValidationException("Invalid task status.");
        }
    }
}

public static class UserValidation
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public const int MinPasswordLength = 8;
    public const int MaxDisplayNameLength = 120;

    public static void ValidateRegister(string email, string password, string displayName)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email is required.");
        }

        email = email.Trim();
        if (!EmailRegex.IsMatch(email))
        {
            throw new ValidationException("Email format is invalid.");
        }

        try
        {
            _ = new MailAddress(email);
        }
        catch
        {
            throw new ValidationException("Email format is invalid.");
        }

        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
        {
            throw new ValidationException($"Password must be at least {MinPasswordLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ValidationException("Display name is required.");
        }

        if (displayName.Length > MaxDisplayNameLength)
        {
            throw new ValidationException($"Display name must be at most {MaxDisplayNameLength} characters.");
        }
    }

    public static void ValidateLogin(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            throw new ValidationException("Email and password are required.");
        }
    }
}
