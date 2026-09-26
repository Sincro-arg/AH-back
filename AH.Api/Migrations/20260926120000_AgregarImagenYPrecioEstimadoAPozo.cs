using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AH.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarImagenYPrecioEstimadoAPozo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagenUrl",
                table: "Pozos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioVentaEstimado",
                table: "Pozos",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagenUrl",
                table: "Pozos");

            migrationBuilder.DropColumn(
                name: "PrecioVentaEstimado",
                table: "Pozos");
        }
    }
}
