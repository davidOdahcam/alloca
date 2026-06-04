using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alloca.Infra.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeskIsReservable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReservable",
                table: "Desks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Preserva o comportamento anterior: as mesas existentes eram sempre reserváveis.
            migrationBuilder.Sql("UPDATE [Desks] SET [IsReservable] = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReservable",
                table: "Desks");
        }
    }
}
