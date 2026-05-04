using TaskManager.Application.Abstractions;
using TaskManager.Application.Contracts;
using TaskManager.Domain;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Services;

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        UserValidation.ValidateRegister(request.Email, request.Password, request.DisplayName);
        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await users.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            throw new ConflictException("Email is already registered.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            DisplayName = request.DisplayName.Trim()
        };

        await users.CreateAsync(user, cancellationToken).ConfigureAwait(false);
        var token = tokenGenerator.CreateToken(user);
        return new AuthResponse(token, user.Id, user.Email, user.DisplayName);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        UserValidation.ValidateLogin(request.Email, request.Password);
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new ValidationException("Invalid email or password.");
        }

        var token = tokenGenerator.CreateToken(user);
        return new AuthResponse(token, user.Id, user.Email, user.DisplayName);
    }
}
