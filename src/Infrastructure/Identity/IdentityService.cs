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
using Microsoft.Extensions.Options;
using NejPortalBackend.Infrastructure.Configs;
using System.Data;

namespace NejPortalBackend.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<IdentityService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly FrontAppURLs _frontAppURLs;
    private readonly IApplicationDbContext _context;

    private readonly SignInManager<ApplicationUser> _signInManager;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
        IAuthorizationService authorizationService,
        ILogger<IdentityService> logger,
        IConfiguration configuration,
    IEmailService emailService,
    IOptions<FrontAppURLs> options,
    IApplicationDbContext context,
    SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _authorizationService = authorizationService;
        _logger = logger;
        _configuration = configuration;
        _emailService = emailService;
        _frontAppURLs = options.Value;
        _context = context;
        _signInManager = signInManager;
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
    public async Task<(Result Result, string UserId)> CreateUserAsync(
    string userName,
    string password,
    string email,
    string? phoneNumber,
    string? codeUser,
    string notif_email,
    CancellationToken cancellationToken = default)
    {
        // Check if a user with the same email already exists
        var existingUserWithEmail = await _userManager.FindByEmailAsync(email);
        if (existingUserWithEmail != null)
        {
            return (Result.Failure(new List<string> { "User with this email already exists." }), string.Empty);
        }

        // Check if a user with the same username already exists
        var existingUserWithUserName = await _userManager.FindByNameAsync(userName); // Fixed: Using FindByNameAsync for username
        if (existingUserWithUserName != null)
        {
            return (Result.Failure(new List<string> { "User with this username already exists." }), string.Empty);
        }

        // Initialize a new Identity user
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            PhoneNumber = phoneNumber,
            EmailConfirmed = true,
            LockoutEnabled = true,
            Email_Notif = notif_email,
            HasAccess = true
        };
       
        bool codeUsed = _userManager.Users.Where(u => u.Id != user.Id && u.CodeRef == codeUser).Count() >= 1 ? true : false;
        bool codeExist = _context.Clients.Where(u => u.CodeClient == codeUser).Count() >= 1 ? true : false;

        if (!string.IsNullOrEmpty(codeUser) && codeUsed)
        {
            return (Result.Failure(new List<string> { "A user with this user code already exists." }), string.Empty);
        }
        if (!string.IsNullOrEmpty(codeUser) && !codeExist)
        {
            return (Result.Failure(new List<string> { "No client was identified with this user code." }), string.Empty);
        }
        else if (!string.IsNullOrEmpty(codeUser))
        {
            user.CodeRef = codeUser;
        }
        // Create the user
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return (Result.Failure(result.Errors.Select(e => e.Description).ToList()), string.Empty);
        }

        // Determine role based on codeUser
        var role = string.IsNullOrEmpty(codeUser) ? Roles.Agent : Roles.Client;

        // Add user to the appropriate role
        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            // If role assignment fails, remove the user
            await _userManager.DeleteAsync(user);
            return (Result.Failure(roleResult.Errors.Select(e => e.Description).ToList()), string.Empty);
        }

        // Generate a password reset token for secure access
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        var applicationBaseUrl = role == Roles.Client ? _frontAppURLs.ClientAppURL : _frontAppURLs.EntrepriseAppURL;
        // Prepare the reset password link
        var resetPasswordLink = $"{applicationBaseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(email)}";

        // Send a welcome email with the temporary password and reset link
        try
        {
            await _emailService.SendWelcomeEmailAsync(email, password, resetPasswordLink, user.UserName); // Adjust method as needed

        }
        catch (Exception ex)
        {
            // Log the error and notify
            _logger.LogError(ex, "Failed to send welcome email to {Email}", email);
        }

        // Return success with the user ID
        return (Result.Success(), user.Id);
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


    public async Task<IReadOnlyCollection<UserDto>> GetAllUsersInRoleAsync(string role)
    {
        // Fetch users assigned to the Client role
        var usersInRole = await _userManager.GetUsersInRoleAsync(role);
        var isClient = role == Roles.Client;
        // Return an empty collection if no users are found
        if (usersInRole == null || !usersInRole.Any())
            return Array.Empty<UserDto>();

        // Map to DTOs
        return usersInRole.Select(user => new UserDto
        {
            Id = user.Id,
            CodeRef = isClient ? user.CodeRef : null,
            UserName = user.UserName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            Role= isClient ? Roles.Client : Roles.Agent,
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
    public async Task<Result> ResetPasswordAsync(string email,string token,string newPassword,CancellationToken cancellationToken = default)
    {
        // Check if the user exists
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Result.Failure(new List<string> { "User not found with the provided email." });
        }

        // Reset the password using the provided token
        var resetPasswordResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!resetPasswordResult.Succeeded)
        {
            var errors = resetPasswordResult.Errors.Select(e => e.Description).ToList();

            // Check if the error is due to token expiration
            if (errors.Contains("Invalid token."))
            {
                return Result.Failure(new List<string> { "The reset password link has expired. Please request a new one." });
            }

            return Result.Failure(errors);
        }

        return Result.Success();
    }
    public async Task<Result> ForgotPasswordAsync(string email)
    {
        // Find user by email
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Result.Failure(new List<string> { "User not found with the provided email." });
        }
        if (user.UserName == null)
        {
            return Result.Failure(new List<string> { "User not found with the provided email." });
        }
        // Generate a password reset token
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        if (string.IsNullOrEmpty(token))
        {
            return Result.Failure(new List<string> { "Failed to generate password reset token." });
        }
        var applicationBaseUrl = await IsInRoleAsync(user.Id, Roles.Client) ? _frontAppURLs.ClientAppURL : _frontAppURLs.EntrepriseAppURL;
        // Prepare the reset password link
        var resetPasswordLink = $"{applicationBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";

        // Send the reset password link to the user via email
        try
        {
            await _emailService.SendResetPasswordEmailAsync(email, resetPasswordLink,user.UserName);
        }
        catch (Exception ex)
        {
            // Log the error and notify
            _logger.LogError(ex, "Failed to send forgot password email to {Email}", email);
            return Result.Failure(new List<string> { "Failed to send reset password email. Please try again later." });
        }

        return Result.Success();
    }


    public async Task<UserDto?> GetUserByIdAsync(string? id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
          
        return user is null
            ? null
            : new UserDto
            {
                Id = user.Id,
                CodeRef = user.CodeRef,
                Nom = user.Nom,
                Prenom = user.Prenom,
                Email = user.Email,
                Email_Notif = user.Email_Notif,
                HasAccess = user.HasAccess,
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName,
                EmailConfirmed = user.EmailConfirmed,
            };
    }

    public async Task<(Result Result, string UserId)> UpdateUserAsync(string userId, string userName, string email, string emailNotif, string? phoneNumber, string? codeUser, bool hasAccess)
    {
        // Find the user by ID
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (Result.Failure(new List<string> { "User not found." }), string.Empty);
        }

        // Check if a user with the same email already exists (but not the current user)
        var existingUserWithEmail = await _userManager.FindByEmailAsync(email);
        if (existingUserWithEmail != null && existingUserWithEmail.Id != userId)
        {
            return (Result.Failure(new List<string> { "A user with this email already exists." }), string.Empty);
        }

        // Check if a user with the same username already exists (but not the current user)
        var existingUserWithUserName = await _userManager.FindByNameAsync(userName);
        if (existingUserWithUserName != null && existingUserWithUserName.Id != userId)
        {
            return (Result.Failure(new List<string> { "A user with this username already exists." }), string.Empty);
        }

        // Update user properties
        user.UserName = userName;
        user.Email = email;
        user.PhoneNumber = phoneNumber;
        user.LockoutEnabled = true;
        user.HasAccess = hasAccess;
        user.Email_Notif = emailNotif;
        var isClient = await IsInRoleAsync(user.Id, Roles.Client) ;
        bool codeUsed = _userManager.Users.Where(u => u.Id != user.Id && u.CodeRef == codeUser).Count() >= 1 ? true : false;
        bool codeExist = _context.Clients.Where(u =>  u.CodeClient == codeUser).Count() >= 1 ? true : false;
        // Save changes to the user

        if (isClient && codeUsed)
        {
            return (Result.Failure(new List<string> { "A user with this user code already exists." }), string.Empty);
        }
        if (isClient && !codeExist)
        {
            return (Result.Failure(new List<string> { "No client was identified with this user code." }), string.Empty);
        }
        else if (isClient)
        {
            user.CodeRef = codeUser;
        }
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return (Result.Failure(updateResult.Errors.Select(e => e.Description).ToList()), string.Empty);
        }

        return (Result.Success(), user.Id);
    }
    public async Task<(Result Result, string UserId)> ChangePasswordAsync(string userId, string oldPassword, string newPassword, string confirmNewPassword)
    {
        // Validate new password and confirmation match
        if (newPassword != confirmNewPassword)
        {
            return (Result.Failure(new List<string> { "New password and confirm password do not match." }), string.Empty);
        }

        // Find the user by their ID
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (Result.Failure(new List<string> { "User not found." }),string.Empty);
        }

        // Attempt to change the user's password
        var changePasswordResult = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);

        if (changePasswordResult.Succeeded)
        {
            // Refresh the sign-in token to ensure a new password invalidates old JWTs
            await _signInManager.RefreshSignInAsync(user);

            return (Result.Success(),string.Empty);
        }
        else
        {
            // Return the errors from the Identity framework
            var errors = changePasswordResult.Errors.Select(e => e.Description).ToList();
            return (Result.Failure(errors),string.Empty);
        }
    }

}
