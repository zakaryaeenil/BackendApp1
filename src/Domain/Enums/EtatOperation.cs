namespace NejPortalBackend.Domain.Enums;

public enum EtatOperation
{
    depotDossier = 0,
    enCours = 1,
    traiter = 2,
    pesage = 3,
    visite = 4,
    envoiValeur = 5,
    liquidation = 6,
    sousReserveCautionBancaire = 7,
    sousResereProductionDocuments = 8,
    mainLevee = 9,
    cloture = 10, //après complétude des documents et confirmation du role necessaire
}
