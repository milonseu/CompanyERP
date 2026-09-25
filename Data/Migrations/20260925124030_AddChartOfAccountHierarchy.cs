using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChartOfAccountHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLeaf",
                table: "ChartOfAccounts",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPostable",
                table: "ChartOfAccounts",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "ChartOfAccounts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccounts_ParentId",
                table: "ChartOfAccounts",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChartOfAccounts_ChartOfAccounts_ParentId",
                table: "ChartOfAccounts",
                column: "ParentId",
                principalTable: "ChartOfAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChartOfAccounts_ChartOfAccounts_ParentId",
                table: "ChartOfAccounts");

            migrationBuilder.DropIndex(
                name: "IX_ChartOfAccounts_ParentId",
                table: "ChartOfAccounts");

            migrationBuilder.DropColumn(
                name: "IsLeaf",
                table: "ChartOfAccounts");

            migrationBuilder.DropColumn(
                name: "IsPostable",
                table: "ChartOfAccounts");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "ChartOfAccounts");
        }
    }
}
