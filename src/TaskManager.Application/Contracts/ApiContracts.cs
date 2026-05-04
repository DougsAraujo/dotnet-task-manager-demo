using TaskManager.Domain;

namespace TaskManager.Application.Contracts;

public sealed record TaskItemResponse(
    Guid Id,
    Guid UserId,
    string Title,
    string Description,
    TaskItemStatus Status,
    DateTime? DueDate);

public sealed record CreateTaskRequest(string Title, string Description, TaskItemStatus Status, DateTime? DueDate);

public sealed record UpdateTaskRequest(string Title, string Description, TaskItemStatus Status, DateTime? DueDate);

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthResponse(string Token, Guid UserId, string Email, string DisplayName);
