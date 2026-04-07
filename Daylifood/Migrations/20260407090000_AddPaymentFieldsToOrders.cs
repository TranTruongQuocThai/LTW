using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Daylifood.Migrations
{
    public partial class AddPaymentFieldsToOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InventoryCommitted",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PaymentGatewayResponseCode",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentGatewayTransactionNo",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE Orders
                SET PaymentMethod = 0,
                    PaymentStatus = CASE WHEN Status = 2 THEN 2 ELSE 0 END,
                    InventoryCommitted = 1,
                    PaymentReference = CAST(Id AS nvarchar(32));
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "InventoryCommitted", table: "Orders");
            migrationBuilder.DropColumn(name: "PaidAt", table: "Orders");
            migrationBuilder.DropColumn(name: "PaymentMethod", table: "Orders");
            migrationBuilder.DropColumn(name: "PaymentGatewayResponseCode", table: "Orders");
            migrationBuilder.DropColumn(name: "PaymentGatewayTransactionNo", table: "Orders");
            migrationBuilder.DropColumn(name: "PaymentReference", table: "Orders");
            migrationBuilder.DropColumn(name: "PaymentStatus", table: "Orders");
        }
    }
}
