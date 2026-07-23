using System.ComponentModel.DataAnnotations;

namespace AssuranceApp.Models;

public enum TypeAuteurReclamation
{
    Assure,
    Assureur,
    Tiers
}

public enum CanalReclamation
{
    Agence,
    Telephone,
    Email,
    Chatbot,
    Courrier
}

public enum PrioriteReclamation
{
    Basse,
    Normale,
    Haute,
    Urgente
}

public enum StatutReclamation
{
    Ouverte,
    EnCours,
    
    Resolue,
    
    Cloturee
}

public class Reclamation
{
    [Key]
    public int IdReclamation { get; set; }

    // Généré côté serveur au moment de la soumission finale, ex: REC-2026-000123
    public string NumeroReclamation { get; set; } = string.Empty;

    // Identifie le client qui a soumis la réclamation (même clé que ClientRecords/ApplicationUsers)
    public string NumeroPermis { get; set; } = string.Empty;

    // ── Auteur ───────────────────────────────────────────────────────────
    public TypeAuteurReclamation TypeAuteur { get; set; } = TypeAuteurReclamation.Assure;

    // Un seul des deux doit être renseigné (cf. contrainte métier chk_reclamation_auteur)
    public int? IdAssure { get; set; }
    public int? IdCompagnie { get; set; }

    // ── Rattachement optionnel à un sinistre / contrat existant ─────────────
    // Le client donne un numéro de police ou une immatriculation ; l'agent
    // résout ces champs en interrogeant Contrats/Vehicules/Sinistres plutôt
    // que de redemander des infos déjà connues (usage, garanties souscrites, etc.)
    public int? IdSinistre { get; set; }
    public string? NumeroSinistre { get; set; }

    public int? IdContrat { get; set; }
    public string? NumeroPolice { get; set; }

    // ── Contenu collecté par l'agent ─────────────────────────────────────
    // Objet : catégorie du motif de réclamation (liste fermée côté agent IA,
    // voir ReclamationAgentTools) — reste string ici pour rester compatible
    // avec le formulaire manuel existant (Views/Reclamation/Create.cshtml).
    public string Objet { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Depuis quand le problème existe (ex: "depuis 3 mois", ou une date) —
    // PAS une date d'incident : une réclamation porte sur un dossier déjà
    // existant, pas sur un nouveau sinistre à déclarer.
    public string? DateProblemeDepuis { get; set; }

    // Le client a-t-il déjà contacté son agence/conseiller à ce sujet ?
    public string? DemarchesDejaEntreprises { get; set; }

    // Ce que le client attend comme résolution (remboursement, réexamen, etc.)
    public string? ResultatSouhaite { get; set; }

    public CanalReclamation Canal { get; set; } = CanalReclamation.Chatbot;
    public PrioriteReclamation Priorite { get; set; } = PrioriteReclamation.Normale;
    public StatutReclamation Statut { get; set; } = StatutReclamation.Ouverte;

    // ── Pièces jointes (mappé en JSON dans Reclamations.documents) ──────────
    [System.Text.Json.Serialization.JsonIgnore]
    public List<DocumentReclamation> Documents { get; set; } = new();

    // ── Suivi / résolution ───────────────────────────────────────────────
    public DateTime DateSoumission { get; set; } = DateTime.UtcNow;
    public DateTime? DateResolution { get; set; }
    public string? CommentaireResolution { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class DocumentReclamation
{
    public int Id { get; set; }
    public string TypeDocument { get; set; } = string.Empty; // "carte grise", "PV police", "photo", "devis"...
    public string NomFichier { get; set; } = string.Empty;
    public string CheminFichier { get; set; } = string.Empty;
    public DateTime DateUpload { get; set; } = DateTime.UtcNow;
    public bool Verifie { get; set; } = false;
}