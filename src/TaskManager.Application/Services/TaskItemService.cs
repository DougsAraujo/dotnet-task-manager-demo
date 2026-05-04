using TaskManager.Application.Abstractions;
using TaskManager.Application.Contracts;
using TaskManager.Domain;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Services;

public sealed class TaskItemService(ITaskRepository tasks)
{
    public async Task<IReadOnlyList<TaskItemResponse>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var list = await tasks.ListByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return list.Select(Map).ToList();
    }

    public async Task<TaskItemResponse> GetAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await tasks.GetByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task is null || task.UserId != userId)
        {
            throw new NotFoundException("Task was not found.");
        }

        return Map(task);
    }

    public async Task<TaskItemResponse> CreateAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        TaskValidation.EnsureDefinedStatus(request.Status);
        TaskValidation.ValidateTaskInput(request.Title, request.Description, request.DueDate);

        var entity = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = request.Status,
            DueDate = request.DueDate
        };

        var created = await tasks.CreateAsync(entity, cancellationToken).ConfigureAwait(false);
        return Map(created);
    }

    public async Task<TaskItemResponse> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        TaskValidation.EnsureDefinedStatus(request.Status);
        TaskValidation.ValidateTaskInput(request.Title, request.Description, request.DueDate);

        var existing = await tasks.GetByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (existing is null || existing.UserId != userId)
        {
            throw new NotFoundException("Task was not found.");
        }

        existing.Title = request.Title.Trim();
        existing.Description = request.Description.Trim();
        existing.Status = request.Status;
        existing.DueDate = request.DueDate;

        var updated = await tasks.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        if (!updated)
        {
            throw new NotFoundException("Task was not found.");
        }

        return Map(existing);
    }

    public async Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var deleted = await tasks.DeleteAsync(taskId, userId, cancellationToken).ConfigureAwait(false);
        if (!deleted)
        {
            throw new NotFoundException("Task was not found.");
        }
    }

    private static TaskItemResponse Map(TaskItem t) =>
        new(t.Id, t.UserId, t.Title, t.Description, t.Status, t.DueDate);
}
