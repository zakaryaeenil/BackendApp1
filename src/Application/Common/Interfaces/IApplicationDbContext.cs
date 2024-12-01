using Microsoft.EntityFrameworkCore.Storage;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    
    DbSet<Operation> Operations { get; }

    DbSet<Commentaire> Commentaires { get; }

    DbSet<Document> Documents { get; }
    DbSet<Historique> Historiques { get; }

    DbSet<Client> Clients { get; }

    DbSet<Dossier> Dossiers { get; }
    DbSet<Facture> Factures { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Begins a database transaction.
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current database transaction.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current database transaction.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

}
