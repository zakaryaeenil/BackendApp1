namespace NejPortalBackend.Domain.Entities;

public class Operation : BaseAuditableEntity
{
    public required string UserId { get; set; }


    public string? Bureau { get; set; }
    public string? CodeDossier { get; set; }
    public string? Regime { get; set; }

    private  bool _estReserver;
    public required bool EstReserver
    {
        get => _estReserver;
        set
        {
        
            _estReserver = value;
        }
    }
    public string? ReserverPar { get; set; }

    public required OperationPriorite OperationPriorite { get; set; }
    public required TypeOperation TypeOperation { get; set; } 
    public required EtatOperation EtatOperation { get; set; }

    public bool TR { get; set; } 
    public bool DEBOURS { get; set; }
    public bool CONFIRMATION_DEDOUANEMENT { get; set; }
    public ICollection<Document> Documents { get;  set; } = new List<Document>();
    public ICollection<Commentaire> Commentaires { get;  set; } = new List<Commentaire>();
    public ICollection<Historique> Historiques { get;  set; } = new List<Historique>();
}
