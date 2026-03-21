using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeudoresApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Deudores",
                columns: table => new
                {
                    NroIdentificacion = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    SituacionMaxima = table.Column<int>(type: "integer", nullable: false),
                    SumaTotalPrestamos = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deudores", x => x.NroIdentificacion);
                });

            migrationBuilder.CreateTable(
                name: "Entidades",
                columns: table => new
                {
                    CodigoEntidad = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    SumaTotalPrestamos = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entidades", x => x.CodigoEntidad);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Deudores");

            migrationBuilder.DropTable(
                name: "Entidades");
        }
    }
}
