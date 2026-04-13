using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DistributedConfigHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GenericSeedDataUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "Name", "Value" },
                values: new object[] { "ExternalPaymentApiUrl", "https://dev-pay.enterprise.com" });

            migrationBuilder.UpdateData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "Name",
                value: "MaxConcurrentTransactions");

            migrationBuilder.UpdateData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                columns: new[] { "Name", "Value" },
                values: new object[] { "ExternalPaymentApiUrl", "https://test-pay.enterprise.com" });

            migrationBuilder.UpdateData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                column: "Name",
                value: "MaxConcurrentTransactions");

            migrationBuilder.UpdateData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
                columns: new[] { "Name", "Value" },
                values: new object[] { "ExternalPaymentApiUrl", "https://pay.enterprise.com" });

            migrationBuilder.UpdateData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                column: "Name",
                value: "MaxConcurrentTransactions");

            migrationBuilder.InsertData(
                table: "Configurations",
                columns: new[] { "Id", "ApplicationName", "CreatedAt", "CreatedBy", "Environment", "IsActive", "Name", "Type", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000101"), "SERVICE-A", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "dev", true, "MainDatabase", "String", null, null, "Host=postgres;Database=db_alpha;Username=postgres;Password=postgres" },
                    { new Guid("00000000-0000-0000-0000-000000000102"), "SERVICE-A", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "staging", true, "MainDatabase", "String", null, null, "Host=postgres;Database=db_alpha;Username=postgres;Password=postgres" },
                    { new Guid("00000000-0000-0000-0000-000000000103"), "SERVICE-A", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "prod", true, "MainDatabase", "String", null, null, "Host=postgres;Database=db_alpha;Username=postgres;Password=postgres" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000101"));

            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000102"));

            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000103"));

            migrationBuilder.UpdateData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "Name", "Value" },
                values: new object[] { "PaymentGatewayUrl", "https://dev-odeme.ibb.istanbul" });

            migrationBuilder.UpdateData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "Name",
                value: "MaxIstanbulKartTransactionsPerMin");

            migrationBuilder.UpdateData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                columns: new[] { "Name", "Value" },
                values: new object[] { "PaymentGatewayUrl", "https://test-odeme.ibb.istanbul" });

            migrationBuilder.UpdateData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                column: "Name",
                value: "MaxIstanbulKartTransactionsPerMin");

            migrationBuilder.UpdateData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
                columns: new[] { "Name", "Value" },
                values: new object[] { "PaymentGatewayUrl", "https://odeme.ibb.istanbul" });

            migrationBuilder.UpdateData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                column: "Name",
                value: "MaxIstanbulKartTransactionsPerMin");
        }
    }
}
