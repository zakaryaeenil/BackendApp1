using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;

namespace NejPortalBackend.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);
    Task<string?> GetCodeClientAsync(string userId);
    Task<string?> GetUserNameByCodeClientAsync(string codeRef);
    

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password);

    Task<Result> DeleteUserAsync(string userId);
    Task<IReadOnlyCollection<UserDto>> GetAllUsersInRoleAsync(string role);
    Task<int> GetUserInRoleCount(string role);

    //Auth
    Task<LoginResponse> LoginAsync(string email, string password, string appIdentifier);
    Task<LoginResponse> RefreshTokenAsync(string refreshToken, string appIdentifier);
}
