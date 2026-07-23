using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace BNA_Assurances.Services;

public static class DevisPdfGenerator
{
    static DevisPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] GeneratePdf(JsonElement row)
    {
        // Extraction des données
        var puissanceFiscale = row.GetProperty("puissance_fiscale").GetInt32();
        var usage = row.GetProperty("usage").GetString() ?? "prive";
        var classe = row.GetProperty("classe_bonus_malus").GetInt32();
        var valeurVenale = row.GetProperty("valeur_venale").GetDecimal();
        var valeurCatalogue = row.GetProperty("valeur_catalogue").GetDecimal();
        var total = row.GetProperty("total_estime_dt").GetDecimal();
        
        var details = row.GetProperty("detail_json");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Element(ComposeHeader);
                page.Content().Element(x => ComposeContent(x, puissanceFiscale, usage, classe, valeurVenale, valeurCatalogue, total, details));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("DEVIS ASSURANCE AUTO").FontSize(20).SemiBold().FontColor("#2b9f62");
                column.Item().Text("BNA Assurances").FontSize(14).FontColor("#0e2a47");
                column.Item().Text($"Émis le {System.DateTime.Now:dd/MM/yyyy à HH:mm}").FontSize(10).FontColor(Colors.Grey.Medium);
            });

            // Injection du logo
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo_bna.png");
            if (File.Exists(logoPath))
            {
                row.ConstantItem(100).Image(logoPath);
            }
        });
    }

    private static void ComposeContent(IContainer container, int pf, string usage, int classe, decimal vv, decimal vc, decimal total, JsonElement details)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            // Informations Véhicule
            column.Item().PaddingBottom(10).Text("1. Informations du Véhicule et Conducteur").FontSize(14).SemiBold().FontColor("#0e2a47");
            
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell().Text($"Puissance Fiscale : {pf} CV");
                table.Cell().Text($"Usage : {(usage.ToLower().Contains("prive") ? "Privé" : "Professionnel")}");
                
                table.Cell().Text($"Classe Bonus-Malus : {classe}");
                table.Cell().Text("");

                table.Cell().Text($"Valeur Vénale : {vv:N3} DT");
                table.Cell().Text($"Valeur Catalogue : {vc:N3} DT");
            });

            column.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            // Tableau des garanties
            column.Item().PaddingBottom(10).Text("2. Détail des Garanties et Primes").FontSize(14).SemiBold().FontColor("#0e2a47");

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                });

                // Header
                table.Cell().BorderBottom(1).BorderColor("#2b9f62").PaddingBottom(5).Text("Garantie").SemiBold();
                table.Cell().BorderBottom(1).BorderColor("#2b9f62").PaddingBottom(5).AlignRight().Text("Prime (DT)").SemiBold();

                // Rows
                foreach (var prop in details.EnumerateObject())
                {
                    table.Cell().PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Text(prop.Name);
                    table.Cell().PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).AlignRight().Text($"{prop.Value.GetDecimal():N3}");
                }

                // Total
                table.Cell().PaddingTop(10).Text("TOTAL ANNUEL ESTIMÉ").SemiBold().FontSize(12).FontColor("#0e2a47");
                table.Cell().PaddingTop(10).AlignRight().Text($"{total:N3} DT").SemiBold().FontSize(14).FontColor("#2b9f62");
            });
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().PaddingTop(5).Text("Ce devis est une estimation indicative non contractuelle, générée automatiquement.").FontSize(9).FontColor(Colors.Grey.Medium);
            column.Item().Text("Il doit être validé et confirmé par un agent de BNA Assurances.").FontSize(9).FontColor(Colors.Grey.Medium);
            column.Item().Text(text =>
            {
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" sur ");
                text.TotalPages();
            });
        });
    }
}
