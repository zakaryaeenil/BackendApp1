using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Operations.Commands.UpdateOperationDetails;

[Authorize(Roles = Roles.AdminAndAgent)]
public record UpdateOperationDetailsCommand : IRequest
{
    public int OperationId { get; init; }

    public int TypeOperationId { get; init; }
    public string? Bureau { get; init; }
    public string? Regime { get; init; }
    public string? ReserverPar { get; init; }
    public int EtatOperationId { get; init; }
    public string? CodeDossier { get; init; }
}

public class UpdateOperationDetailsCommandValidator : AbstractValidator<UpdateOperationDetailsCommand>
{
    public UpdateOperationDetailsCommandValidator()
    {
        RuleFor(v => v.EtatOperationId)
              .NotNull().WithMessage("Etat is required.");
        RuleFor(v => v.TypeOperationId)
              .NotNull().WithMessage("Type is required.");
        RuleFor(v => v.OperationId)
              .NotNull().WithMessage("Operation is required.");
    }
}

public class UpdateOperationDetailsCommandHandler : IRequestHandler<UpdateOperationDetailsCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;
    private readonly ILogger<UpdateOperationDetailsCommandHandler> _logger;

    public UpdateOperationDetailsCommandHandler(IApplicationDbContext context, IUser currentUserService, IIdentityService identityService, ILogger<UpdateOperationDetailsCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _logger = logger;
    }
    public async Task Handle(UpdateOperationDetailsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to process UpdateOperationDetailsCommand for Operation: {OperationId}", request.OperationId);

        try
        {
            // Ensure the current user is authenticated
            if (string.IsNullOrWhiteSpace(_currentUserService.Id))
            {
                throw new UnauthorizedAccessException();
            }

            // Validate TypeOperationId
            bool isValidTypeOperation = Enum.IsDefined(typeof(TypeOperation), request.TypeOperationId);
            if (!isValidTypeOperation)
            {
                _logger.LogWarning("Invalid TypeOperation value: {TypeOperationId}", request.TypeOperationId);
                throw new InvalidOperationException("Invalid TypeOperation value.");
            }
            
            // Validate EtatOperationId
            bool isValidEtatOperation = Enum.IsDefined(typeof(EtatOperation), request.EtatOperationId);
            if (!isValidEtatOperation)
            {
                _logger.LogWarning("Invalid EtatOperation value: {EtatOperationId}", request.EtatOperationId);
                throw new InvalidOperationException("Invalid EtatOperation value.");
            }
            
            var isAgent = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Agent);
            var isAdmin = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Administrator);

            // Fetch the operation entity by its Id
            var entity = await _context.Operations
                .FindAsync(new object[] { request.OperationId }, cancellationToken)
                ?? throw new NotFoundException(nameof(Operations), request.OperationId.ToString());

            bool isEtatOperationCloture = request.EtatOperationId == (int)EtatOperation.cloture;
            bool isCodeDossierValid = !string.IsNullOrWhiteSpace(request.CodeDossier) && _context.Dossiers.Any(d => d.CodeDossier == request.CodeDossier);
            
            if (isEtatOperationCloture && !isCodeDossierValid)
            {
                throw new InvalidOperationException("Impossile de cloturer une opération sans unn code dossier valid");
            }
            else
            {
                // Get the client's username
                var membreUsername = await _identityService.GetUserNameAsync(_currentUserService.Id);
                if (!string.IsNullOrWhiteSpace(membreUsername))
                {
                    
                    bool isUpdated = false;

                    if (entity.TypeOperation != (TypeOperation)request.TypeOperationId && ((isAgent && !isEtatOperationCloture ) || isAdmin))
                    {
                        entity.TypeOperation = (TypeOperation)request.TypeOperationId;
                        isUpdated = true;
                    }
                    if (entity.EtatOperation != (EtatOperation)request.EtatOperationId && ((isAgent && !isEtatOperationCloture) || isAdmin))
                    {
                        entity.EtatOperation = (EtatOperation)request.EtatOperationId;
                        isUpdated = true;
                    }
                    if (entity.Bureau != request.Bureau && ((isAgent && !isEtatOperationCloture) || isAdmin))
                    {
                        entity.Bureau = request.Bureau;
                        isUpdated = true;
                    }
                    if (entity.Regime != request.Regime && ((isAgent && !isEtatOperationCloture) || isAdmin))
                    {
                        entity.Regime = request.Regime;
                        isUpdated = true;
                    }
                    if (entity.ReserverPar != request.ReserverPar && ((isAgent && !isEtatOperationCloture) || isAdmin))
                    {
                        entity.ReserverPar = request.ReserverPar;
                        entity.EstReserver = true;
                        isUpdated = true;
                    }
                    if ((entity.CodeDossier != request.CodeDossier && isCodeDossierValid && ((isAgent && !isEtatOperationCloture ) || isAdmin)) || (request.CodeDossier == null && ((isAgent && !isEtatOperationCloture) || isAdmin)))
                    {
                        entity.CodeDossier = request.CodeDossier;
                        isUpdated = true;
                    }
                    // Only update if there were changes
                    if (isUpdated)
                    {
                        _context.Operations.Update(entity);

                        // Create and log the historical record for the modification
                        var historique = new Historique
                        {
                            Action = "L'opération numéro : "+ entity.Id+" a été modifiée par l'equipe : "+ membreUsername + " : Details Operation a été modifié avec succès.",
                            UserId = _currentUserService.Id,
                            OperationId = entity.Id
                        };

                        // Add the historique record to the database
                        await _context.Historiques.AddAsync(historique, cancellationToken);

                        // Save changes to the database
                        await _context.SaveChangesAsync(cancellationToken);

                        _logger.LogInformation("Operation {OperationId} modified successfully : Details Operation a été modifié avec succès.", entity.Id);

                    }
                    else
                    {
                        _logger.LogWarning("Operation {OperationId}  not be updated.", entity.Id);
                        throw new InvalidOperationException("operation  not be updated");
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid membre equipe UserName value: {Id}", _currentUserService.Id);
                    throw new InvalidOperationException("Invalid membre equipe Id value.");
                }
            }
       
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during UpdateOperationCommand processing.");

            throw ex switch
            {
                NotFoundException or InvalidOperationException or UnauthorizedAccessException => ex,
                _ => new ApplicationException("An unexpected error occurred. Please try again later.", ex),
            };
        }
    }
}
