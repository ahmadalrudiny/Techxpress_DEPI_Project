using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechXpress.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CartJson",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CartJson",
                table: "AspNetUsers");
        }
    }
}
