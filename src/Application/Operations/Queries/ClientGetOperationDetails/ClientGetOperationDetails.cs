using System.Reflection.Metadata.Ecma335;
using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Domain.Constants;

namespace NejPortalBackend.Application.Operations.Queries.ClientGetOperationDetails;

[Authorize(Roles = Roles.Client)]
public record ClientGetOperationDetailsQuery : IRequest<OperationDetailVm>
{
    public required int OperationId { get; init; }
}

public class ClientGetOperationDetailsQueryValidator : AbstractValidator<ClientGetOperationDetailsQuery>
{
    public ClientGetOperationDetailsQueryValidator()
    {
        RuleFor(x => x.OperationId)
            .NotNull()
            .NotEmpty().WithMessage("Id Operation is Requered.");
    }
}

public class ClientGetOperationDetailsQueryHandler : IRequestHandler<ClientGetOperationDetailsQuery, OperationDetailVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly ILogger<ClientGetOperationDetailsQueryHandler> _logger;


    public ClientGetOperationDetailsQueryHandler(
        IApplicationDbContext context,
        IUser currentUserService,
        IIdentityService identityService,
        IMapper mapper,
        ILogger<ClientGetOperationDetailsQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<OperationDetailVm> Handle(ClientGetOperationDetailsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ClientGetOperationDetailsQuery received with parameters: {Request}", request);

        if (string.IsNullOrWhiteSpace(_currentUserService.Id))
        {
            _logger.LogError("Unauthorized access attempt. Current user ID is null or empty.");
            throw new UnauthorizedAccessException("User is not authorized.");
        }

        // Validate client role
        var isClient = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Client);
        if (!isClient)
        {
            _logger.LogWarning("User {UserId} attempted to access client operations without proper role.", _currentUserService.Id);
            throw new InvalidOperationException($"User {_currentUserService.Id} does not have the required role.");
        }

        try
        {
            // Check if the operation exists
            var operationExists = await _context.Operations
                .AnyAsync(o => o.Id == request.OperationId && o.UserId == _currentUserService.Id, cancellationToken);

            if (!operationExists)
            {
                _logger.LogWarning("Operation {OperationId} does not exist or does not belong to user {UserId}.", request.OperationId, _currentUserService.Id);
                return new OperationDetailVm(); // Return an empty VM if operation doesn't exist
            }

            // Fetch operation details
            var operation = await _context.Operations
                .Where(o => o.Id == request.OperationId && o.UserId == _currentUserService.Id)
                .ProjectTo<ClientOperationDto>(_mapper.ConfigurationProvider)
                .FirstAsync(cancellationToken);

            // Fetch associated comments
            var commentaires = await _context.Commentaires
                .Where(c => c.OperationId == request.OperationId)
                .OrderByDescending(c => c.Created)
                .AsNoTracking()
                .ProjectTo<CommentaireDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            // Fetch associated documents
            var documents = await _context.Documents
                .Where(d => d.OperationId == request.OperationId)
                .OrderByDescending(d => d.Created)
                .AsNoTracking()
                .ProjectTo<DocumentDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            // Return the VM
            var operationDetailsVm = new OperationDetailVm
            {
                Commentaires = commentaires,
                Documents = documents,
                Operation = operation
            };

            _logger.LogInformation("Operation details retrieved successfully for OperationId: {OperationId}, UserId: {UserId}.", request.OperationId, _currentUserService.Id);
            return operationDetailsVm;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing ClientGetOperationDetailsQuery for user {UserId}.", _currentUserService.Id);
            throw;
        }
    }

}
