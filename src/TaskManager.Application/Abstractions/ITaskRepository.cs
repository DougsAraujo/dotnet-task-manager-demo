using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaskItem> CreateAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
