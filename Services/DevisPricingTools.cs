namespace AssuranceApp.Services;

/// <summary>
/// Calculateur de devis auto — 100% déterministe (grilles tarifaires réelles),
/// aucun modèle ML impliqué. Voir documents tarifaires AMI Assurances Sfax 2022.
/// </summary>
public static class DevisPricingCalculator
{
    // ── RC : tarif de base par puissance fiscale (classe 4 = 100%) ─────────
    private static decimal TarifBaseRC(int puissanceFiscale) => puissanceFiscale switch
    {
        2 => 95.000m,
        3 or 4 => 110.000m,
        5 or 6 => 145.000m,
        >= 7 and <= 10 => 181.000m,
        >= 11 and <= 14 => 210.000m,
        _ => 255.000m // 15 et plus
    };

    // ── Bonus-malus : % appliqué au tarif de base RC selon la classe ───────
    private static readonly Dictionary<int, decimal> BonusMalusAutresUsages = new()
    {
        [1] = 0.80m, [2] = 0.90m, [3] = 1.00m, [4] = 1.00m,
        [5] = 1.50m, [6] = 1.70m, [7] = 2.00m, [8] = 2.00m
    };

    private static readonly Dictionary<int, decimal> BonusMalusUsageAffaire = new()
    {
        [1] = 0.70m, [2] = 0.80m, [3] = 1.00m, [4] = 1.00m, [5] = 1.50m,
        [6] = 1.40m, [7] = 1.60m, [8] = 2.00m, [9] = 2.50m, [10] = 3.00m, [11] = 3.50m
    };

    // ── Dommages subis par le véhicule : par niveau de franchise ───────────
    // (primeBase en DT, surprime en ‰ de la valeur catalogue)
    private static readonly Dictionary<int, (decimal primeBase, decimal surprimePourMille)> FranchiseDommagesVehicule = new()
    {
        [0] = (22.000m, 32.0m),
        [1] = (21.000m, 26.5m),
        [2] = (19.000m, 21.0m),
        [4] = (15.000m, 17.0m),
        [8] = (13.000m, 13.0m),
        [12] = (9.000m, 9.0m),
        [16] = (8.000m, 7.0m),
        [20] = (5.000m, 4.0m)
    };

    public record DevisRequest
    {
        public int PuissanceFiscale { get; init; }
        public string Usage { get; init; } = "prive"; // "prive" ou "affaire"
        public int ClasseBonusMalus { get; init; } = 4; // défaut : classe 4 = 100%
        public decimal ValeurVenale { get; init; }
        public decimal ValeurCatalogue { get; init; }
        public List<string> GarantiesSouhaitees { get; init; } = new();
        public int NiveauFranchiseDommages { get; init; } = 0; // pertinent seulement si "dommages_vehicule" demandé
    }

    public record DevisResultat
    {
        public Dictionary<string, decimal> DetailParGarantie { get; init; } = new();
        public decimal Total { get; init; }
        public List<string> Avertissements { get; init; } = new();
    }

    public class DevisPricingConfig
    {
        public Dictionary<string, decimal>? BonusMalusAutresUsages { get; set; }
        public Dictionary<string, decimal>? BonusMalusUsageAffaire { get; set; }
        public Dictionary<string, FranchiseConfig>? FranchiseDommagesVehicule { get; set; }
        public decimal? VolFixe { get; set; }
        public decimal? VolMultiplicateur { get; set; }
        public decimal? IncendieFixe { get; set; }
        public decimal? IncendieMultiplicateur { get; set; }
        public decimal? DefenseRecours { get; set; }
        public decimal? DommagesCollisionMultiplicateur { get; set; }
        public decimal? BrisGlaceMultiplicateur { get; set; }
        public decimal? AssistanceGold { get; set; }
        public decimal? AccessoirePolice { get; set; }
    }

    public class FranchiseConfig
    {
        public decimal PrimeBase { get; set; }
        public decimal SurprimePourMille { get; set; }
    }

    public static DevisResultat Calculer(DevisRequest req, DevisPricingConfig? config = null)
    {
        if (req.PuissanceFiscale <= 0) 
            throw new ArgumentException("La puissance fiscale doit être supérieure à 0.");
            
        var table = req.Usage == "affaire" ? BonusMalusUsageAffaire : BonusMalusAutresUsages;
        if (!table.ContainsKey(req.ClasseBonusMalus))
            throw new ArgumentException($"Classe bonus-malus invalide ({req.ClasseBonusMalus}) pour l'usage '{req.Usage}'.");

        var requiresVenale = req.GarantiesSouhaitees.Any(g => g == "vol" || g == "incendie" || g == "dommages_collision" || g == "bris_glace");
        if (requiresVenale && req.ValeurVenale <= 0)
            throw new ArgumentException("La valeur vénale estimée doit être supérieure à 0 pour calculer ces garanties (Vol, Incendie, etc.).");

        if (req.GarantiesSouhaitees.Contains("dommages_vehicule") && req.ValeurCatalogue <= 0)
            throw new ArgumentException("La valeur catalogue estimée (à l'état neuf) doit être supérieure à 0 pour la garantie Dommages au véhicule.");

        var detail = new Dictionary<string, decimal>();
        var avertissements = new List<string>();

        // RC (obligatoire, toujours incluse)
        var baseRC = TarifBaseRC(req.PuissanceFiscale);
        
        decimal pourcentage = 1.00m;
        string classeStr = req.ClasseBonusMalus.ToString();

        if (config != null)
        {
            var confTable = req.Usage == "affaire" ? config.BonusMalusUsageAffaire : config.BonusMalusAutresUsages;
            if (confTable != null && confTable.TryGetValue(classeStr, out var val)) pourcentage = val;
            else if (req.Usage == "affaire" && BonusMalusUsageAffaire.TryGetValue(req.ClasseBonusMalus, out var defVal1)) pourcentage = defVal1;
            else if (req.Usage != "affaire" && BonusMalusAutresUsages.TryGetValue(req.ClasseBonusMalus, out var defVal2)) pourcentage = defVal2;
            else throw new ArgumentException($"Classe bonus-malus invalide ({req.ClasseBonusMalus}) pour l'usage '{req.Usage}'.");
        }
        else
        {
            var fallbackTable = req.Usage == "affaire" ? BonusMalusUsageAffaire : BonusMalusAutresUsages;
            if (fallbackTable.TryGetValue(req.ClasseBonusMalus, out var val)) pourcentage = val;
            else throw new ArgumentException($"Classe bonus-malus invalide ({req.ClasseBonusMalus}) pour l'usage '{req.Usage}'.");
        }

        detail["Responsabilité Civile"] = Math.Round(baseRC * pourcentage, 3);

        foreach (var garantie in req.GarantiesSouhaitees)
        {
            switch (garantie)
            {
                case "vol":
                    detail["Vol"] = Math.Round((config?.VolFixe ?? 30.000m) + (config?.VolMultiplicateur ?? 0.0026m) * req.ValeurVenale, 3);
                    break;

                case "incendie":
                    detail["Incendie"] = Math.Round((config?.IncendieFixe ?? 30.000m) + (config?.IncendieMultiplicateur ?? 0.003m) * req.ValeurVenale, 3);
                    break;

                case "defense_recours":
                    detail["Défense et recours"] = config?.DefenseRecours ?? 20.000m;
                    break;

                case "dommages_vehicule":
                    string franchiseStr = req.NiveauFranchiseDommages.ToString();
                    if (config?.FranchiseDommagesVehicule != null && config.FranchiseDommagesVehicule.TryGetValue(franchiseStr, out var fc))
                    {
                        detail["Dommages subis par le véhicule"] = Math.Round(fc.PrimeBase + (fc.SurprimePourMille / 1000m) * req.ValeurCatalogue, 3);
                    }
                    else if (FranchiseDommagesVehicule.TryGetValue(req.NiveauFranchiseDommages, out var f))
                    {
                        detail["Dommages subis par le véhicule"] = Math.Round(f.primeBase + (f.surprimePourMille / 1000m) * req.ValeurCatalogue, 3);
                    }
                    else
                    {
                        avertissements.Add("Niveau de franchise invalide pour 'dommages_vehicule' — garantie ignorée.");
                    }
                    break;

                case "dommages_collision":
                    detail["Dommages collision"] = Math.Round((config?.DommagesCollisionMultiplicateur ?? 0.07m) * req.ValeurVenale, 3);
                    break;

                case "bris_glace":
                    detail["Bris de glace"] = Math.Round((config?.BrisGlaceMultiplicateur ?? 0.05m) * req.ValeurVenale, 3);
                    break;

                case "assistance_gold":
                    detail["Assistance Automobile Gold"] = config?.AssistanceGold ?? 50.000m;
                    break;

                case "accessoire_police":
                    detail["Accessoire police"] = config?.AccessoirePolice ?? 40.000m;
                    break;

                default:
                    avertissements.Add($"Garantie inconnue ignorée : {garantie}");
                    break;
            }
        }

        return new DevisResultat
        {
            DetailParGarantie = detail,
            Total = detail.Values.Sum(),
            Avertissements = avertissements
        };
    }
}

/// <summary>
/// Tool schema (format Groq/OpenAI) pour exposer le calculateur de devis à l'agent.
/// </summary>
public static class DevisPricingTools
{
    public static readonly object[] Tools = new object[]
    {
        new
        {
            type = "function",
            function = new
            {
                name = "estimate_devis",
                description = "Calcule l'estimation de prime annuelle pour un contrat auto. " +
                               "ATTENTION : Tu DOIS appeler cet outil avec TOUTES les informations (puissance_fiscale, usage, classe_bonus_malus, valeur_venale, valeur_catalogue, et garanties_souhaitees).",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        puissance_fiscale = new { type = "integer", description = "Puissance fiscale du véhicule en CV (ex: 5)." },
                        usage = new { type = "string", @enum = new[] { "prive", "affaire" }, description = "Usage du véhicule ('prive' ou 'affaire')." },
                        classe_bonus_malus = new { type = "integer", description = "Classe bonus-malus du conducteur (ex: 4)." },
                        valeur_venale = new { type = "number", description = "Valeur vénale actuelle estimée en DT (ex: 35000)." },
                        valeur_catalogue = new { type = "number", description = "Valeur catalogue à l'état neuf estimée en DT (ex: 45000)." },
                        garanties_souhaitees = new { type = "array", items = new { type = "string", @enum = new[] { "vol", "incendie", "defense_recours", "dommages_vehicule", "dommages_collision", "bris_glace", "assistance_gold", "accessoire_police" } }, description = "Liste des garanties optionnelles souhaitées." },
                        niveau_franchise_dommages = new { type = "integer", @enum = new[] { 0, 1, 2, 4, 8, 12, 16, 20 }, description = "Niveau de franchise en % (0, 1, 2, 4, 8, 12, 16, 20) si 'dommages_vehicule' est demandée. Défaut à 0." }
                    },
                    required = new[] { "puissance_fiscale", "usage", "classe_bonus_malus", "valeur_venale", "valeur_catalogue" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "send_devis_email",
                description = "Envoie le devis généré en PDF par e-mail à l'adresse fournie par le client.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        email = new { type = "string", description = "L'adresse e-mail du client." },
                        devis_id = new { type = "string", description = "L'ID unique du devis (retourné par estimate_devis)." }
                    },
                    required = new[] { "email", "devis_id" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "search_car_price",
                description = "Cherche sur le web (marché tunisien) le prix actuel (valeur vénale) et le prix catalogue (valeur neuf) d'un véhicule.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        marque = new { type = "string", description = "La marque du véhicule (ex: Volkswagen)." },
                        modele = new { type = "string", description = "Le modèle exact du véhicule (ex: Golf 6)." },
                        annee = new { type = "integer", description = "L'année de mise en circulation (ex: 2011)." }
                    },
                    required = new[] { "marque", "modele", "annee" }
                }
            }
        }
    };
}