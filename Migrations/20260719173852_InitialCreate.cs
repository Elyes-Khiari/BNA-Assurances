using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssuranceApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    NumeroPermis = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroContrat = table.Column<string>(type: "text", nullable: false),
                    NumeroSinistre = table.Column<string>(type: "text", nullable: false),
                    DateSurvenance = table.Column<string>(type: "text", nullable: false),
                    Immatriculation = table.Column<string>(type: "text", nullable: false),
                    Usage = table.Column<string>(type: "text", nullable: false),
                    DateDebutEffet = table.Column<string>(type: "text", nullable: false),
                    DateFinEffet = table.Column<string>(type: "text", nullable: false),
                    NumeroPermis = table.Column<string>(type: "text", nullable: false),
                    GarantiesSouscrites = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reclamations",
                columns: table => new
                {
                    IdReclamation = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroReclamation = table.Column<string>(type: "text", nullable: false),
                    NumeroPermis = table.Column<string>(type: "text", nullable: false),
                    TypeAuteur = table.Column<int>(type: "integer", nullable: false),
                    IdAssure = table.Column<int>(type: "integer", nullable: true),
                    IdCompagnie = table.Column<int>(type: "integer", nullable: true),
                    IdSinistre = table.Column<int>(type: "integer", nullable: true),
                    NumeroSinistre = table.Column<string>(type: "text", nullable: true),
                    IdContrat = table.Column<int>(type: "integer", nullable: true),
                    NumeroPolice = table.Column<string>(type: "text", nullable: true),
                    Objet = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DateProblemeDepuis = table.Column<string>(type: "text", nullable: true),
                    DemarchesDejaEntreprises = table.Column<string>(type: "text", nullable: true),
                    ResultatSouhaite = table.Column<string>(type: "text", nullable: true),
                    Canal = table.Column<int>(type: "integer", nullable: false),
                    Priorite = table.Column<int>(type: "integer", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    DateSoumission = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateResolution = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CommentaireResolution = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reclamations", x => x.IdReclamation);
                });

            migrationBuilder.CreateTable(
                name: "DocumentReclamation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TypeDocument = table.Column<string>(type: "text", nullable: false),
                    NomFichier = table.Column<string>(type: "text", nullable: false),
                    CheminFichier = table.Column<string>(type: "text", nullable: false),
                    DateUpload = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Verifie = table.Column<bool>(type: "boolean", nullable: false),
                    ReclamationIdReclamation = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentReclamation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentReclamation_Reclamations_ReclamationIdReclamation",
                        column: x => x.ReclamationIdReclamation,
                        principalTable: "Reclamations",
                        principalColumn: "IdReclamation");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentReclamation_ReclamationIdReclamation",
                table: "DocumentReclamation",
                column: "ReclamationIdReclamation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationUsers");

            migrationBuilder.DropTable(
                name: "ClientRecords");

            migrationBuilder.DropTable(
                name: "DocumentReclamation");

            migrationBuilder.DropTable(
                name: "Reclamations");
        }
    }
}
