using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;

namespace NejPortalBackend.Application.Common.Features.ChangePasswordCompte;

[Authorize(Roles = Roles.AdminAndAgentAndClient)]
public record ChangePasswordCompteCommand : IRequest<(Result Result, string? UserId)>
{
    public required string OldPassword { get; init; }
    public required string NewPassword { get; init; }
    public required string ConfirmPassword { get; init; }
}

public class ChangePasswordCompteCommandValidator : AbstractValidator<ChangePasswordCompteCommand>
{
    public ChangePasswordCompteCommandValidator()
    {
        RuleFor(x => x.OldPassword).NotNull().NotEmpty().WithMessage("OldPassword is required.");
        RuleFor(x => x.NewPassword).NotNull().NotEmpty().WithMessage("NewPassword is required.");
        RuleFor(x => x.ConfirmPassword).NotNull().NotEmpty().WithMessage("ConfirmPassword is required.");
    }
}

public class ChangePasswordCompteCommandHandler : IRequestHandler<ChangePasswordCompteCommand, (Result Result, string? UserId)>
{
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;

    public ChangePasswordCompteCommandHandler(IIdentityService identityService, IUser currentUserService)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<(Result Result, string? UserId)> Handle(ChangePasswordCompteCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.Id == null)
            throw new UnauthorizedAccessException();
       return await _identityService.ChangePasswordAsync(_currentUserService.Id, request.OldPassword, request.NewPassword, request.ConfirmPassword);
    }
}
