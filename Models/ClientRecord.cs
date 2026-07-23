namespace AssuranceApp.Models;

public class ClientRecord
{
    public int Id { get; set; }

    public string NumeroContrat { get; set; } = string.Empty;

    public string NumeroSinistre { get; set; } = string.Empty;

    public string DateSurvenance { get; set; } = string.Empty;

    public string Immatriculation { get; set; } = string.Empty;

    public string Usage { get; set; } = string.Empty;

    public string DateDebutEffet { get; set; } = string.Empty;

    public string DateFinEffet { get; set; } = string.Empty;

    public string NumeroPermis { get; set; } = string.Empty;

    public string GarantiesSouscrites { get; set; } = string.Empty;
}
