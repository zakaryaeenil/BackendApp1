using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;

namespace NejPortalBackend.Application.Features.Auth;

[Authorize(Roles = Roles.AdminAndAgentAndClient)]
public record RefreshTokenCommand : IRequest<LoginResponse>
{
    public required string RefreshToken { get; init; }
    public required string AppIdentifier { get; init; }
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
           .NotNull()
           .NotEmpty().WithMessage("RefreshToken is Requered.");
        RuleFor(x => x.AppIdentifier)
           .NotNull()
           .NotEmpty().WithMessage("AppIdentifier is Requered.");
    }
}
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IIdentityService _identityService;

    public RefreshTokenCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.RefreshTokenAsync(request.RefreshToken,request.AppIdentifier);
    }
}