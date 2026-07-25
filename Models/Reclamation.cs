using System.ComponentModel.DataAnnotations;

namespace AssuranceApp.Models;



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

    // ── Rattachement optionnel à un sinistre / contrat existant ─────────────
    public string? NumeroSinistre { get; set; }
    public string? NumeroPolice { get; set; }

    // ── Contenu collecté par l'agent ─────────────────────────────────────
    public string Objet { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

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