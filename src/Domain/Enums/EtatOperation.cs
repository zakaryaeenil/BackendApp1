namespace NejPortalBackend.Domain.Enums;

public enum EtatOperation
{
        depotDossier = 10,
        validationDocuments = 11,
        reserve = 0, //apres decaration insertion automatique
        ouvert = 1,
        enCours = 2,
        traitementDeValeur = 3, // Visite Physique / Visite Intégrale / Visite admis conforme 
        liquidation = 4,
        attentePaimement = 5,
        mainLevee = 6,
        enCoursFacturation = 7, // condition de toutes les données récupérées 
        cloture = 8, //après complétude des documents et confirmation du role necessaire
}
