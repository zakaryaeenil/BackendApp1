using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;

namespace NejPortalBackend.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);
    Task<int?> GetTypeOperationAsync(string userId);
    Task<string?> GetCodeClientAsync(string userId);
    Task<string?> GetUserNameByCodeClientAsync(string codeRef);
    Task<string?> GetUserEmailNotifAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<ICollection<UserDto>> GetAllUsersInRoleAsync(string role);
    Task<int> GetUserInRoleCount(string role);
    Task<UserDto?> GetUserByIdAsync(string? id, CancellationToken cancellationToken = default);

    //Auth
    Task<LoginResponse> LoginAsync(string email, string password, string appIdentifier);
    Task<LoginResponse> RefreshTokenAsync(string refreshToken, string appIdentifier);
    Task<Result> ResetPasswordAsync(string email,string token,string newPassword,CancellationToken cancellationToken = default);
    Task<Result> ForgotPasswordAsync(string email);

    Task<(Result Result, string UserId)> CreateUserAsync(string userName,string password,string email,int? typeOperation,string? phoneNumber,string? codeUser,string notif_email,CancellationToken cancellationToken = default);
    Task<(Result Result, string UserId)> UpdateUserAsync(string userId, string userName, string email, string emailNotif, string? phoneNumber, string? codeUser, bool hasAccess);
    Task<(Result Result, string UserId)> ChangePasswordAsync(string userId, string oldPassword, string newPassword, string confirmNewPassword);
}
