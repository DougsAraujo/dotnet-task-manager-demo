using Microsoft.AspNetCore.Identity;
using TaskManager.Application.Abstractions;

namespace TaskManager.Infrastructure.Auth;

public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(HasherSubject.Instance, password);

    public bool Verify(string password, string passwordHash) =>
        _hasher.VerifyHashedPassword(HasherSubject.Instance, passwordHash, password) != PasswordVerificationResult.Failed;

    private sealed class HasherSubject
    {
        public static readonly object Instance = new();
    }
}
