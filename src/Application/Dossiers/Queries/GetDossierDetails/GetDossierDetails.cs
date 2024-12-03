using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Dossiers.Queries.GetDossierDetails;

[Authorize(Roles = Roles.AdminAndAgent)]
public record GetDossierDetailsQuery : IRequest<DossierDetailVm>
{
    public required string CodeDossier { get; init; }
}

public class GetDossierDetailsQueryValidator : AbstractValidator<GetDossierDetailsQuery>
{
    public GetDossierDetailsQueryValidator()
    {
        RuleFor(x => x.CodeDossier)
       .NotNull().NotEmpty()
           .WithMessage("Code du dossier is required");
    }
}

public class GetDossierDetailsQueryHandler : IRequestHandler<GetDossierDetailsQuery, DossierDetailVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetDossierDetailsQueryHandler> _logger;

    public GetDossierDetailsQueryHandler(
        IApplicationDbContext context,
        IMapper mapper,
        ILogger<GetDossierDetailsQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<DossierDetailVm> Handle(GetDossierDetailsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetDossierDetailsQuery for CodeDossier: {CodeDossier}", request.CodeDossier);

        try
        {
            var distinctEtatPayements = _context.Factures
                .Where(p => p.CodeDossier == request.CodeDossier && !string.IsNullOrWhiteSpace(request.CodeDossier))
                .Select(p => p.EtatPayement)
                .Distinct()
                .ToList();

            _logger.LogDebug("Distinct payment states for CodeDossier {CodeDossier}: {EtatPayements}", request.CodeDossier, distinctEtatPayements);

            var opList = await _context.Operations
                .Where(f => !string.IsNullOrWhiteSpace(f.CodeDossier) && f.CodeDossier.Trim() == request.CodeDossier.Trim())
                .ToListAsync(cancellationToken);
            _logger.LogDebug("Retrieved {OperationCount} operations for CodeDossier: {CodeDossier}", opList.Count, request.CodeDossier);

            var factureList = await _context.Factures
                .Where(p => !string.IsNullOrWhiteSpace(p.CodeDossier) && p.CodeDossier.Trim() == request.CodeDossier.Trim())
                .ToListAsync(cancellationToken);
            _logger.LogDebug("Retrieved {FactureCount} factures for CodeDossier: {CodeDossier}", factureList.Count, request.CodeDossier);

            if (!opList.Any())
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
                            : EtatPayement.Impayée,
                    Client = opList.First().UserId ?? string.Empty,
                    Agents = string.Join("-", opList.Select(p => p.ReserverPar)) ?? string.Empty
                },
                FactureDtos = await _context.Factures
                    .Where(p => !string.IsNullOrWhiteSpace(p.CodeDossier) && p.CodeDossier.Trim() == request.CodeDossier.Trim())
                    .ProjectTo<FactureDto>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken),
            };

            _logger.LogInformation("Successfully handled GetDossierDetailsQuery for CodeDossier: {CodeDossier}", request.CodeDossier);
            return dossierDetails;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error in GetDossierDetailsQuery for CodeDossier: {CodeDossier}", request.CodeDossier);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling GetDossierDetailsQuery for CodeDossier: {CodeDossier}", request.CodeDossier);
            throw;
        }
    }
}