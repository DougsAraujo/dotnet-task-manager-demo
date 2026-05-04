namespace TaskManager.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = "Data Source=taskmanager.db";
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TaskManager";
    public string Audience { get; set; } = "TaskManagerClients";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 480;
}

public sealed class DemoUserOptions
{
    public const string SectionName = "DemoUser";

    public string Email { get; set; } = "demo@taskmanager.local";
    public string Password { get; set; } = "Demo123!";
    public string DisplayName { get; set; } = "Demo User";
}
