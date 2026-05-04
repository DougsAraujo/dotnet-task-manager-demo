namespace TaskManager.Application.Abstractions;

public interface IDevDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
