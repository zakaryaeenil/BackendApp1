using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;

namespace NejPortalBackend.Application.Features.Auth;

public record ForgotPasswordCommand : IRequest<Result>
{
    public required string Email { get; init; }
}

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
     .NotNull()
     .NotEmpty().EmailAddress().WithMessage("Email is Requered.");
    }
}
public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;

    public ForgotPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.ForgotPasswordAsync(request.Email);
    }
}