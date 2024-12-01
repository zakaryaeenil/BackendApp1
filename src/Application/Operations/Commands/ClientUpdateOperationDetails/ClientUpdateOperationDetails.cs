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
        RuleFor(v => v.TypeOperationId).NotEmpty()
                .NotNull().WithMessage("Type is required.");
        RuleFor(v => v.OperationId).NotEmpty()
                .NotNull().WithMessage("Operation is required.");
    }
}

public class ClientUpdateOperationDetailsCommandHandler : IRequestHandler<ClientUpdateOperationDetailsCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;
    private readonly ILogger<ClientUpdateOperationDetailsCommandHandler> _logger;

    public ClientUpdateOperationDetailsCommandHandler(IApplicationDbContext context, IUser currentUserService, IIdentityService identityService, ILogger<ClientUpdateOperationDetailsCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _logger = logger;
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

            // Check if the operation is in the correct state for modification
            if (entity.EtatOperation == EtatOperation.depotDossier)
            {
                // Update TypeOperation, Bureau, and Regime if necessary
                bool isUpdated = false;

                if (entity.TypeOperation != (TypeOperation)request.TypeOperationId)
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
                            Action = $"L'opération numéro : {entity.Id} a été modifiée par le client {clientUsername}: Details Operation a été modifié avec succès.",
                            UserId = _currentUserService.Id,
                            OperationId = entity.Id
                        };

                        // Add the historique record to the database
                        await _context.Historiques.AddAsync(historique, cancellationToken);

                        // Save changes to the database
                        await _context.SaveChangesAsync(cancellationToken);

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
            else
            {
                _logger.LogWarning("Operation {OperationId} is not in depotDossier state and cannot be updated.", entity.Id);
                throw new InvalidOperationException("Operation is not in the depotDossier state to update it.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during ClientUpdateOperationCommandDetails processing.");

            throw ex switch
            {
                NotFoundException or InvalidOperationException or UnauthorizedAccessException => ex,
                _ => new ApplicationException("An unexpected error occurred. Please try again later.", ex),
            };
        }
    }
}
