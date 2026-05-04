using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string CreateToken(User user);
}
