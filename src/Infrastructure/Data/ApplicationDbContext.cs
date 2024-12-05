using System.Reflection;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace NejPortalBackend.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    public DbSet<Operation> Operations => Set<Operation>();
    public DbSet<Commentaire> Commentaires => Set<Commentaire>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Historique> Historiques => Set<Historique>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Dossier> Dossiers => Set<Dossier>();
    public DbSet<Facture> Factures => Set<Facture>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
    /// <summary>
    /// Starts a new database transaction.
    /// </summary>
        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return await Database.BeginTransactionAsync(cancellationToken);
        }

        /// <summary>
        /// Commits the current database transaction.
        /// </summary>
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (Database.CurrentTransaction != null)
            {
                await Database.CommitTransactionAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Rolls back the current database transaction.
        /// </summary>
        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (Database.CurrentTransaction != null)
            {
                await Database.RollbackTransactionAsync(cancellationToken);
            }
        }
}
