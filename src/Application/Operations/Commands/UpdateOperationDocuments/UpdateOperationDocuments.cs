using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Application.Operations.Commands.UpdateOperationDocuments;

[Authorize(Roles = Roles.AdminAndAgent)]
public record UpdateOperationDocumentsCommand : IRequest
{
    public required int OperationId { get; init; }

    public IEnumerable<IFormFile>? Files { get; init; }

    public IEnumerable<int>? DocumentIds { get; set; }
}

public class UpdateOperationDocumentsCommandValidator : AbstractValidator<UpdateOperationDocumentsCommand>
{
    public UpdateOperationDocumentsCommandValidator()
    {
        RuleFor(v => v.OperationId).NotEmpty()
              .NotNull().WithMessage("Operation is required.");
    }
}

public class UpdateOperationDocumentsCommandHandler : IRequestHandler<UpdateOperationDocumentsCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;
    private readonly IFileService _fileService;
    private readonly ILogger<UpdateOperationDocumentsCommandHandler> _logger;

    public UpdateOperationDocumentsCommandHandler(IApplicationDbContext context, IUser currentUserService, IIdentityService identityService, IFileService fileService, ILogger<UpdateOperationDocumentsCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _fileService = fileService;
        _logger = logger;
    }

    public async Task Handle(UpdateOperationDocumentsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to process ClientUpdateOperationDocumentsCommand for user: {UserId}, operation : {OperationId}", _currentUserService.Id, request.OperationId);

        try
        {
            if (string.IsNullOrWhiteSpace(_currentUserService.Id))
                throw new UnauthorizedAccessException();

            var entity = await _context.Operations
                    .FindAsync([request.OperationId], cancellationToken) ?? throw new NotFoundException(nameof(Operations), request.OperationId.ToString());

            if (request.Files?.Count() > 0 || request.DocumentIds?.Count() > 0)
            {
                // Validate client role
                if (!await _identityService.IsInRoleAsync(entity.UserId, Roles.Client))
                {
                    _logger.LogWarning("Invalid Client Id value: {UserId}", entity.UserId);
                    throw new InvalidOperationException("Invalid Client Id value.");
                }
                // Get the client's username
                var clientUsername = await _identityService.GetUserNameAsync(entity.UserId);
                if (!string.IsNullOrWhiteSpace(clientUsername))
                {
                    bool isUpdated = false;
                    if (request.Files?.Count() > 0)
                    {
                        foreach (var file in request.Files)
                        {
                            var fileinfo = await _fileService.Create(file, "documents", clientUsername, entity.Id);
                            var doc = new Document
                            {
                                CheminFichier = fileinfo.CheminFichier,
                                NomDocument = fileinfo.NomDocument,
                                TailleFichier = fileinfo.TailleFichier,
                                TypeFichier = fileinfo.TypeFichier,
                                EstAccepte = true,
                                OperationId = entity.Id
                            };

                            await _context.Documents.AddAsync(doc, cancellationToken);
                            _logger.LogDebug("File added: {FileName} for OperationId: {OperationId}", doc.NomDocument, entity.Id);
                        }
                          isUpdated = true;
                    }
                    if (request.DocumentIds?.Count() > 0)
                    {
                        foreach (var documentId in request.DocumentIds)
                        {
                            var doc = await _context.Documents
                            .FindAsync([documentId], cancellationToken) ?? throw new NotFoundException(nameof(DocumentDto), documentId.ToString());

                            doc.EstAccepte = !doc.EstAccepte;


                            _context.Documents.Update(doc);
                            _logger.LogDebug("File added: {FileName} for OperationId: {OperationId}", doc.NomDocument, entity.Id);

                        }
                        
                        isUpdated = true;
                    }
                    if(isUpdated)
                    {
                        // Create and log the historical record for the modification
                        var historique = new Historique
                        {
                            Action = $"L'opération numéro : {entity.Id} a été modifiée par l'equipe' {_currentUserService.Id}: Documents Operation a été modifié avec succès.",
                            UserId = _currentUserService.Id,
                            OperationId = entity.Id
                        };

                        // Add the historique record to the database
                        await _context.Historiques.AddAsync(historique, cancellationToken);

                        // Save changes to the database
                        await _context.SaveChangesAsync(cancellationToken);

                        _logger.LogInformation("Operation {OperationId} modified successfully : Details Operation a été modifié avec succès.", entity.Id);
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid Client UserName value: {Id}", _currentUserService.Id);
                    throw new InvalidOperationException("Invalid Client Id value.");
                }
            }
            else
            {
                _logger.LogInformation("No changes were made to the operation {OperationId}", entity.Id);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during ClientUpdateOperationCommentairesCommand processing.");

            throw ex switch
            {
                NotFoundException or InvalidOperationException or UnauthorizedAccessException => ex,
                _ => new ApplicationException("An unexpected error occurred. Please try again later.", ex),
            };
        }
    }
}
