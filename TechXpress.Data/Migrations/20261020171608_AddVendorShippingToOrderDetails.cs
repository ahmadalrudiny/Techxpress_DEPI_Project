using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechXpress.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorShippingToOrderDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Carrier",
                table: "OrderDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "OrderDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAt",
                table: "OrderDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShippingStatus",
                table: "OrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TrackingNumber",
                table: "OrderDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Carrier",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ShippingStatus",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "TrackingNumber",
                table: "OrderDetails");
        }
    }
}
