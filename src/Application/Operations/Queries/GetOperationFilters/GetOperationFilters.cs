using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Operations.Queries.GetOperationFilters;

[Authorize(Roles = Roles.AdminAndAgent)]
public record GetOperationFiltersQuery : IRequest<OperationFiltersVm>;


public class GetOperationFiltersQueryHandler : IRequestHandler<GetOperationFiltersQuery, OperationFiltersVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly ILogger<GetOperationFiltersQueryHandler> _logger;

    public GetOperationFiltersQueryHandler(
        IApplicationDbContext context,
        IUser currentUserService,
        IIdentityService identityService,
        IMapper mapper,
        ILogger<GetOperationFiltersQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<OperationFiltersVm> Handle(GetOperationFiltersQuery request, CancellationToken cancellationToken)
    {
         if (string.IsNullOrWhiteSpace(_currentUserService.Id))
        {
            _logger.LogError("Unauthorized access attempt. Current user ID is null or empty.");
            throw new UnauthorizedAccessException("User is not authorized.");
        }

        // Validate client role
        bool isValidUser = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Administrator) || await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Agent) ;
        if (!isValidUser)
        {
            _logger.LogWarning("User {UserId} attempted to access equipe operations without proper role.", _currentUserService.Id);
            throw new InvalidOperationException($"User {_currentUserService.Id} does not have the required role.");
        }
        try
        {
            var etatOperations = Enum.GetValues(typeof(EtatOperation))
                .Cast<EtatOperation>()
                .Select(p => new EtatOperationDto { Value = (int)p, Name = p.ToString() })
                .ToList();

            var typeOperations = Enum.GetValues(typeof(TypeOperation))
                .Cast<TypeOperation>()
                .Select(p => new TypeOperationDto { Value = (int)p, Name = p.ToString() })
                .ToList();


            bool isAgent = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Agent);
            bool isAdmin = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Administrator);

            int? typeOperation = await _identityService.GetTypeOperationAsync(_currentUserService.Id);

            var listAgents = await _identityService.GetAllUsersInRoleAsync(Roles.Agent);
            if (isAdmin)
            {
                listAgents = typeOperation != null ? listAgents.Where(o =>o.TypeOperation != null && (int)o.TypeOperation == typeOperation).ToList() : listAgents;
                typeOperations = typeOperation != null ? typeOperations.Where(o =>  (int)o.Value == typeOperation).ToList() : typeOperations;
            }
            if (isAgent)
            {
                listAgents = typeOperation != null ? listAgents.Where(o => o.TypeOperation != null && (int)o.TypeOperation == typeOperation).ToList() : throw new UnauthorizedAccessException("User is not authorized.");
                typeOperations = typeOperation != null ? typeOperations.Where(o => (int)o.Value == typeOperation).ToList() : throw new UnauthorizedAccessException("User is not authorized.");
            }
            var listClients = await _identityService.GetAllUsersInRoleAsync(Roles.Client);

            //
            var dossierFilters = await _context.Dossiers
                                                .AsNoTracking()
                                                .Select(g => new
                                                {
                                                    g.CodeDossier,
                                                    g.CodeClient
                                                })
                                                .ToListAsync(cancellationToken);

           var dossierHelpersDtos = new List<DossierHelpersDto>();

           foreach (var dossier in dossierFilters)
             {
              var userName = await _identityService.GetUserNameByCodeClientAsync(dossier.CodeClient);
                dossierHelpersDtos.Add(
                    new DossierHelpersDto
                    {
                        CodeDossier = dossier.CodeDossier,
                        Nom = $"{dossier.CodeDossier}--{userName}"
                    }
                );
             }
                                                  
            var filtersVm = new OperationFiltersVm
            {
                EtatOperations = etatOperations,
                TypeOperations = typeOperations,
                ListAgents = listAgents,
                ListClients = listClients,
                ListDossierHelpersDto = dossierHelpersDtos
            };

            _logger.LogInformation("Successfully retrieved operation filters.");
            return await Task.FromResult(filtersVm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing ClientGetOperationFiltersQuery.");
            throw;
        }
    }
}
