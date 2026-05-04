using TaskManager.Application;
using TaskManager.Domain;

namespace TaskManager.Application.Tests;

public sealed class TaskValidationTests
{
    [Fact]
    public void ValidateTaskInput_throws_when_title_empty()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            TaskValidation.ValidateTaskInput(" ", "ok", null));
        Assert.Contains("Title", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureDefinedStatus_throws_for_invalid_enum_value()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            TaskValidation.EnsureDefinedStatus((TaskItemStatus)99));
        Assert.Contains("status", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateTaskInput_throws_when_title_exceeds_max_length()
    {
        var title = new string('x', TaskValidation.MaxTitleLength + 1);
        var ex = Assert.Throws<ValidationException>(() =>
            TaskValidation.ValidateTaskInput(title, "ok", null));
        Assert.Contains("Title", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateTaskInput_throws_when_description_exceeds_max_length()
    {
        var description = new string('y', TaskValidation.MaxDescriptionLength + 1);
        var ex = Assert.Throws<ValidationException>(() =>
            TaskValidation.ValidateTaskInput("ok", description, null));
        Assert.Contains("Description", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateTaskInput_accepts_boundary_lengths()
    {
        var title = new string('t', TaskValidation.MaxTitleLength);
        var description = new string('d', TaskValidation.MaxDescriptionLength);
        var exception = Record.Exception(() =>
            TaskValidation.ValidateTaskInput(title, description, null));
        Assert.Null(exception);
    }
}
