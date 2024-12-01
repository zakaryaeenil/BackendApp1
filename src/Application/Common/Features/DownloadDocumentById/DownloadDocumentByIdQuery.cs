using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Domain.Constants;

namespace NejPortalBackend.Application.Features.DownloadDocumentById;

[Authorize(Roles = Roles.AdminAndAgentAndClient)]
public record DownloadDocumentByIdQuery : IRequest<DownloadDocumentVm>
{
    public required int DocumentId { get; init; }
}

public class DownloadDocumentByIdQueryValidator : AbstractValidator<DownloadDocumentByIdQuery>
{
    public DownloadDocumentByIdQueryValidator()
    {
        RuleFor(x => x.DocumentId)
           .NotNull()
           .NotEmpty().WithMessage("Id Document is Requered.");
    }
}

public class DownloadDocumentByIdQueryHandler : IRequestHandler<DownloadDocumentByIdQuery, DownloadDocumentVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly ILogger<DownloadDocumentByIdQueryHandler> _logger;


    public DownloadDocumentByIdQueryHandler(
        IApplicationDbContext context,
        IUser currentUserService,
        IIdentityService identityService,
        IMapper mapper,
        ILogger<DownloadDocumentByIdQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<DownloadDocumentVm> Handle(DownloadDocumentByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing DownloadDocumentByIdQuery for DocumentId: {DocumentId}", request.DocumentId);

        // Fetch document from the database
        var document = await _context.Documents.FindAsync(new object?[] { request.DocumentId }, cancellationToken: cancellationToken)
                        ?? throw new NotFoundException(nameof(DocumentDto), request.DocumentId.ToString());

        var filePath = document.CheminFichier;

        // Ensure the file exists before attempting to read it
        if (!File.Exists(filePath))
        {
            _logger.LogError("File not found at path: {FilePath} for DocumentId: {DocumentId}", filePath, request.DocumentId);
            throw new NotFoundException("File not found", filePath);
        }

        try
        {
            _logger.LogInformation("Reading file from path: {FilePath}", filePath);
            byte[] fileContent = await File.ReadAllBytesAsync(filePath, cancellationToken);

            _logger.LogInformation("Successfully read file for DocumentId: {DocumentId}", request.DocumentId);

            return new DownloadDocumentVm
            {
                FileContent = fileContent,
                FileName = document.NomDocument,
                ContentType = document.TypeFichier,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while reading file for DocumentId: {DocumentId} at path: {FilePath}", request.DocumentId, filePath);
            throw new Exception("An error occurred while reading the file.", ex);
        }
    }
}
