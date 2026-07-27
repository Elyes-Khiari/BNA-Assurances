using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssuranceApp.Migrations
{
    /// <inheritdoc />
    public partial class AddResetCodeToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateProblemeDepuis",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "DemarchesDejaEntreprises",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "IdAssure",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "IdCompagnie",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "IdContrat",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "IdSinistre",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "ResultatSouhaite",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "TypeAuteur",
                table: "Reclamations");

            migrationBuilder.AddColumn<string>(
                name: "ResetCode",
                table: "ApplicationUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetCodeExpires",
                table: "ApplicationUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResetCode",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "ResetCodeExpires",
                table: "ApplicationUsers");

            migrationBuilder.AddColumn<string>(
                name: "DateProblemeDepuis",
                table: "Reclamations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DemarchesDejaEntreprises",
                table: "Reclamations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdAssure",
                table: "Reclamations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdCompagnie",
                table: "Reclamations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdContrat",
                table: "Reclamations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdSinistre",
                table: "Reclamations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultatSouhaite",
                table: "Reclamations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TypeAuteur",
                table: "Reclamations",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
