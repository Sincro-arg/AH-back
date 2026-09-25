using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AH.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarInversiones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Inversiones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PozoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inversiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inversiones_Pozos_PozoId",
                        column: x => x.PozoId,
                        principalTable: "Pozos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inversiones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inversiones_PozoId",
                table: "Inversiones",
                column: "PozoId");

            migrationBuilder.CreateIndex(
                name: "IX_Inversiones_UsuarioId",
                table: "Inversiones",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inversiones");
        }
    }
}
