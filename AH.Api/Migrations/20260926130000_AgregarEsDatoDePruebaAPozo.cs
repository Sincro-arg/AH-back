using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AH.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEsDatoDePruebaAPozo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsDatoDePrueba",
                table: "Pozos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Los pozos huerfanos que dejo RealDeploymentE2ETests corriendo contra
            // el deployment real (se ven en /pozos con el guid como nombre) quedan
            // marcados como dato de prueba para que el listado publico los excluya.
            migrationBuilder.Sql(
                "UPDATE \"Pozos\" SET \"EsDatoDePrueba\" = true WHERE \"Titulo\" LIKE 'Pozo E2E%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsDatoDePrueba",
                table: "Pozos");
        }
    }
}
