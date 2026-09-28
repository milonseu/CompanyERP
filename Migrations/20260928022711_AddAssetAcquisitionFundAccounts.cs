using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyERP.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetAcquisitionFundAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BankAccountId",
                table: "AssetAcquisitions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CashAccountId",
                table: "AssetAcquisitions",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountId",
                table: "AssetAcquisitions");

            migrationBuilder.DropColumn(
                name: "CashAccountId",
                table: "AssetAcquisitions");
        }
    }
}
