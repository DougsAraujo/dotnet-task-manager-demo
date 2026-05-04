using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure;

namespace TaskManager.Infrastructure.Tests;

public sealed class UserRepositoryTests : IAsyncLifetime
{
    private string _dbPath = null!;
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"tm_users_{Guid.NewGuid():N}.db");
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
    public async Task Create_GetById_GetByEmail_roundtrip()
    {
        var users = _provider.GetRequiredService<IUserRepository>();
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Email = "roundtrip@test.local",
            PasswordHash = "HASH",
            DisplayName = "RT"
        };

        await users.CreateAsync(user);

        var byId = await users.GetByIdAsync(id);
        Assert.NotNull(byId);
        Assert.Equal("roundtrip@test.local", byId!.Email);

        var byEmail = await users.GetByEmailAsync("roundtrip@test.local");
        Assert.NotNull(byEmail);
        Assert.Equal(id, byEmail!.Id);
    }

    [Fact]
    public async Task GetById_returns_null_for_unknown_user()
    {
        var users = _provider.GetRequiredService<IUserRepository>();
        Assert.Null(await users.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByEmail_returns_null_when_missing()
    {
        var users = _provider.GetRequiredService<IUserRepository>();
        Assert.Null(await users.GetByEmailAsync("nobody-here@test.local"));
    }
}
