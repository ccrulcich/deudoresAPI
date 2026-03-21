using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeudoresApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeudoresIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Deudores_SituacionMaxima",
                table: "Deudores",
                column: "SituacionMaxima");

            migrationBuilder.CreateIndex(
                name: "IX_Deudores_SumaTotalPrestamos",
                table: "Deudores",
                column: "SumaTotalPrestamos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Deudores_SituacionMaxima",
                table: "Deudores");

            migrationBuilder.DropIndex(
                name: "IX_Deudores_SumaTotalPrestamos",
                table: "Deudores");
        }
    }
}
