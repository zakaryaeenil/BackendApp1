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
    public required int OperationPrioriteId { get; init; }

    public required bool TR { get; init; } = false;
    public required bool DEBOURS { get; init; } = false;
    public required bool CONFIRMATION_DEDOUANEMENT { get; init; } = false;

    public string? Commentaire { get; init; }
    public IEnumerable<IFormFile>? Files { get; init; }
}

public class ClientCreateOperationCommandValidator : AbstractValidator<ClientCreateOperationCommand>
{
    public ClientCreateOperationCommandValidator()
    {
        RuleFor(v => v.TypeOperationId)
               .NotNull().WithMessage("Type is required.");
        RuleFor(v => v.OperationPrioriteId)
               .NotNull().WithMessage("OperationPrioriteId is required.");
    }
}

public class ClientCreateOperationCommandHandler : IRequestHandler<ClientCreateOperationCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;
    private readonly IFileService _fileService;
    private readonly ILogger<ClientCreateOperationCommandHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;

    public ClientCreateOperationCommandHandler(IEmailService emailService, INotificationService notificationService, IApplicationDbContext context, IUser currentUserService, IIdentityService identityService, IFileService fileService, ILogger<ClientCreateOperationCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _fileService = fileService;
        _logger = logger;
        _emailService = emailService;
        _notificationService = notificationService;
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

            // Validate TypeOperation
            bool isValidOperationPriorite = Enum.IsDefined(typeof(OperationPriorite), request.OperationPrioriteId);
            if (!isValidTypeOperation)
            {
                _logger.LogWarning("Invalid Operation Priorite value: {OperationPrioriteId}", request.OperationPrioriteId);
                throw new InvalidOperationException("Invalid Operation Priorite value.");
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
                OperationPriorite = (OperationPriorite)request.OperationPrioriteId,
                UserId = _currentUserService.Id,
                ReserverPar = null,
                EstReserver = false,
                TR = request.TR,
                DEBOURS = request.DEBOURS,
                CONFIRMATION_DEDOUANEMENT = request.CONFIRMATION_DEDOUANEMENT
            };

            await _context.Operations.AddAsync(operation, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken); // Operation gets an ID here

            _logger.LogDebug("Operation created successfully with Id: {OperationId}", operation.Id);

            // Create and log the historical record for the creation
            var historique = new Historique
            {
                Action = "L'opération numéro : "+ operation.Id+" a été criée par le client "+ clientUsername + ": Operation a été crié avec succès.",
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



            var admins = await _identityService.GetAllUsersInRoleAsync(Roles.Administrator);


            //Notif and mail
            var notificationAdminMessage = "A new operation (ID: " + operation.Id + " ) has been created for"+ clientUsername ;

           
            foreach(var admin in admins)
            {
                if (!string.IsNullOrWhiteSpace(admin.Id))
                    await _notificationService.SendNotificationAsync(admin.Id, notificationAdminMessage, cancellationToken);

                // Send  email
                try
                {
                    if (!string.IsNullOrWhiteSpace(admin.Email_Notif) && !string.IsNullOrWhiteSpace(admin.UserName))
                        await _emailService.SendOperationEmailAsync(admin.Email_Notif, operation.Id, notificationAdminMessage, admin.UserName);
                }
                catch (Exception ex)
                {
                    // Log the error and notify
                    _logger.LogError(ex, "Failed to send create Operation email to {Email}", admin.Email_Notif);

                }

            }

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
