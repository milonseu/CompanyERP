using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyERP.Migrations
{
    /// <inheritdoc />
    public partial class AddStockBalanceValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalValue",
                table: "StockBalances",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE StockBalances SET TotalValue = Quantity * AverageCost WHERE Quantity > 0 AND TotalValue = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalValue",
                table: "StockBalances");
        }
    }
}
