using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;

namespace NejPortalBackend.Application.Features.Auth;

public record ResetPasswordCommand : IRequest<Result>
{
    public required string Email { get; init; }
    public required string Token { get; init; }
    public required string NewPassword { get; init; }
}

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
     .NotNull()
     .NotEmpty().EmailAddress().WithMessage("Email is Requered.");
        RuleFor(x => x.NewPassword)
           .NotNull()
           .NotEmpty().WithMessage("Password is Requered.");
        RuleFor(x => x.Token)
       .NotNull()
       .NotEmpty().WithMessage("Token is Requered.");
    }
}
public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;

    public ResetPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {

        return await _identityService.ResetPasswordAsync(request.Email,request.Token,request.NewPassword,cancellationToken);
    }
}