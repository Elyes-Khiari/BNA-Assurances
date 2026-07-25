namespace AssuranceApp.Services;

using System;
using System.Collections.Generic;
using System.Linq;

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
    // Valeurs vérifiées contre le document "SYSTÈME BONUS MALUS" (mise en vigueur 01/04/2007).
    private static readonly Dictionary<int, decimal> BonusMalusAutresUsages = new()
    {
        [1] = 0.80m, [2] = 0.90m, [3] = 1.00m, [4] = 1.20m,
        [5] = 1.50m, [6] = 1.70m, [7] = 2.00m, [8] = 2.00m
        // Pas de classe 8+ dans ce barème — voir ResoudreClasseBonusMalus pour le cas "novice".
    };

    private static readonly Dictionary<int, decimal> BonusMalusUsageAffaire = new()
    {
        [1] = 0.70m, [2] = 0.80m, [3] = 0.90m, [4] = 1.00m, [5] = 1.20m,
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

        // Situation du client, utilisée pour DÉDUIRE automatiquement la classe
        // bonus-malus plutôt que de faire confiance à une valeur donnée à l'aveugle :
        // - "classe_connue"            -> on utilise ClasseBonusMalus tel que fourni
        // - "novice_ou_resilie_2ans"    -> Classe 8 (privé) / Classe 5 (affaire) — RÈGLE OBLIGATOIRE
        // - "deuxieme_vehicule"         -> Classe 4 (privé) / Classe 3 (affaire)
        // - "voiture_fonction"          -> Classe 4
        public string SituationClient { get; init; } = "classe_connue";
        public int ClasseBonusMalus { get; init; } = 4; // utilisé seulement si SituationClient = "classe_connue"

        public decimal ValeurVenale { get; init; }
        public decimal ValeurCatalogue { get; init; }
        public List<string> GarantiesSouhaitees { get; init; } = new();
        public int NiveauFranchiseDommages { get; init; } = 0;
    }

    public record DevisResultat
    {
        public Dictionary<string, decimal> DetailParGarantie { get; init; } = new();
        public decimal Total { get; init; }
        public int ClasseBonusMalusAppliquee { get; init; }
        public List<string> Avertissements { get; init; } = new();
    }

    // Permet de surcharger les grilles tarifaires (ex: mise à jour annuelle) sans
    // recompiler le code. Passé en optionnel — si null, les valeurs vérifiées
    // ci-dessus (documents AMI Assurances Sfax 2022) sont utilisées.
    public class DevisPricingConfig
    {
        public Dictionary<string, decimal>? BonusMalusAutresUsages { get; set; }
        public Dictionary<string, decimal>? BonusMalusUsageAffaire { get; set; }
        public Dictionary<string, FranchiseConfig>? FranchiseDommagesVehicule { get; set; }
        public decimal? VolFixe { get; set; }
        public decimal? VolMultiplicateurPourMille { get; set; }
        public decimal? IncendieFixe { get; set; }
        public decimal? IncendieMultiplicateurPourMille { get; set; }
        public decimal? DefenseRecours { get; set; }
        public decimal? DommagesCollisionPourcentage { get; set; }
        public decimal? BrisGlacePourcentage { get; set; }
        public decimal? AssistanceGold { get; set; }
        public decimal? AccessoirePolice { get; set; }
    }

    public class FranchiseConfig
    {
        public decimal PrimeBase { get; set; }
        public decimal SurprimePourMille { get; set; }
    }

    // Déduit la classe bonus-malus réelle à appliquer selon la situation du client —
    // c'est ici, en code, que la règle du document tarifaire est appliquée, pas
    // laissée à la discrétion du modèle.
    private static int ResoudreClasseBonusMalus(DevisRequest req, List<string> avertissements)
    {
        switch (req.SituationClient)
        {
            case "novice_ou_resilie_2ans":
                return req.Usage == "affaire" ? 5 : 8;

            case "deuxieme_vehicule":
                return req.Usage == "affaire" ? 3 : 4;

            case "voiture_fonction":
                return 4;

            case "classe_connue":
                return req.ClasseBonusMalus;

            default:
                avertissements.Add($"situation_client '{req.SituationClient}' inconnue — classe 4 (100%) appliquée par défaut.");
                return 4;
        }
    }

    public static DevisResultat Calculer(DevisRequest req, DevisPricingConfig? config = null)
    {
        if (req.PuissanceFiscale <= 0)
            throw new ArgumentException("La puissance fiscale doit être supérieure à 0.");

        var requiresVenale = req.GarantiesSouhaitees.Any(g => g == "vol" || g == "incendie" || g == "dommages_collision" || g == "bris_glace");
        if (requiresVenale && req.ValeurVenale <= 0)
            throw new ArgumentException("La valeur vénale estimée doit être supérieure à 0 pour calculer ces garanties (Vol, Incendie, Dommages collision, Bris de glace).");

        if (req.GarantiesSouhaitees.Contains("dommages_vehicule") && req.ValeurCatalogue <= 0)
            throw new ArgumentException("La valeur catalogue estimée (à l'état neuf) doit être supérieure à 0 pour la garantie Dommages au véhicule.");

        var detail = new Dictionary<string, decimal>();
        var avertissements = new List<string>();

        // ── Classe bonus-malus : résolue automatiquement selon la situation ────
        var classeAppliquee = ResoudreClasseBonusMalus(req, avertissements);

        var confBmTable = req.Usage == "affaire" ? config?.BonusMalusUsageAffaire : config?.BonusMalusAutresUsages;
        var fallbackBmTable = req.Usage == "affaire" ? BonusMalusUsageAffaire : BonusMalusAutresUsages;

        decimal pourcentage;
        if (confBmTable != null && confBmTable.TryGetValue(classeAppliquee.ToString(), out var confVal))
        {
            pourcentage = confVal;
        }
        else if (fallbackBmTable.TryGetValue(classeAppliquee, out var defVal))
        {
            pourcentage = defVal;
        }
        else
        {
            // Ne devrait arriver que si situation_client = "classe_connue" avec une classe hors barème.
            throw new ArgumentException($"Classe bonus-malus invalide ({classeAppliquee}) pour l'usage '{req.Usage}'.");
        }

        var baseRC = TarifBaseRC(req.PuissanceFiscale);
        detail["Responsabilité Civile"] = Math.Round(baseRC * pourcentage, 3);

        foreach (var garantie in req.GarantiesSouhaitees)
        {
            switch (garantie)
            {
                case "vol":
                    // 30,000 DT prime de base + 2,6‰ de la valeur vénale
                    detail["Vol"] = Math.Round(
                        (config?.VolFixe ?? 30.000m) + ((config?.VolMultiplicateurPourMille ?? 2.6m) / 1000m) * req.ValeurVenale, 3);
                    break;

                case "incendie":
                    // 30,000 DT prime de base + 3‰ de la valeur vénale
                    detail["Incendie"] = Math.Round(
                        (config?.IncendieFixe ?? 30.000m) + ((config?.IncendieMultiplicateurPourMille ?? 3.0m) / 1000m) * req.ValeurVenale, 3);
                    break;

                case "defense_recours":
                    detail["Défense et recours"] = config?.DefenseRecours ?? 20.000m;
                    break;

                case "dommages_vehicule":
                    string franchiseStr = req.NiveauFranchiseDommages.ToString();
                    if (config?.FranchiseDommagesVehicule != null && config.FranchiseDommagesVehicule.TryGetValue(franchiseStr, out var fc))
                    {
                        detail["Dommages subis par le véhicule"] =
                            Math.Round(fc.PrimeBase + (fc.SurprimePourMille / 1000m) * req.ValeurCatalogue, 3);
                    }
                    else if (FranchiseDommagesVehicule.TryGetValue(req.NiveauFranchiseDommages, out var f))
                    {
                        // Chaque niveau de franchise a SA PROPRE surprime — jamais une valeur fixe.
                        detail["Dommages subis par le véhicule"] =
                            Math.Round(f.primeBase + (f.surprimePourMille / 1000m) * req.ValeurCatalogue, 3);
                    }
                    else
                    {
                        avertissements.Add($"Niveau de franchise {req.NiveauFranchiseDommages}% invalide pour 'dommages_vehicule' — garantie ignorée. Valeurs valides : 0,1,2,4,8,12,16,20.");
                    }
                    break;

                case "dommages_collision":
                    // 7% du capital (valeur vénale)
                    detail["Dommages collision"] = Math.Round((config?.DommagesCollisionPourcentage ?? 0.07m) * req.ValeurVenale, 3);
                    break;

                case "bris_glace":
                    // 5% du capital — AUCUNE partie fixe
                    detail["Bris de glace"] = Math.Round((config?.BrisGlacePourcentage ?? 0.05m) * req.ValeurVenale, 3);
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
            ClasseBonusMalusAppliquee = classeAppliquee,
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
                description = "Calcule une estimation de prime annuelle pour un contrat auto, à partir " +
                               "de formules tarifaires réelles (pas une estimation approximative par l'IA). " +
                               "IMPORTANT : ne demande JAMAIS directement 'quelle est votre classe bonus-malus' " +
                               "à un nouveau client — demande plutôt sa situation via situation_client " +
                               "(nouveau conducteur/contrat résilié depuis +2 ans, 2ème véhicule, voiture de " +
                               "fonction, ou classe déjà connue d'un contrat en cours) : la classe réelle est " +
                               "déduite automatiquement par le serveur selon les règles officielles, jamais " +
                               "par l'IA elle-même.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        puissance_fiscale = new { type = "integer", description = "Puissance fiscale du véhicule en CV (ex: 5)" },
                        usage = new { type = "string", @enum = new[] { "prive", "affaire" }, description = "Usage du véhicule" },
                        situation_client = new
                        {
                            type = "string",
                            @enum = new[] { "novice_ou_resilie_2ans", "deuxieme_vehicule", "voiture_fonction", "classe_connue" },
                            description = "Situation utilisée pour déduire la classe bonus-malus. 'classe_connue' si le client connaît déjà sa classe actuelle (contrat en cours ou reconduction)."
                        },
                        classe_bonus_malus = new { type = "integer", description = "Classe bonus-malus actuelle du client — UNIQUEMENT si situation_client = 'classe_connue'" },
                        valeur_venale = new { type = "number", description = "Valeur vénale actuelle estimée du véhicule en DT (ex: 35000)" },
                        valeur_catalogue = new { type = "number", description = "Valeur catalogue à l'état neuf estimée en DT (ex: 45000)" },
                        garanties_souhaitees = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "string",
                                @enum = new[] { "vol", "incendie", "defense_recours", "dommages_vehicule", "dommages_collision", "bris_glace", "assistance_gold", "accessoire_police" }
                            },
                            description = "Garanties facultatives souhaitées en plus de la RC (toujours incluse)"
                        },
                        niveau_franchise_dommages = new
                        {
                            type = "integer",
                            @enum = new[] { 0, 1, 2, 4, 8, 12, 16, 20 },
                            description = "Niveau de franchise en % — uniquement si 'dommages_vehicule' est demandée. Défaut 0."
                        }
                    },
                    required = new[] { "puissance_fiscale", "usage", "situation_client" }
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