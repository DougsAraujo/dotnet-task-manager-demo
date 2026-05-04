using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TaskManager.Application.Contracts;
using TaskManager.Domain;

namespace TaskManager.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"tm_api_{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = $"Data Source={_dbPath}",
                ["Jwt:Issuer"] = "TaskManager",
                ["Jwt:Audience"] = "TaskManagerClients",
                ["Jwt:SigningKey"] = "TEST_KEY_MUST_BE_AT_LEAST_32_CHARS!!",
                ["Jwt:ExpiryMinutes"] = "60",
                ["DemoUser:Email"] = $"demo_{Guid.NewGuid():N}@local.test",
                ["DemoUser:Password"] = "Demo123!",
                ["DemoUser:DisplayName"] = "Demo"
            });
        });
    }
}

public sealed class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiIntegrationTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Info_is_public()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Tasks_require_auth()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_login_crud_flow()
    {
        var client = _factory.CreateClient();
        var email = $"user_{Guid.NewGuid():N}@test.local";

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password1!", "Tester"));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password1!"));
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth?.Token);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var empty = await client.GetFromJsonAsync<List<TaskItemResponse>>("/api/tasks");
        Assert.NotNull(empty);
        Assert.Empty(empty!);

        var create = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Item", "Body", TaskItemStatus.Pending, null));
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<TaskItemResponse>();
        Assert.NotNull(created);

        var list = await client.GetFromJsonAsync<List<TaskItemResponse>>("/api/tasks");
        Assert.Single(list!);

        var put = await client.PutAsJsonAsync($"/api/tasks/{created!.Id}", new UpdateTaskRequest("Item2", "Body2", TaskItemStatus.Completed, null));
        put.EnsureSuccessStatusCode();

        var del = await client.DeleteAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_bad_request()
    {
        var client = _factory.CreateClient();
        var email = $"badpwd_{Guid.NewGuid():N}@test.local";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password1!", "T"));
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword1!"));

        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
    }

    [Fact]
    public async Task Register_duplicate_email_returns_conflict()
    {
        var client = _factory.CreateClient();
        var email = $"dup_{Guid.NewGuid():N}@test.local";

        var first = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password1!", "A"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password2!", "B"));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Get_unknown_task_returns_not_found()
    {
        var client = _factory.CreateClient();
        var email = $"nf_{Guid.NewGuid():N}@test.local";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password1!", "T"));
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password1!"));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var response = await client.GetAsync($"/api/tasks/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task User_cannot_read_another_users_task()
    {
        var client = _factory.CreateClient();
        var userA = $"a_{Guid.NewGuid():N}@test.local";
        var userB = $"b_{Guid.NewGuid():N}@test.local";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(userA, "Password1!", "A"));
        var loginA = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(userA, "Password1!"));
        var tokenA = (await loginA.Content.ReadFromJsonAsync<AuthResponse>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        var create = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Secret", "", TaskItemStatus.Pending, null));
        create.EnsureSuccessStatusCode();
        var taskId = (await create.Content.ReadFromJsonAsync<TaskItemResponse>())!.Id;

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(userB, "Password1!", "B"));
        var loginB = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(userB, "Password1!"));
        var tokenB = (await loginB.Content.ReadFromJsonAsync<AuthResponse>())!.Token;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var probe = await client.GetAsync($"/api/tasks/{taskId}");
        Assert.Equal(HttpStatusCode.NotFound, probe.StatusCode);
    }
}
