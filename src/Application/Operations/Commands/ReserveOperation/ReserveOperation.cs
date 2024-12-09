using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Operations.Commands.UpdateOperationCommentaires;
using NejPortalBackend.Domain.Constants;

namespace NejPortalBackend.Application.Operations.Commands.ReserveOperation;


[Authorize(Roles = Roles.Agent)]
public record ReserveOperationCommand : IRequest
{
    public int OperationId { get; init; }

}

public class ReserveOperationCommandValidator : AbstractValidator<ReserveOperationCommand>
{
    public ReserveOperationCommandValidator()
    {
        RuleFor(v => v.OperationId).NotEmpty()
            .NotNull().WithMessage("Operation is required.");
    }
}

public class ReserveOperationCommandHandler : IRequestHandler<ReserveOperationCommand>
{
    private readonly ILogger<ReserveOperationCommand> _logger;
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;

    public ReserveOperationCommandHandler(IApplicationDbContext context, IUser currentUserService, IIdentityService identityService, ILogger<ReserveOperationCommand> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task Handle(ReserveOperationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling ReserveOperationCommand for OperationId: {OperationId}", request.OperationId);

            if (_currentUserService.Id == null)
            {
                _logger.LogWarning("Unauthorized access attempt for OperationId: {OperationId}", request.OperationId);
                throw new UnauthorizedAccessException("Current user is not authorized.");
            }

            // Retrieve the operation entity
            var entity = await _context.Operations.FindAsync(new object[] { request.OperationId }, cancellationToken);
            if (entity == null)
            {
                _logger.LogWarning("Operation not found with Id: {OperationId}", request.OperationId);
                throw new NotFoundException(nameof(Operations), request.OperationId.ToString());
            }

            var userId = _currentUserService.Id;
            var isAgent = await _identityService.IsInRoleAsync(userId, Roles.Agent);

            if (isAgent)
            {
                if (!entity.EstReserver)
                {
                    entity.ReserverPar = userId;
                    entity.EstReserver = true;
                    _context.Operations.Update(entity);

                    _logger.LogInformation("Operation with Id: {OperationId} reserved by UserId: {UserId}", request.OperationId, userId);

                    // Save changes to the database
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Changes successfully saved to the database for OperationId: {OperationId}", request.OperationId);
                }
                else
                {
                    _logger.LogWarning("Operation with Id: {OperationId} is already reserved.", request.OperationId);
                }
            }
            else
            {
                _logger.LogWarning("UserId: {UserId} is not authorized as an agent to reserve OperationId: {OperationId}", userId, request.OperationId);
                throw new UnauthorizedAccessException("User is not authorized as an agent.");
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access error while handling ReserveOperationCommand for OperationId: {OperationId}", request.OperationId);
            throw;
        }
        catch (NotFoundException ex)
        {
            _logger.LogError(ex, "Not found error while handling ReserveOperationCommand for OperationId: {OperationId}", request.OperationId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while handling ReserveOperationCommand for OperationId: {OperationId}", request.OperationId);
            throw; // Rethrow to ensure the error propagates up the call stack
        }
    }

}
