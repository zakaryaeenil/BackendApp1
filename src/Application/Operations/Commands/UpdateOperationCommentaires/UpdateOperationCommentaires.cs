using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Application.Operations.Commands.UpdateOperationCommentaires;

[Authorize(Roles = Roles.AdminAndAgent)]
public record UpdateOperationCommentairesCommand : IRequest
{
    public int OperationId { get; set; }
    public required string Commentaire { get; init; }
}

public class UpdateOperationCommentairesCommandValidator : AbstractValidator<UpdateOperationCommentairesCommand>
{
    public UpdateOperationCommentairesCommandValidator()
    {
        RuleFor(v => v.Commentaire).NotEmpty()
               .NotNull().WithMessage("Commantaire not empty and not null.");
        RuleFor(v => v.OperationId).NotEmpty()
              .NotNull().WithMessage("Operation is required.");
    }
}

public class UpdateOperationCommentairesCommandHandler : IRequestHandler<UpdateOperationCommentairesCommand>
{
    private readonly ILogger<UpdateOperationCommentairesCommandHandler> _logger;
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;

    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public UpdateOperationCommentairesCommandHandler(IEmailService emailService,IApplicationDbContext context, IUser currentUserService, IIdentityService identityService, ILogger<UpdateOperationCommentairesCommandHandler> logger, INotificationService notificationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _logger = logger;
        _notificationService = notificationService;
        _emailService = emailService;
    }

    public async Task Handle(UpdateOperationCommentairesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to process UpdateOperationCommentairesCommand for user: {UserId}, operation : {OperationId}", _currentUserService.Id, request.OperationId);

        try
        {
            if (string.IsNullOrWhiteSpace(_currentUserService.Id))
                throw new UnauthorizedAccessException();

            Operation entity = await _context.Operations
                    .FindAsync(new object[] { request.OperationId }, cancellationToken) ?? throw new NotFoundException(nameof(Operations), request.OperationId.ToString());

            if (!string.IsNullOrWhiteSpace(request.Commentaire))
            {
                Commentaire commentaire = new Commentaire { Message = request.Commentaire, OperationId = entity.Id, UserId = _currentUserService.Id };

                await _context.Commentaires.AddAsync(commentaire, cancellationToken);
                var userName = await _identityService.GetUserNameAsync(_currentUserService.Id);
                // Create and log the historical record for the modification
                var historique = new Historique
                {
                    Action = $"L'opération numéro : {entity.Id} a été modifiée  {userName} : Commantaire a été ajouté avec succès.",
                    UserId = _currentUserService.Id,
                    OperationId = entity.Id
                };

                // Add the historique record to the database
                await _context.Historiques.AddAsync(historique, cancellationToken);

                // Save changes to the database
                await _context.SaveChangesAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(entity.ReserverPar))
                {
                    // Send notification
                    var notificationAgentMessage = "operation (ID: " + entity.Id + " ) : Comments has been modified.";
                    await _notificationService.SendNotificationAsync(entity.ReserverPar, notificationAgentMessage, cancellationToken);


                    var reserverParUserName = await _identityService.GetUserNameAsync(entity.ReserverPar);
                    var reserverParEmail = await _identityService.GetUserEmailNotifAsync(entity.ReserverPar);


                    // Send the reset password link to the user via email
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(reserverParUserName) && !string.IsNullOrWhiteSpace(reserverParEmail))
                            await _emailService.SendOperationEmailAsync(reserverParEmail, entity.Id, notificationAgentMessage, reserverParUserName);
                    }
                    catch (Exception ex)
                    {
                        // Log the error and notify
                        _logger.LogError(ex, "Failed to send update Operation email to {Email}", reserverParUserName);

                    }
                }

                // Send notification
                var notificationMessage = "operation (ID: " + entity.Id + " ) :  Comments has been modified.";
                await _notificationService.SendNotificationAsync(entity.UserId, notificationMessage, cancellationToken);

                var clientUserName = await _identityService.GetUserNameAsync(entity.UserId);
                var clientEmail = await _identityService.GetUserEmailNotifAsync(entity.UserId);


                // Send email to the user via email
                try
                {
                    if (!string.IsNullOrWhiteSpace(clientUserName) && !string.IsNullOrWhiteSpace(clientEmail))
                        await _emailService.SendOperationEmailAsync(clientEmail, entity.Id, notificationMessage, clientUserName);
                }
                catch (Exception ex)
                {
                    // Log the error and notify
                    _logger.LogError(ex, "Failed to send update Comments Operation email to {Email}", clientEmail);

                }


                _logger.LogInformation("Operation {OperationId} modified successfully", entity.Id);
            }
            else
            {
                _logger.LogInformation("No changes were made to the operation {OperationId}", entity.Id);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during UpdateOperationCommentairesCommand processing.");

            throw ex switch
            {
                NotFoundException or InvalidOperationException or UnauthorizedAccessException => ex,
                _ => new ApplicationException(ex.Message, ex),
            };
        }
    }
}
