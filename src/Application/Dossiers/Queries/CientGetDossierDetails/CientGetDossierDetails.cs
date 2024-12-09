using System.Data;
using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Application.Dossiers.Queries.GetDossierDetails;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Dossiers.Queries.CientGetDossierDetails;

[Authorize(Roles = Roles.Client)]
public record CientGetDossierDetailsQuery : IRequest<DossierDetailVm>
{
    public required string CodeDossier { get; init; }
}

public class CientGetDossierDetailsQueryValidator : AbstractValidator<CientGetDossierDetailsQuery>
{
    public CientGetDossierDetailsQueryValidator()
    {
        RuleFor(x => x.CodeDossier)
       .NotNull().NotEmpty()
           .WithMessage("Code du dossier is required");
    }
}

public class CientGetDossierDetailsQueryHandler : IRequestHandler<CientGetDossierDetailsQuery, DossierDetailVm>
{

    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CientGetDossierDetailsQueryHandler> _logger;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;

    public CientGetDossierDetailsQueryHandler(
        IApplicationDbContext context,
          IUser currentUserService,
        IIdentityService identityService,
        IMapper mapper,
        ILogger<CientGetDossierDetailsQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _identityService = identityService;
    }

    public async Task<DossierDetailVm> Handle(CientGetDossierDetailsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ClientGetAllOperationsQuery received with parameters: {Request}", request);

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
            throw new InvalidOperationException("User " + _currentUserService.Id + " does not have the required role.");
        }

        try
        {

            var usercode = await _identityService.GetCodeClientAsync(_currentUserService.Id);

            if (string.IsNullOrWhiteSpace(usercode))
            {
                _logger.LogWarning("User {UserId} attempted to access client operations without proper code Client.", _currentUserService.Id);
                throw new InvalidOperationException("User " + _currentUserService.Id + " does not have the required  code Client.");
            }

            var distinctEtatPayements = _context.Factures
               .Where(p => p.CodeDossier == request.CodeDossier && !string.IsNullOrWhiteSpace(request.CodeDossier) && p.CodeClient == usercode)
               .Select(p => p.EtatPayement)
               .Distinct()
               .ToList();

            _logger.LogDebug("Distinct payment states for CodeDossier {CodeDossier}: {EtatPayements}", request.CodeDossier, distinctEtatPayements);


            var opList = await _context.Operations
                .Where(f => !string.IsNullOrWhiteSpace(f.CodeDossier) && f.CodeDossier.Trim() == request.CodeDossier.Trim() && f.UserId == _currentUserService.Id)
                .ToListAsync(cancellationToken);
            _logger.LogDebug("Retrieved {OperationCount} operations for CodeDossier: {CodeDossier}", opList.Count, request.CodeDossier);

            var factureList = await _context.Factures
               .Where(p => !string.IsNullOrWhiteSpace(p.CodeDossier) && p.CodeDossier.Trim() == request.CodeDossier.Trim() && p.CodeClient == usercode)
               .ToListAsync(cancellationToken);
            _logger.LogDebug("Retrieved {FactureCount} factures for CodeDossier: {CodeDossier}", factureList.Count, request.CodeDossier);

            if (opList.Count == 0)
            {
                _logger.LogWarning("No operations found for CodeDossier: {CodeDossier}", request.CodeDossier);
                throw new InvalidOperationException($"No operations found for dossier {request.CodeDossier}");
            }
            var operationIds = opList.Select(o => o.Id).ToList();
            var codeDossier = opList.First().CodeDossier ?? null;
            var dossierDetails = new DossierDetailVm
            {
                Operations = await _context.Operations
                 .Where(c => c.CodeDossier == request.CodeDossier)
                 .AsNoTracking()
                 .ProjectTo<OperationDto>(_mapper.ConfigurationProvider)
                 .ToListAsync(cancellationToken),

                Dossier = new DossierDto
                {
                    CodeDossier = codeDossier,
                    NombreFactures = factureList.Count(f => f.CodeDossier == opList.First().CodeDossier),
                    Desription = string.Join("\n", factureList.Select(p => p.Description)),
                    MontantTotal = factureList.Sum(p => p.MontantTotal),
                    MontantPaye = factureList.Sum(p => p.MontantPaye),
                    MontantReste = factureList.Sum(p => p.MontantTotal - p.MontantPaye),
                    EtatPayement = _context.Factures.Any(f => !string.IsNullOrWhiteSpace(codeDossier) && f.CodeDossier == codeDossier && f.EtatPayement == EtatPayement.PayementIncomplet)
                     ? EtatPayement.PayementIncomplet
                     : _context.Factures.All(f => !string.IsNullOrWhiteSpace(codeDossier) && f.CodeDossier == codeDossier && f.EtatPayement == EtatPayement.Payée)
                         ? EtatPayement.Payée
                         : EtatPayement.Impayée
                },
                FactureDtos = await _context.Factures
                 .Where(p => !string.IsNullOrWhiteSpace(p.CodeDossier) && p.CodeDossier.Trim() == request.CodeDossier.Trim())
                 .ProjectTo<FactureDto>(_mapper.ConfigurationProvider)
                 .ToListAsync(cancellationToken),
            };

            _logger.LogInformation("Successfully handled ClientGetAllOperationsQuery for CodeDossier: {CodeDossier}", request.CodeDossier);
            return dossierDetails;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex.Message, "Validation error in ClientGetAllOperationsQuery for CodeDossier: {CodeDossier}", request.CodeDossier);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "An error occurred while handling ClientGetAllOperationsQuery for CodeDossier: {CodeDossier}", request.CodeDossier);
            throw;
        }
    }
}
