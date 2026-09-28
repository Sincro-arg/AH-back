using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AH.Api.Migrations
{
    /// <inheritdoc />
    // Migracion de SOLO DATOS (no toca el esquema): en produccion (Render)
    // quedaron pozos sembrados en una corrida anterior del codigo, cuando
    // DbSeeder.cs todavia usaba https://cdn.example.com/pozos/*.jpg como
    // ImagenUrl de ejemplo. Ese dominio no resuelve (ERR_NAME_NOT_RESOLVED),
    // asi que /pozos y /mis-inversiones publicados muestran el icono de
    // fallback en vez de una foto. Como la siembra es ADITIVA (solo agrega
    // pozos que faltan por Titulo, nunca modifica los que ya existen), el
    // codigo actual -que ya usa https://picsum.photos/seed/<slug>/400/300-
    // nunca corrigio esas filas viejas. Este UPDATE las corrige puntualmente
    // por Titulo, solo donde la ImagenUrl actual todavia es la del dominio
    // roto (para no pisar una imagen distinta que haya cargado un usuario a
    // mano), y no toca ningun otro campo.
    public partial class CorregirImagenUrlPozosRotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Pozos" AS p
                SET "ImagenUrl" = v."ImagenUrl"
                FROM (VALUES
                    ('VW Gol Trend 2019', 'https://picsum.photos/seed/vw-gol-trend-2019/400/300'),
                    ('Fiat Cronos 2021', 'https://picsum.photos/seed/fiat-cronos-2021/400/300'),
                    ('Toyota Corolla 2018', 'https://picsum.photos/seed/toyota-corolla-2018/400/300'),
                    ('Ford Focus 2016', 'https://picsum.photos/seed/ford-focus-2016/400/300'),
                    ('Chevrolet Onix 2020', 'https://picsum.photos/seed/chevrolet-onix-2020/400/300'),
                    ('Renault Sandero Stepway 2017', 'https://picsum.photos/seed/renault-sandero-stepway-2017/400/300'),
                    ('Peugeot 208 2019', 'https://picsum.photos/seed/peugeot-208-2019/400/300'),
                    ('Honda Civic 2020', 'https://picsum.photos/seed/honda-civic-2020/400/300'),
                    ('Volkswagen Vento 2018', 'https://picsum.photos/seed/vw-vento-2018/400/300'),
                    ('Fiat Toro 2021', 'https://picsum.photos/seed/fiat-toro-2021/400/300'),
                    ('Renault Kangoo 2020', 'https://picsum.photos/seed/renault-kangoo-2020/400/300'),
                    ('Jeep Renegade 2019', 'https://picsum.photos/seed/jeep-renegade-2019/400/300'),
                    ('Nissan Kicks 2021', 'https://picsum.photos/seed/nissan-kicks-2021/400/300'),
                    ('Ford Ka 2017', 'https://picsum.photos/seed/ford-ka-2017/400/300'),
                    ('Toyota Hilux 2019', 'https://picsum.photos/seed/toyota-hilux-2019/400/300'),
                    ('Chevrolet Tracker 2022', 'https://picsum.photos/seed/chevrolet-tracker-2022/400/300'),
                    ('Citroen C4 Cactus 2020', 'https://picsum.photos/seed/citroen-c4-cactus-2020/400/300'),
                    ('Peugeot 308 2015', 'https://picsum.photos/seed/peugeot-308-2015/400/300'),
                    ('Volkswagen Suran 2016', 'https://picsum.photos/seed/vw-suran-2016/400/300'),
                    ('Fiat Argo 2019', 'https://picsum.photos/seed/fiat-argo-2019/400/300'),
                    ('Renault Duster 2018', 'https://picsum.photos/seed/renault-duster-2018/400/300'),
                    ('Chevrolet Cruze 2017', 'https://picsum.photos/seed/chevrolet-cruze-2017/400/300'),
                    ('Ford EcoSport 2018', 'https://picsum.photos/seed/ford-ecosport-2018/400/300'),
                    ('Toyota Etios 2020', 'https://picsum.photos/seed/toyota-etios-2020/400/300'),
                    ('Volkswagen Polo 2022', 'https://picsum.photos/seed/vw-polo-2022/400/300'),
                    ('Peugeot 3008 2019', 'https://picsum.photos/seed/peugeot-3008-2019/400/300'),
                    ('Nissan Versa 2021', 'https://picsum.photos/seed/nissan-versa-2021/400/300'),
                    ('Citroen C3 2018', 'https://picsum.photos/seed/citroen-c3-2018/400/300'),
                    ('Honda HR-V 2020', 'https://picsum.photos/seed/honda-hrv-2020/400/300'),
                    ('Jeep Compass 2018', 'https://picsum.photos/seed/jeep-compass-2018/400/300'),
                    ('Renault Logan 2016', 'https://picsum.photos/seed/renault-logan-2016/400/300')
                ) AS v("Titulo", "ImagenUrl")
                WHERE p."Titulo" = v."Titulo"
                  AND p."ImagenUrl" LIKE 'https://cdn.example.com/%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sin Down: es una correccion de datos rotos (URL que no resuelve),
            // no hay un valor anterior valido al que "revertir".
        }
    }
}
