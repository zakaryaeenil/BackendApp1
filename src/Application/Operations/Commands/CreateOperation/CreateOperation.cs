using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Operations.Commands.CreateOperation;

[Authorize(Roles = Roles.AdminAndAgent)]
public record CreateOperationCommand : IRequest<int>
{
    public required string ClientId { get; init; }
    public string? AgentId { get; init; }
    public int TypeOperationId { get; init; }

    public required int OperationPrioriteId { get; init; }

    public required bool TR { get; init; } = false;
    public required bool DEBOURS { get; init; } = false;
    public required bool CONFIRMATION_DEDOUANEMENT { get; init; } = false;

    public string? Commentaire { get; init; }
    public IEnumerable<IFormFile>? Files { get; init; }

}

public class CreateOperationCommandValidator : AbstractValidator<CreateOperationCommand>
{
    public CreateOperationCommandValidator()
    {
        RuleFor(v => v.TypeOperationId)
                .NotNull().WithMessage("Type is required.");
        RuleFor(v => v.ClientId).NotEmpty()
                .NotNull().WithMessage("Client Idantifiant required.");
        RuleFor(v => v.OperationPrioriteId)
              .NotNull().WithMessage("OperationPrioriteId is required.");
    }
}

public class CreateOperationCommandHandler : IRequestHandler<CreateOperationCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;
    private readonly IFileService _fileService;
    private readonly ILogger<CreateOperationCommandHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;

    public CreateOperationCommandHandler(IEmailService emailService,IApplicationDbContext context, IUser currentUserService, IIdentityService identityService, IFileService fileService, ILogger<CreateOperationCommandHandler> logger, INotificationService notificationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _fileService = fileService;
        _logger = logger;
        _notificationService = notificationService;
        _emailService = emailService;
    }

    public async Task<int> Handle(CreateOperationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to process CreateOperationCommand for user: {ClientId}", request.ClientId);

        // Begin a transaction
        using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            if (string.IsNullOrWhiteSpace(_currentUserService.Id))
                throw new UnauthorizedAccessException();

            // Validate TypeOperation
            bool isValidTypeOperation = Enum.IsDefined(typeof(TypeOperation), request.TypeOperationId);


            if (!isValidTypeOperation)
            {
                _logger.LogWarning("Invalid TypeOperation value: {TypeOperationId}", request.TypeOperationId);
                throw new InvalidOperationException("Invalid TypeOperation value.");
            }

            // Validate TypeOperation
            bool isValidOperationPriorite = Enum.IsDefined(typeof(OperationPriorite), request.OperationPrioriteId);
            if (!isValidOperationPriorite)
            {
                _logger.LogWarning("Invalid Operation Priorite value: {OperationPrioriteId}", request.OperationPrioriteId);
                throw new InvalidOperationException("Invalid Operation Priorite value.");
            }

            // Validate client role
            if (string.IsNullOrWhiteSpace(request.ClientId) || !await _identityService.IsInRoleAsync(request.ClientId, Roles.Client))
            {
                _logger.LogWarning("Invalid Client Id value: {ClientId}", request.ClientId);
                throw new InvalidOperationException("Invalid Client Id value.");
            }
            // Validate agent role
            if (!string.IsNullOrWhiteSpace(request.AgentId) && !await _identityService.IsInRoleAsync(request.AgentId, Roles.Agent))
            {
                _logger.LogWarning("Invalid Agent Id value: {AgentId}", request.AgentId);
                throw new InvalidOperationException("Invalid Agent Id value.");
            }

            var clientUsername = await _identityService.GetUserNameAsync(request.ClientId);
            if (string.IsNullOrWhiteSpace(clientUsername))
            {
                _logger.LogWarning("Invalid Client UserName value: {UserName}", _currentUserService.Id);
                throw new InvalidOperationException("Invalid Client UserName value.");
            }

            // Create and save the Operation
            var operation = new Operation
            {
                OperationPriorite = (OperationPriorite)request.OperationPrioriteId,
                TypeOperation = (TypeOperation)request.TypeOperationId,
                EtatOperation = EtatOperation.depotDossier,
                UserId = request.ClientId,
                ReserverPar = null,
                EstReserver = false,
                TR = request.TR,
                DEBOURS = request.DEBOURS,
                CONFIRMATION_DEDOUANEMENT = request.CONFIRMATION_DEDOUANEMENT
            };
            if (!string.IsNullOrWhiteSpace(request.AgentId) && await _identityService.IsInRoleAsync(request.AgentId, Roles.Agent))
            {
                operation.ReserverPar = request.AgentId;
                operation.EstReserver = true;
            }

            await _context.Operations.AddAsync(operation, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken); // Operation gets an ID here

            var userName = await _identityService.GetUserNameAsync(_currentUserService.Id);
            var userNameClient = await _identityService.GetUserNameAsync(request.ClientId);
            // Create and log the historical record for the creation
            var historique = new Historique
            {
                Action = $"L'opération numéro : "+ operation.Id +" a été criée par : " + userName + " pour le client "+ userNameClient + " : Operation a été criée avec succès.",
                UserId = _currentUserService.Id,
                OperationId = operation.Id
            };

            // Add the historique record to the database
            await _context.Historiques.AddAsync(historique, cancellationToken);
          
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
            await _context.SaveChangesAsync(cancellationToken); // Operation gets an ID here
            // Commit the transaction
            await transaction.CommitAsync(cancellationToken);

            //Notif and mail
            if(!string.IsNullOrWhiteSpace(operation.ReserverPar) && operation.ReserverPar != _currentUserService.Id)
            {
                // Send notification
                var notificationAgentMessage = "A new operation (ID: " + operation.Id + " ) has been created and afficted to you.";
                await _notificationService.SendNotificationAsync(operation.ReserverPar, notificationAgentMessage, cancellationToken);


                var reserverParUserName = await _identityService.GetUserNameAsync(operation.ReserverPar);
                var reserverParEmail = await _identityService.GetUserEmailNotifAsync(operation.ReserverPar);


                // Send the reset password link to the user via email
                try
                {
                    if(!string.IsNullOrWhiteSpace(reserverParUserName) && !string.IsNullOrWhiteSpace(reserverParEmail))
                    await _emailService.SendOperationEmailAsync(reserverParEmail, operation.Id, notificationAgentMessage, reserverParUserName);
                }
                catch (Exception ex)
                {
                    // Log the error and notify
                    _logger.LogError(ex, "Failed to send create Operation email to {Email}", reserverParUserName);

                }
            }

            var notificationMessage = "A new operation (ID: "+operation.Id+" ) has been created for you.";
            await _notificationService.SendNotificationAsync(request.ClientId, notificationMessage, cancellationToken);

            var clientUserName = await _identityService.GetUserNameAsync(request.ClientId);
            var clientEmail = await _identityService.GetUserEmailNotifAsync(request.ClientId);


            // Send email to the user via email
            try
            {
                if (!string.IsNullOrWhiteSpace(clientUserName) && !string.IsNullOrWhiteSpace(clientEmail))
                    await _emailService.SendOperationEmailAsync(clientEmail, operation.Id, notificationMessage, clientUserName);
            }
            catch (Exception ex)
            {
                // Log the error and notify
                _logger.LogError(ex, "Failed to send create Operation email to {Email}", clientEmail);

            }




            _logger.LogInformation("Operation created successfully with Id: {OperationId}", operation.Id);

            return operation.Id;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError("Rollback  CreateOperationCommand processing.");
            _logger.LogError(ex, "An error occurred during CreateOperationCommand processing.");

            throw ex switch
            {
                NotFoundException or InvalidOperationException or UnauthorizedAccessException => ex,
                _ => new ApplicationException(ex.Message, ex),
            };
        }
    }
}
