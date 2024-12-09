using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Operations.Commands.ClientCreateOperation;

[Authorize(Roles = Roles.Client)]
public record ClientCreateOperationCommand : IRequest<int>
{
    public required int TypeOperationId { get; init; }
    public string? Commentaire { get; init; }
    public IEnumerable<IFormFile>? Files { get; init; }
}

public class ClientCreateOperationCommandValidator : AbstractValidator<ClientCreateOperationCommand>
{
    public ClientCreateOperationCommandValidator()
    {
        RuleFor(v => v.TypeOperationId)
               .NotNull().WithMessage("Type is required.");
    }
}

public class ClientCreateOperationCommandHandler : IRequestHandler<ClientCreateOperationCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;
    private readonly IFileService _fileService;
    private readonly ILogger<ClientCreateOperationCommandHandler> _logger;

    public ClientCreateOperationCommandHandler(IApplicationDbContext context, IUser currentUserService, IIdentityService identityService, IFileService fileService, ILogger<ClientCreateOperationCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _fileService = fileService;
        _logger = logger;
    }

    public async Task<int> Handle(ClientCreateOperationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to process ClientCreateOperationCommand for user: {UserId}", _currentUserService.Id);

        // Begin a transaction
        using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            if (string.IsNullOrWhiteSpace(_currentUserService.Id))
                throw new UnauthorizedAccessException();
            
            // Validate client role
            if (!await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Client))
            {
                _logger.LogWarning("Invalid Client Id value: {Id}", _currentUserService.Id);
                throw new InvalidOperationException("Invalid Client Id value.");
            }
            // Validate TypeOperation
            bool isValidTypeOperation = Enum.IsDefined(typeof(TypeOperation), request.TypeOperationId);
            if (!isValidTypeOperation)
            {
                _logger.LogWarning("Invalid TypeOperation value: {TypeOperationId}", request.TypeOperationId);
                throw new InvalidOperationException("Invalid TypeOperation value.");
            }
          
            var clientUsername = await _identityService.GetUserNameAsync(_currentUserService.Id);
            if (string.IsNullOrWhiteSpace(clientUsername))
            {
                _logger.LogWarning("Invalid Client UserName value: {UserName}", _currentUserService.Id);
                throw new InvalidOperationException("Invalid Client UserName value.");
            }
            // Create and save the Operation
            var operation = new Operation
            {
                TypeOperation = (TypeOperation)request.TypeOperationId,
                EtatOperation = EtatOperation.depotDossier,
                UserId = _currentUserService.Id,
                ReserverPar = null,
                EstReserver = false
            };

            await _context.Operations.AddAsync(operation, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken); // Operation gets an ID here

            _logger.LogDebug("Operation created successfully with Id: {OperationId}", operation.Id);

            // Create and log the historical record for the creation
            var historique = new Historique
            {
                Action = "L'opération numéro : "+ operation.Id+" a été criée par le client {_currentUserService.Id}: Operation a été crié avec succès.",
                UserId = _currentUserService.Id,
                OperationId = operation.Id
            };

            // Add the historique record to the database
            await _context.Historiques.AddAsync(historique, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken); 
            // Process Files
            if (request.Files?.Count() > 0)
            {
                foreach (var file in request.Files)
                {
                    var fileInfo = await _fileService.Create(file, "documents", clientUsername, operation.Id);

                    var document = new Document
                    {
                        NomDocument = fileInfo.NomDocument,
                        CheminFichier = fileInfo.CheminFichier,
                        TailleFichier = fileInfo.TailleFichier,
                        TypeFichier = fileInfo.TypeFichier,
                        EstAccepte = true,
                        OperationId = operation.Id
                    };

                    await _context.Documents.AddAsync(document, cancellationToken);
                    _logger.LogDebug("File added: {FileName} for OperationId: {OperationId}", fileInfo.NomDocument, operation.Id);
                }
            }

            // Process Commentaire
            if (!string.IsNullOrWhiteSpace(request.Commentaire))
            {
                var commentaire = new Commentaire
                {
                    Message = request.Commentaire,
                    OperationId = operation.Id,
                    UserId = _currentUserService.Id
                };

                await _context.Commentaires.AddAsync(commentaire, cancellationToken);
                _logger.LogDebug("Commentaire added: {Message} for OperationId: {OperationId}", request.Commentaire, operation.Id);
            }

            // Commit the transaction
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Operation created successfully with Id: {OperationId}", operation.Id);

            return operation.Id;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "An error occurred during ClientCreateOperationCommand processing.");

            throw ex switch
            {
                NotFoundException or InvalidOperationException or UnauthorizedAccessException => ex,
                _ => new ApplicationException(ex.Message,ex),
            };
        }
    }

}
