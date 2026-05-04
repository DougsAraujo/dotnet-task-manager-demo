using Moq;
using TaskManager.Application;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Contracts;
using TaskManager.Application.Services;
using TaskManager.Domain;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tests;

public sealed class TaskItemServiceTests
{
    [Fact]
    public async Task GetAsync_throws_when_task_owned_by_other_user()
    {
        var tasks = new Mock<ITaskRepository>();
        tasks.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Title = "t",
                Description = "d",
                Status = TaskItemStatus.Pending
            });

        var sut = new TaskItemService(tasks.Object);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.GetAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_persists_task()
    {
        var tasks = new Mock<ITaskRepository>();
        tasks.Setup(x => x.CreateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem t, CancellationToken _) => t);

        var userId = Guid.NewGuid();
        var sut = new TaskItemService(tasks.Object);
        var dto = await sut.CreateAsync(userId, new CreateTaskRequest("Title", "Desc", TaskItemStatus.InProgress, null));

        Assert.Equal("Title", dto.Title);
        Assert.Equal(TaskItemStatus.InProgress, dto.Status);
        tasks.Verify(x => x.CreateAsync(It.Is<TaskItem>(t => t.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_maps_repository_items()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var tasks = new Mock<ITaskRepository>();
        tasks.Setup(x => x.ListByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaskItem>
            {
                new()
                {
                    Id = taskId,
                    UserId = userId,
                    Title = "One",
                    Description = "D",
                    Status = TaskItemStatus.Pending
                }
            });

        var sut = new TaskItemService(tasks.Object);
        var list = await sut.ListAsync(userId);

        Assert.Single(list);
        Assert.Equal("One", list[0].Title);
        Assert.Equal(taskId, list[0].Id);
    }

    [Fact]
    public async Task GetAsync_returns_task_when_user_owns_it()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var entity = new TaskItem
        {
            Id = taskId,
            UserId = userId,
            Title = "Mine",
            Description = "",
            Status = TaskItemStatus.Completed
        };

        var tasks = new Mock<ITaskRepository>();
        tasks.Setup(x => x.GetByIdAsync(taskId, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var sut = new TaskItemService(tasks.Object);
        var dto = await sut.GetAsync(userId, taskId);

        Assert.Equal("Mine", dto.Title);
        Assert.Equal(TaskItemStatus.Completed, dto.Status);
    }

    [Fact]
    public async Task GetAsync_throws_when_task_missing()
    {
        var tasks = new Mock<ITaskRepository>();
        tasks.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var sut = new TaskItemService(tasks.Object);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.GetAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_persists_changes_when_owner()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var existing = new TaskItem
        {
            Id = taskId,
            UserId = userId,
            Title = "Old",
            Description = "OldD",
            Status = TaskItemStatus.Pending
        };

        var tasks = new Mock<ITaskRepository>();
        tasks.Setup(x => x.GetByIdAsync(taskId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        tasks.Setup(x => x.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = new TaskItemService(tasks.Object);
        var dto = await sut.UpdateAsync(userId, taskId, new UpdateTaskRequest("New", "NewD", TaskItemStatus.InProgress, null));

        Assert.Equal("New", dto.Title);
        Assert.Equal(TaskItemStatus.InProgress, dto.Status);
        tasks.Verify(x => x.UpdateAsync(It.Is<TaskItem>(t => t.Title == "New" && t.Description == "NewD"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_throws_when_repository_reports_no_row()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var existing = new TaskItem
        {
            Id = taskId,
            UserId = userId,
            Title = "T",
            Description = "D",
            Status = TaskItemStatus.Pending
        };

        var tasks = new Mock<ITaskRepository>();
        tasks.Setup(x => x.GetByIdAsync(taskId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        tasks.Setup(x => x.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = new TaskItemService(tasks.Object);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.UpdateAsync(userId, taskId, new UpdateTaskRequest("A", "B", TaskItemStatus.Pending, null)));
    }

    [Fact]
    public async Task DeleteAsync_calls_repository()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var tasks = new Mock<ITaskRepository>();
        tasks.Setup(x => x.DeleteAsync(taskId, userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = new TaskItemService(tasks.Object);
        await sut.DeleteAsync(userId, taskId);

        tasks.Verify(x => x.DeleteAsync(taskId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_throws_when_not_deleted()
    {
        var tasks = new Mock<ITaskRepository>();
        tasks.Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new TaskItemService(tasks.Object);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.DeleteAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_throws_when_task_owned_by_other_user()
    {
        var taskId = Guid.NewGuid();
        var tasks = new Mock<ITaskRepository>();
        tasks.Setup(x => x.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskItem
            {
                Id = taskId,
                UserId = Guid.NewGuid(),
                Title = "Other",
                Description = "",
                Status = TaskItemStatus.Pending
            });

        var sut = new TaskItemService(tasks.Object);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.UpdateAsync(Guid.NewGuid(), taskId, new UpdateTaskRequest("X", "Y", TaskItemStatus.Pending, null)));
    }
}
