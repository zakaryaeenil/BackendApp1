using System.Runtime.InteropServices;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NejPortalBackend.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();

        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // Default roles
        var administratorRole = new IdentityRole(Roles.Administrator);
        var agentRole = new IdentityRole(Roles.Agent);
        var clientRole = new IdentityRole(Roles.Client);

        if (_roleManager.Roles.All(r => r.Name != administratorRole.Name))
        {
            await _roleManager.CreateAsync(administratorRole);
        }
        if (_roleManager.Roles.All(r => r.Name != agentRole.Name))
        {
            await _roleManager.CreateAsync(agentRole);
        }

        if (_roleManager.Roles.All(r => r.Name != clientRole.Name))
        {
            await _roleManager.CreateAsync(clientRole);
        }

        // Default users
        var administrator = new ApplicationUser { UserName = "administrator@localhost", Email = "administrator@localhost" };

        // Default users
        var client = new ApplicationUser { UserName = "client", Email = "client@localhost", CodeRef = "112111" };
        // Default users
        var agent = new ApplicationUser { UserName = "agent", Email = "agent@localhost" };

        if (_userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            await _userManager.CreateAsync(administrator, "Administrator1!");
            if (!string.IsNullOrWhiteSpace(administratorRole.Name))
            {
                await _userManager.AddToRolesAsync(administrator, new[] { administratorRole.Name });
            }
        }
        if (_userManager.Users.All(u => u.UserName != client.UserName))
        {
            await _userManager.CreateAsync(client, "Administrator1!");
            if (!string.IsNullOrWhiteSpace(clientRole.Name))
            {
                await _userManager.AddToRolesAsync(client, new[] { clientRole.Name });
            }
        }
        if (_userManager.Users.All(u => u.UserName != agent.UserName))
        {
            await _userManager.CreateAsync(agent, "Administrator1!");
            if (!string.IsNullOrWhiteSpace(agentRole.Name))
            {
                await _userManager.AddToRolesAsync(agent, new[] { agentRole.Name });
            }
        }
    }
}
