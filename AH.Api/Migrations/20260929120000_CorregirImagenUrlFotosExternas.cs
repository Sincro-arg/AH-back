using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AH.Api.Migrations
{
    /// <inheritdoc />
    // Migracion de SOLO DATOS (no toca el esquema): en produccion (Render)
    // quedaron pozos con ImagenUrl apuntando a https://picsum.photos/seed/...,
    // heredado de una version anterior de DbSeeder.cs (ver el comentario en
    // linea 17 de ese archivo) que usaba ese servicio de fotos random como
    // placeholder. Esas fotos no tienen ninguna relacion con autos (paisajes,
    // gente, ciudades), mezcladas en la misma grilla con la ilustracion
    // /imagenes/auto.jpg que usan los pozos sembrados mas nuevos. Como la
    // siembra es ADITIVA (solo agrega pozos que faltan por Titulo, nunca
    // modifica los que ya existen), el codigo actual -que ya usa el
    // placeholder propio- nunca corrigio esas filas viejas. Este UPDATE las
    // corrige puntualmente, solo donde la ImagenUrl actual todavia apunta a
    // picsum.photos (para no pisar una imagen distinta que haya cargado un
    // usuario a mano), y no toca ningun otro campo.
    public partial class CorregirImagenUrlFotosExternas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Pozos"
                SET "ImagenUrl" = '/imagenes/auto.jpg'
                WHERE "ImagenUrl" LIKE 'https://picsum.photos/%'
                   OR "ImagenUrl" LIKE 'http://picsum.photos/%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sin Down: es una correccion de datos rotos (fotos externas sin
            // relacion con autos), no hay un valor anterior valido al que
            // "revertir".
        }
    }
}
