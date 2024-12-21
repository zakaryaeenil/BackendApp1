using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Operations.Commands.ClientUpdateOperationDetails;

[Authorize(Roles = Roles.Client)]
public record ClientUpdateOperationDetailsCommand : IRequest
{
    public required int OperationId { get; init; }
    public  required int TypeOperationId { get; init; }

    public string? Bureau { get; init; }
    public string? Regime { get; init; }
}

public class ClientUpdateOperationDetailsCommandValidator : AbstractValidator<ClientUpdateOperationDetailsCommand>
{
    public ClientUpdateOperationDetailsCommandValidator()
    {
        RuleFor(v => v.TypeOperationId)
                .NotNull().WithMessage("Type is required.");
        RuleFor(v => v.OperationId)
                .NotNull().WithMessage("Operation is required.");
    }
}

public class ClientUpdateOperationDetailsCommandHandler : IRequestHandler<ClientUpdateOperationDetailsCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;
    private readonly ILogger<ClientUpdateOperationDetailsCommandHandler> _logger;

    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;

    public ClientUpdateOperationDetailsCommandHandler(IEmailService emailService, INotificationService notificationService, IApplicationDbContext context, IUser currentUserService, IIdentityService identityService, ILogger<ClientUpdateOperationDetailsCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _logger = logger;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    public async Task Handle(ClientUpdateOperationDetailsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to process ClientUpdateOperationDetailsCommandHandler for user: {UserId}", _currentUserService.Id);

        try
        {
            // Ensure the current user is authenticated
            if (string.IsNullOrWhiteSpace(_currentUserService.Id))
            {
                throw new UnauthorizedAccessException();
            }

            // Validate client role
            if (!await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Client))
            {
                _logger.LogWarning("Invalid Client Id value: {Id}", _currentUserService.Id);
                throw new InvalidOperationException("Invalid Client Id value.");
            }
            // Validate TypeOperationId
            bool isValidTypeOperation = Enum.IsDefined(typeof(TypeOperation), request.TypeOperationId);
            if (!isValidTypeOperation)
            {
                _logger.LogWarning("Invalid TypeOperation value: {TypeOperationId}", request.TypeOperationId);
                throw new InvalidOperationException("Invalid TypeOperation value.");
            }

            // Fetch the operation entity by its Id
            var entity = await _context.Operations
                .FindAsync(new object[] { request.OperationId }, cancellationToken)
                ?? throw new NotFoundException(nameof(Operations), request.OperationId.ToString());

            
                // Update TypeOperation, Bureau, and Regime if necessary
                bool isUpdated = false;

                if (entity.TypeOperation != (TypeOperation)request.TypeOperationId && entity.EtatOperation == EtatOperation.depotDossier)
                {
                    entity.TypeOperation = (TypeOperation)request.TypeOperationId;
                    isUpdated = true;
                }
                if (entity.Bureau != request.Bureau)
                {
                    entity.Bureau = request.Bureau;
                    isUpdated = true;
                }
                if (entity.Regime != request.Regime)
                {
                    entity.Regime = request.Regime;
                    isUpdated = true;
                }

                // Only update if there were changes
                if (isUpdated)
                {
                    _context.Operations.Update(entity);

                    // Get the client's username
                    var clientUsername = await _identityService.GetUserNameAsync(_currentUserService.Id);
                    if (!string.IsNullOrWhiteSpace(clientUsername))
                    {
                        // Create and log the historical record for the modification
                        var historique = new Historique
                        {
                            Action = $"L'opération numéro : "+entity.Id+" a été modifiée par le client"+ clientUsername+": Details Operation a été modifié avec succès.",
                            UserId = _currentUserService.Id,
                            OperationId = entity.Id
                        };

                        // Add the historique record to the database
                        await _context.Historiques.AddAsync(historique, cancellationToken);

                        // Save changes to the database
                        await _context.SaveChangesAsync(cancellationToken);
                        //Notif and mail
                        var notificationMessage = "Operation (ID: " + entity.Id + " ) : Details has been Modified by" + clientUsername;
                        if (!string.IsNullOrWhiteSpace(entity.ReserverPar))
                        {
                            // Send notification
                            await _notificationService.SendNotificationAsync(entity.ReserverPar, notificationMessage, cancellationToken);


                            var reserverParUserName = await _identityService.GetUserNameAsync(entity.ReserverPar);
                            var reserverParEmail = await _identityService.GetUserEmailNotifAsync(entity.ReserverPar);


                            // Send the reset password link to the user via email
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(reserverParUserName) && !string.IsNullOrWhiteSpace(reserverParEmail))
                                    await _emailService.SendOperationEmailAsync(reserverParEmail, entity.Id, notificationMessage, reserverParUserName);
                            }
                            catch (Exception ex)
                            {
                                // Log the error and notify
                                _logger.LogError(ex, "Failed to send update Operation email to {Email}", reserverParUserName);

                            }
                        }
                        else
                        {

                            var admins = await _identityService.GetAllUsersInRoleAsync(Roles.Administrator);

                            foreach (var admin in admins)
                            {
                                if (!string.IsNullOrWhiteSpace(admin.Id))
                                    await _notificationService.SendNotificationAsync(admin.Id, notificationMessage, cancellationToken);

                                // Send  email
                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(admin.Email_Notif) && !string.IsNullOrWhiteSpace(admin.UserName))
                                        await _emailService.SendOperationEmailAsync(admin.Email_Notif, entity.Id, notificationMessage, admin.UserName);
                                }
                                catch (Exception ex)
                                {
                                    // Log the error and notify
                                    _logger.LogError(ex, "Failed to send create Operation email to {Email}", admin.Email_Notif);

                                }

                            }
                        }
                    _logger.LogInformation("Operation {OperationId} modified successfully", entity.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid Client UserName value: {Id}", _currentUserService.Id);
                        throw new InvalidOperationException("Invalid Client Id value.");
                    }

                }
                else
                {
                    _logger.LogInformation("No changes were made to the operation {OperationId}", entity.Id);
                }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during ClientUpdateOperationCommandDetails processing.");

            throw ex switch
            {
                NotFoundException or InvalidOperationException or UnauthorizedAccessException => ex,
                _ => new ApplicationException(ex.Message, ex),
            };
        }
    }
}
