using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Application.Common.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace NejPortalBackend.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<IdentityService> _logger;
    private readonly IConfiguration _configuration;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
        IAuthorizationService authorizationService,
        ILogger<IdentityService> logger,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _authorizationService = authorizationService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user?.UserName;
    }
    public async Task<string?> GetUserNameByCodeClientAsync(string codeRef)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.CodeRef == codeRef);

        return user?.UserName;
    }
    public async Task<string?> GetCodeClientAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user?.CodeRef;
    }
    public async Task<string?> GetUserIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user?.UserName;
    }
    public async Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName,
        };

        var result = await _userManager.CreateAsync(user, password);

        return (result.ToApplicationResult(), user.Id);
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

        var result = await _authorizationService.AuthorizeAsync(principal, policyName);

        return result.Succeeded;
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    public async Task<Result> DeleteUserAsync(ApplicationUser user)
    {
        var result = await _userManager.DeleteAsync(user);

        return result.ToApplicationResult();
    }
    public async Task<IReadOnlyCollection<UserDto>> GetAllUsersInRoleAsync(string role)
    {
        // Fetch users assigned to the Client role
        var usersInRole = await _userManager.GetUsersInRoleAsync(role);
        var isClient = role == Roles.Client;
        // Return an empty collection if no users are found
        if (usersInRole == null || !usersInRole.Any())
            return [];

        // Map to DTOs
        return usersInRole.Select(user => new UserDto
        {
            Id = user.Id,
            CodeRef = isClient ? user.CodeRef : null,
            UserName = user.UserName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            HasAccess = user.HasAccess
        }).ToList().AsReadOnly();
    }

    public async Task<int> GetUserInRoleCount(string role)
    {
        // Assuming you have a role called "Agent"
        var agents = await _userManager.GetUsersInRoleAsync(role);

        return agents.Count;
    }

    //ALL
    public async Task<LoginResponse> LoginAsync(string email, string password, string appIdentifier)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            _logger.LogWarning("Login attempt failed: User not found for email {Email}", email);
            return new LoginResponse { Error = "User not found." };
        }

        // Check if the user has access
        if (!user.HasAccess)
        {
            _logger.LogWarning("Login attempt failed: User does not have access for email {Email}", email);
            return new LoginResponse { Error = "Your account does not have access." };
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            _logger.LogWarning("Login attempt failed: Incorrect password for email {Email}", email);
            return new LoginResponse { Error = "Incorrect password." };
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login attempt failed: Account locked for email {Email}", email);
            return new LoginResponse { Error = "Account is locked." };
        }

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            _logger.LogWarning("Login attempt failed: Email not confirmed for email {Email}", email);
            return new LoginResponse { Error = "Email is not confirmed." };
        }

        var roles = await _userManager.GetRolesAsync(user);

        // Determine audience and role validation based on appIdentifier
        string? audience = null;
        if (appIdentifier == FrontAppsConfig.ClientApp)
        {
            audience = _configuration["JwtSettings:Audience2"];
            if (!roles.Contains(Roles.Client))
            {
                _logger.LogWarning("Login attempt failed: User {Email} does not have Client role for ClientApp.", email);
                return new LoginResponse { Error = "User does not have the Client role." };
            }

            var isCodeClientValid = await IsInRoleAsync(user.Id, Roles.Client);
            if (!isCodeClientValid)
            {
                _logger.LogWarning("Login attempt failed: Invalid CodeClient for email {Email}", email);
                return new LoginResponse { Error = "Invalid Code Client." };
            }
        }
        else if (appIdentifier == FrontAppsConfig.EntrepriseApp)
        {
            audience = _configuration["JwtSettings:Audience1"];
            if (!roles.Any(role => role == Roles.Administrator || role == Roles.Agent))
            {
                _logger.LogWarning("Login attempt failed: User {Email} does not have the required role for Admin/Agent.", email);
                return new LoginResponse { Error = "User does not have the required role for this application." };
            }
        }
        else
        {
            _logger.LogWarning("Login attempt failed: User {Email} does not have the required role.", email);
            return new LoginResponse { Error = "User does not requested from the apps that communicate with this application." };

        }

        // Generate JWT token
        var token = GenerateJwtToken(user, roles, audience);

        // Generate and save refresh token
        var refreshToken = GenerateRefreshToken();
        await SaveRefreshTokenAsync(user, refreshToken);

        return new LoginResponse
        {
            TokenType = "Bearer",
            AccessToken = token,
            ExpiresIn = (int)new JwtSecurityTokenHandler().ReadToken(token).ValidTo.Subtract(DateTime.UtcNow).TotalSeconds,
            RefreshToken = refreshToken
        };
    }
    public async Task<LoginResponse> RefreshTokenAsync(string refreshToken, string appIdentifier)
    {
        // Find the user with a matching refresh token and a valid expiration time
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.RefreshTokenExpiryTime > DateTime.UtcNow);

        if (user == null)
        {
            _logger.LogWarning("Refresh token request failed: Invalid or expired refresh token. AppIdentifier: {AppIdentifier}", appIdentifier);
            return new LoginResponse { Error = "Invalid or expired refresh token." };
        }

        // Get the user's roles
        var roles = await _userManager.GetRolesAsync(user);

        // Determine the audience and validate the role
        string? audience;
        if (appIdentifier == FrontAppsConfig.ClientApp)
        {
            audience = _configuration["JwtSettings:Audience2"];
            if (!roles.Contains(Roles.Client))
            {
                _logger.LogWarning("Refresh token request failed: User does not have the Client role. UserId: {UserId}, AppIdentifier: {AppIdentifier}", user.Id, appIdentifier);
                return new LoginResponse { Error = "User does not have the Client role." };
            }
        }
        else if (appIdentifier == FrontAppsConfig.EntrepriseApp) // Default to Admin/Agent
        {
            audience = _configuration["JwtSettings:Audience1"];
            if (!roles.Any(role => role == Roles.Administrator || role == Roles.Agent))
            {
                _logger.LogWarning("Refresh token request failed: User does not have a valid role for Admin/Agent. UserId: {UserId}, AppIdentifier: {AppIdentifier}", user.Id, appIdentifier);
                return new LoginResponse { Error = "User does not have a valid role for this application." };
            }
        }
        else
        {
            _logger.LogWarning("Refresh token request failed: User does not have a valid appIdentifier. UserId: {UserId}, AppIdentifier: {AppIdentifier}", user.Id, appIdentifier);
            return new LoginResponse { Error = "User does not have a valid appIdentifier for this application." };

        }

        // Generate new JWT token and refresh token
        var newAccessToken = GenerateJwtToken(user, roles, audience);
        var newRefreshToken = GenerateRefreshToken();

        // Update the user's refresh token and expiration time
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Adjust expiration as needed
        await _userManager.UpdateAsync(user);

        // Return the new tokens
        return new LoginResponse
        {
            TokenType = "Bearer",
            AccessToken = newAccessToken,
            ExpiresIn = (int)new JwtSecurityTokenHandler().ReadToken(newAccessToken).ValidTo.Subtract(DateTime.UtcNow).TotalSeconds,
            RefreshToken = newRefreshToken
        };
    }
    private static string GenerateRefreshToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[32];
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
    private string GenerateJwtToken(ApplicationUser user, IEnumerable<string> roles, string? audience)
    {
        // Ensure required configurations are present
        var secretKey = _configuration["JwtSettings:SecretKey"];
        if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(audience))
            throw new InvalidOperationException("JWT settings are not properly configured.");

        // Security key and signing credentials
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims for the token
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
        new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
        new Claim(ClaimTypes.NameIdentifier, user.Id)
    };

        // Add roles to claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Create the token descriptor
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(12), // Adjust token expiry as needed
            SigningCredentials = credentials,
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = audience
        };

        // Generate the token
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        _logger.LogInformation("JWT token generated for user {Email} with roles {Roles}", user.Email, string.Join(", ", roles));
        return tokenHandler.WriteToken(token);
    }
    private async Task SaveRefreshTokenAsync(ApplicationUser user, string refreshToken)
    {
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Example: 7-day expiry
        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            _logger.LogError("Failed to save refresh token for user {UserId}: {Errors}", user.Id, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            throw new InvalidOperationException("Failed to save refresh token.");
        }

        _logger.LogInformation("Refresh token saved for user {UserId}.", user.Id);
    }



}
