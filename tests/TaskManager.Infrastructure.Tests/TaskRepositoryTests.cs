using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Abstractions;
using TaskManager.Domain;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure;

namespace TaskManager.Infrastructure.Tests;

public sealed class TaskRepositoryTests : IAsyncLifetime
{
    private string _dbPath = null!;
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"tm_repo_{Guid.NewGuid():N}.db");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:ConnectionString"] = $"Data Source={_dbPath}",
            ["Jwt:Issuer"] = "test",
            ["Jwt:Audience"] = "test",
            ["Jwt:SigningKey"] = "TEST_KEY_MUST_BE_AT_LEAST_32_CHARS!!",
            ["Jwt:ExpiryMinutes"] = "60",
            ["DemoUser:Email"] = "seed@test.local",
            ["DemoUser:Password"] = "unused",
            ["DemoUser:DisplayName"] = "unused"
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);
        _provider = services.BuildServiceProvider();

        await _provider.GetRequiredService<IDatabaseMigrator>().MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch
        {
            // ignore locked file on Windows
        }
    }

    [Fact]
    public async Task Task_roundtrip_for_user()
    {
        var users = _provider.GetRequiredService<IUserRepository>();
        var tasks = _provider.GetRequiredService<ITaskRepository>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "repo@test.local",
            PasswordHash = "HASH",
            DisplayName = "Repo"
        };
        await users.CreateAsync(user);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "A",
            Description = "B",
            Status = TaskItemStatus.Pending,
            DueDate = DateTime.UtcNow.Date
        };
        await tasks.CreateAsync(task);

        var list = await tasks.ListByUserAsync(user.Id);
        Assert.Single(list);

        var loaded = await tasks.GetByIdAsync(task.Id);
        Assert.NotNull(loaded);
        Assert.Equal("A", loaded.Title);

        loaded.Status = TaskItemStatus.Completed;
        Assert.True(await tasks.UpdateAsync(loaded));

        Assert.True(await tasks.DeleteAsync(task.Id, user.Id));
        Assert.Null(await tasks.GetByIdAsync(task.Id));
    }
}
