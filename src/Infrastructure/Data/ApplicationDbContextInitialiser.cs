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
        await _context.Notifications.ExecuteDeleteAsync();
        await _context.Operations.ExecuteDeleteAsync();
        await _context.Dossiers.ExecuteDeleteAsync();
        await _context.Clients.ExecuteDeleteAsync();




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
        await _userManager.Users.ExecuteDeleteAsync();
        // Default users
        var sadministrator = new ApplicationUser { UserName = "superAdmin", Email = "elnilzakarya@gmail.com",EmailConfirmed = true,Email_Notif = "elnilzakarya@gmail.com" };


        if (_userManager.Users.All(u => u.UserName != sadministrator.UserName))
        {
            await _userManager.CreateAsync(sadministrator, "Administrator1!");
            if (!string.IsNullOrWhiteSpace(administratorRole.Name))
            {
                await _userManager.AddToRolesAsync(sadministrator, new[] { administratorRole.Name });
            }
        }
        // Default users
        var administrator = new ApplicationUser { UserName = "ImportAdmin", Email = "admin2@nejtrans.com", EmailConfirmed = true, Email_Notif = "admin2@nejtrans.com",TypeOperation = Domain.Enums.TypeOperation.Import };


        if (_userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            await _userManager.CreateAsync(administrator, "Administrator1!");
            if (!string.IsNullOrWhiteSpace(administratorRole.Name))
            {
                await _userManager.AddToRolesAsync(administrator, new[] { administratorRole.Name });
            }
        }
        // Default users
        var qadministrator = new ApplicationUser { UserName = "ExportAdmin", Email = "anas.jamali0507@gmail.com", EmailConfirmed = true, Email_Notif = "anas.jamali0507@gmail.com\"", TypeOperation = Domain.Enums.TypeOperation.Export };


        if (_userManager.Users.All(u => u.UserName != qadministrator.UserName))
        {
            await _userManager.CreateAsync(qadministrator, "Administrator1!");
            if (!string.IsNullOrWhiteSpace(administratorRole.Name))
            {
                await _userManager.AddToRolesAsync(qadministrator, new[] { administratorRole.Name });
            }
        }
        Client client = new Client { CodeClient = "112111", Nom = "CAPGEMINI" };
        Client client2 = new Client { CodeClient = "234091", Nom = "ATOS" };

        Dossier dossier = new Dossier { CodeClient = "112111", CodeDossier = "111111"};
        Dossier dossier2 = new Dossier { CodeClient = "112111", CodeDossier = "150112" };
        Dossier dossier3 = new Dossier { CodeClient = "112111", CodeDossier = "333411" };

        await _context.Dossiers.AddAsync(dossier);
        await _context.Dossiers.AddAsync(dossier2);
        await _context.Dossiers.AddAsync(dossier3);

        await _context.Clients.AddAsync(client);
        await _context.Clients.AddAsync(client2);

        await _context.SaveChangesAsync();
    }
}
