using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechXpress.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreCreditAndPlatformEarnings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "StoreCreditUsed",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StoreCreditBalance",
                table: "AspNetUsers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PlatformEarnings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    OrderDetailId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsReversed = table.Column<bool>(type: "bit", nullable: false),
                    ReversedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformEarnings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformEarnings_OrderDetails_OrderDetailId",
                        column: x => x.OrderDetailId,
                        principalTable: "OrderDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformEarnings_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreCreditTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreCreditTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreCreditTransactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreCreditTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEarnings_OrderDetailId",
                table: "PlatformEarnings",
                column: "OrderDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEarnings_OrderId",
                table: "PlatformEarnings",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreCreditTransactions_OrderId_Type",
                table: "StoreCreditTransactions",
                columns: new[] { "OrderId", "Type" },
                unique: true,
                filter: "[OrderId] IS NOT NULL AND [Type] = 'OrderRefund'");

            migrationBuilder.CreateIndex(
                name: "IX_StoreCreditTransactions_UserId",
                table: "StoreCreditTransactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformEarnings");

            migrationBuilder.DropTable(
                name: "StoreCreditTransactions");

            migrationBuilder.DropColumn(
                name: "StoreCreditUsed",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StoreCreditBalance",
                table: "AspNetUsers");
        }
    }
}
