using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchToAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "PurchaseInvoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "JournalEntries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_BranchId",
                table: "PurchaseInvoices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_BranchId",
                table: "JournalEntries",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_CompanyId_BranchId",
                table: "JournalEntries",
                columns: new[] { "CompanyId", "BranchId" });

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_Branches_BranchId",
                table: "JournalEntries",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_Branches_BranchId",
                table: "PurchaseInvoices",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_Branches_BranchId",
                table: "JournalEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoices_Branches_BranchId",
                table: "PurchaseInvoices");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseInvoices_BranchId",
                table: "PurchaseInvoices");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_BranchId",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_CompanyId_BranchId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "JournalEntries");
        }
    }
}
