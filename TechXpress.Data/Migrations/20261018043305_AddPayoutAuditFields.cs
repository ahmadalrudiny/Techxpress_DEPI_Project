using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechXpress.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAtUtc",
                table: "WalletTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedByName",
                table: "WalletTransactions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedByUserId",
                table: "WalletTransactions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "WalletTransactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessedAtUtc",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "ProcessedByName",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "ProcessedByUserId",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "WalletTransactions");
        }
    }
}
