using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AH.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPozos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pozos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AutoDescripcion = table.Column<string>(type: "text", nullable: false),
                    MontoObjetivo = table.Column<decimal>(type: "numeric", nullable: false),
                    MontoRecaudado = table.Column<decimal>(type: "numeric", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PrecioCompra = table.Column<decimal>(type: "numeric", nullable: true),
                    FechaCompra = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PrecioVenta = table.Column<decimal>(type: "numeric", nullable: true),
                    FechaVenta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pozos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pozos");
        }
    }
}
