﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Operations.Commands.CreateOperation;

[Authorize(Roles = Roles.AdminAndAgent)]
public record CreateOperationCommand : IRequest<int>
{
    public required string ClientId { get; init; }
    public string? AgentId { get; init; }
    public int TypeOperationId { get; init; }
    public string? Commentaire { get; init; }
    public IEnumerable<IFormFile>? Files { get; init; }

}

public class CreateOperationCommandValidator : AbstractValidator<CreateOperationCommand>
{
    public CreateOperationCommandValidator()
    {
        RuleFor(v => v.TypeOperationId)
                .NotNull().WithMessage("Type is required.");
        RuleFor(v => v.ClientId).NotEmpty()
                .NotNull().WithMessage("Client Idantifiant required.");
    }
}

public class CreateOperationCommandHandler : IRequestHandler<CreateOperationCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;
    private readonly IFileService _fileService;
    private readonly ILogger<CreateOperationCommandHandler> _logger;

    public CreateOperationCommandHandler(IApplicationDbContext context, IUser currentUserService, IIdentityService identityService, IFileService fileService, ILogger<CreateOperationCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _fileService = fileService;
        _logger = logger;
    }

    public async Task<int> Handle(CreateOperationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to process CreateOperationCommand for user: {ClientId}", request.ClientId);

        // Begin a transaction
        using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            if (string.IsNullOrWhiteSpace(_currentUserService.Id))
                throw new UnauthorizedAccessException();

            // Validate TypeOperation
            bool isValidTypeOperation = Enum.IsDefined(typeof(TypeOperation), request.TypeOperationId);


            if (!isValidTypeOperation)
            {
                _logger.LogWarning("Invalid TypeOperation value: {TypeOperationId}", request.TypeOperationId);
                throw new InvalidOperationException("Invalid TypeOperation value.");
            }

            // Validate client role
            if (string.IsNullOrWhiteSpace(request.ClientId) || !await _identityService.IsInRoleAsync(request.ClientId, Roles.Client))
            {
                _logger.LogWarning("Invalid Client Id value: {ClientId}", request.ClientId);
                throw new InvalidOperationException("Invalid Client Id value.");
            }
            // Validate agent role
            if (!string.IsNullOrWhiteSpace(request.AgentId) && !await _identityService.IsInRoleAsync(request.AgentId, Roles.Agent))
            {
                _logger.LogWarning("Invalid Agent Id value: {AgentId}", request.AgentId);
                throw new InvalidOperationException("Invalid Agent Id value.");
            }

            var clientUsername = await _identityService.GetUserNameAsync(request.ClientId);
            if (string.IsNullOrWhiteSpace(clientUsername))
            {
                _logger.LogWarning("Invalid Client UserName value: {UserName}", _currentUserService.Id);
                throw new InvalidOperationException("Invalid Client UserName value.");
            }

            // Create and save the Operation
            var operation = new Operation
            {
                TypeOperation = (TypeOperation)request.TypeOperationId,
                EtatOperation = EtatOperation.depotDossier,
                UserId = request.ClientId,
                ReserverPar = null,
                EstReserver = false
            };
            if (!string.IsNullOrWhiteSpace(request.AgentId) && await _identityService.IsInRoleAsync(request.AgentId, Roles.Agent))
            {
                operation.ReserverPar = request.AgentId;
                operation.EstReserver = true;
            }

            await _context.Operations.AddAsync(operation, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken); // Operation gets an ID here

            var userName = _identityService.GetUserNameAsync(_currentUserService.Id);
            // Create and log the historical record for the creation
            var historique = new Historique
            {
                Action = $"L'opération numéro :"+ operation.Id +"a été criée par l'equipe" + userName + " pour le client "+request.ClientId+" : Operation a été crié avec succès.",
                UserId = _currentUserService.Id,
                OperationId = operation.Id
            };

            // Add the historique record to the database
            await _context.Historiques.AddAsync(historique, cancellationToken);
          
            // Process Files
            if (request.Files?.Count() > 0)
            {
               foreach (var file in request.Files)
                    {
                        var fileInfo = await _fileService.Create(file, "documents", clientUsername, operation.Id);

                        var document = new Document
                        {
                            NomDocument = fileInfo.NomDocument,
                            CheminFichier = fileInfo.CheminFichier,
                            TailleFichier = fileInfo.TailleFichier,
                            TypeFichier = fileInfo.TypeFichier,
                            EstAccepte = true,
                            OperationId = operation.Id
                        };

                        await _context.Documents.AddAsync(document, cancellationToken);
                        _logger.LogDebug("File added: {FileName} for OperationId: {OperationId}", fileInfo.NomDocument, operation.Id);
                    }
            }

            // Process Commentaire
            if (!string.IsNullOrWhiteSpace(request.Commentaire))
            {
                var commentaire = new Commentaire
                {
                    Message = request.Commentaire,
                    OperationId = operation.Id,
                    UserId = _currentUserService.Id
                };

                await _context.Commentaires.AddAsync(commentaire, cancellationToken);
                _logger.LogDebug("Commentaire added: {Message} for OperationId: {OperationId}", request.Commentaire, operation.Id);
            }

            // Commit the transaction
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Operation created successfully with Id: {OperationId}", operation.Id);

            return operation.Id;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError("Rollback  CreateOperationCommand processing.");
            _logger.LogError(ex, "An error occurred during CreateOperationCommand processing.");

            throw ex switch
            {
                NotFoundException or InvalidOperationException or UnauthorizedAccessException => ex,
                _ => new ApplicationException("An unexpected error occurred. Please try again later.", ex),
            };
        }
    }
}
