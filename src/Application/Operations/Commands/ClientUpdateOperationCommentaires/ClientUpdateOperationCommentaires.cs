using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Application.Operations.Commands.ClientUpdateOperationCommentaires;

[Authorize(Roles = Roles.Client)]
public record ClientUpdateOperationCommentairesCommand : IRequest
{
    public required int OperationId { get; set; }
    public required string Commentaire { get; init; }

}

public class ClientUpdateOperationCommentairesCommandValidator : AbstractValidator<ClientUpdateOperationCommentairesCommand>
{
    public ClientUpdateOperationCommentairesCommandValidator()
    {
        RuleFor(v => v.Commentaire).NotEmpty()
              .NotNull().WithMessage("Commantaire is required.");
        RuleFor(v => v.OperationId).NotEmpty()
              .NotNull().WithMessage("Operation is required.");
    }
}

public class ClientUpdateOperationCommentairesCommandHandler : IRequestHandler<ClientUpdateOperationCommentairesCommand>
{

    private readonly ILogger<ClientUpdateOperationCommentairesCommandHandler> _logger;
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;

    public ClientUpdateOperationCommentairesCommandHandler(IApplicationDbContext context, IUser currentUserService, IIdentityService identityService, ILogger<ClientUpdateOperationCommentairesCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task Handle(ClientUpdateOperationCommentairesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to process ClientUpdateOperationCommentairesCommand for user: {UserId}, operation : {OperationId}", _currentUserService.Id, request.OperationId);

        try
        {
            if (string.IsNullOrWhiteSpace(_currentUserService.Id))
                throw new UnauthorizedAccessException();

            var entity = await _context.Operations
                    .FindAsync(new object[] { request.OperationId }, cancellationToken) ?? throw new NotFoundException(nameof(Operations), request.OperationId.ToString());

            if (!string.IsNullOrWhiteSpace(request.Commentaire))
            {
                // Validate client role
                if (!await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Client))
                {
                    _logger.LogWarning("Invalid Client Id value: {Id}", _currentUserService.Id);
                    throw new InvalidOperationException("Invalid Client Id value.");
                }

                Commentaire commentaire = new Commentaire { Message = request.Commentaire, OperationId = entity.Id, UserId = _currentUserService.Id };
                
                // Get the client's username
                var clientUsername = await _identityService.GetUserNameAsync(_currentUserService.Id);
                if (!string.IsNullOrWhiteSpace(clientUsername))
                {
                    await _context.Commentaires.AddAsync(commentaire, cancellationToken);

                    // Create and log the historical record for the modification
                    var historique = new Historique
                    {
                        Action = $"L'opération numéro : {entity.Id} a été modifiée par le client {clientUsername}: Commantaire Operation a été modifié avec succès.",
                        UserId = _currentUserService.Id,
                        OperationId = entity.Id
                    };

                    // Add the historique record to the database
                    await _context.Historiques.AddAsync(historique, cancellationToken);

                    // Save changes to the database
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Operation {OperationId} modified successfully: Operation a été modifié avec succès", entity.Id);
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
            _logger.LogError(ex, "An error occurred during ClientUpdateOperationCommentairesCommand processing.");

            throw ex switch
            {
                NotFoundException or InvalidOperationException or UnauthorizedAccessException => ex,
                _ => new ApplicationException("An unexpected error occurred. Please try again later.", ex),
            };
        }
    }
}
