using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Infrastructure.Data;
using NejPortalBackend.Infrastructure.Data.Interceptors;
using NejPortalBackend.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using NejPortalBackend.Infrastructure.Configs;
using NejPortalBackend.Infrastructure.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        Guard.Against.Null(connectionString, message: "Connection string 'DefaultConnection' not found.");

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ApplicationDbContextInitialiser>();
        services.AddAuthentication(options =>
                                    {
                                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                                    })
                                            .AddJwtBearer(options =>
                                     {
                                         var secretKey = configuration["JwtSettings:SecretKey"];
                                         if (string.IsNullOrEmpty(secretKey))
                                             throw new InvalidOperationException("JWT secret key is not configured.");

                                         var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

                                         options.TokenValidationParameters = new TokenValidationParameters
                                         {
                                             ValidateIssuer = true,
                                             ValidateAudience = true,
                                             ValidateLifetime = true,
                                             ValidateIssuerSigningKey = true,
                                             ValidIssuer = configuration["JwtSettings:Issuer"],
                                             ValidAudiences = new[] { configuration["JwtSettings:Audience1"], configuration["JwtSettings:Audience2"] },
                                             IssuerSigningKey = signingKey,
                                             ClockSkew = TimeSpan.FromDays(15)
                                         };

                                         // Handle SignalR token from query string
                                         options.Events = new JwtBearerEvents
                                         {
                                             OnMessageReceived = context =>
                                             {
                                                 var accessToken = context.Request.Query["access_token"];
                                                 var path = context.HttpContext.Request.Path;

                                                 // Attach token if accessing SignalR hub
                                                 if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
                                                 {
                                                     context.Token = accessToken;
                                                 }

                                                 return Task.CompletedTask;
                                             }
                                         };
                                     });



        services.AddAuthorizationBuilder();

        services
            .AddIdentityCore<ApplicationUser>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
        })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints();

        services.AddSingleton(TimeProvider.System);
        services.AddTransient<IIdentityService, IdentityService>();
        services.AddTransient<IFileService, FileService>();

        // Bind email settings
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<FrontAppURLs>(configuration.GetSection("FrontAppURLs"));
        // Register email service
        services.AddTransient<IEmailService, EmailService>();

        services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator)));
        
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigins", policyBuilder =>
            {
                policyBuilder.WithOrigins("http://localhost:4200","http://localhost:4300") // Add the Angular app's URL here
                             .AllowAnyHeader()
                             .AllowAnyMethod()
                             .AllowCredentials();
            });
        });
        return services;
    }
}
