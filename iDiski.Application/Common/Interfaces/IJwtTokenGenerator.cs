using iDiski.Domain.Entities;
using iDiski.Domain.Enums;

namespace iDiski.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, Role[] roles);
}
