using Microsoft.EntityFrameworkCore;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Application.Comptes.Commands.CreateCompte;

[Authorize(Roles = Roles.Administrator)]
public record CreateCompteCommand : IRequest<(Result Result, string? UserId)>
{
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public required string Email_Notif { get; init; }
    public string? PhoneNumber { get; init; }
    public required string Password { get; init; }
    public string? CodeUser { get; init; }
    public int? TypeOperationId { get; init; }
}

public class CreateCompteCommandValidator : AbstractValidator<CreateCompteCommand>
{
    public CreateCompteCommandValidator()
    {
        RuleFor(x => x.UserName).NotNull().NotEmpty().WithMessage("UserName is required.");
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress().WithMessage("A valid email is required.");
        RuleFor(x => x.Email_Notif).NotNull().NotEmpty().EmailAddress().WithMessage("A valid notification email is required.");
        RuleFor(x => x.Password).NotNull().NotEmpty().WithMessage("Password is required.");
    }
}

public class CreateCompteCommandHandler : IRequestHandler<CreateCompteCommand, (Result Result, string? UserId)>
{
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;


    public CreateCompteCommandHandler(IIdentityService identityService, IUser currentUserService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    public async Task<(Result Result, string? UserId)> Handle(CreateCompteCommand request, CancellationToken cancellationToken)
    {

        if (string.IsNullOrWhiteSpace(_currentUserService.Id))
        {
         
            throw new UnauthorizedAccessException("User is not authorized.");
        }
        bool isAdmin = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Administrator);
        int? typeOperation = await _identityService.GetTypeOperationAsync(_currentUserService.Id);

    
        // Filter operations by user and criteria
        if (isAdmin)
        {
            typeOperation = typeOperation != null ? typeOperation : request.TypeOperationId;
        }

        return await _identityService.CreateUserAsync(request.UserName, request.Password, request.Email, typeOperation, request.PhoneNumber, request.CodeUser,request.Email_Notif, cancellationToken);
    }
}
