using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyERP.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuPermissionCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PermissionCode",
                table: "Menus",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PermissionCode",
                table: "Menus");
        }
    }
}
