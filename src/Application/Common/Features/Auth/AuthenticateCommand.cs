using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;

namespace NejPortalBackend.Application.Features.Auth;


public record AuthenticateCommand : IRequest<LoginResponse>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string AppIdentifier { get; init; }
}

public class AuthenticateCommandValidator : AbstractValidator<AuthenticateCommand>
{
    public AuthenticateCommandValidator()
    {
        RuleFor(x => x.Email)
           .NotNull()
           .NotEmpty().WithMessage("Email is Requered.");
        RuleFor(x => x.Password)
           .NotNull()
           .NotEmpty().WithMessage("Password is Requered.");
            RuleFor(x => x.AppIdentifier)
           .NotNull()
           .NotEmpty().WithMessage("AppIdentifier is Requered.");
    }
}

public class AuthenticateCommandHandler : IRequestHandler<AuthenticateCommand, LoginResponse>
{
    private readonly IIdentityService _identityService;

    public AuthenticateCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<LoginResponse> Handle(AuthenticateCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.LoginAsync(request.Email, request.Password,request.AppIdentifier);
    }
}
