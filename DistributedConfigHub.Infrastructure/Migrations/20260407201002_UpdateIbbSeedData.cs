using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DistributedConfigHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIbbSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.InsertData(
                table: "Configurations",
                columns: new[] { "Id", "ApplicationName", "Environment", "IsActive", "Name", "Type", "Value" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "SERVICE-A", "dev", true, "PaymentGatewayUrl", "String", "https://dev-odeme.ibb.istanbul" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "SERVICE-A", "dev", true, "MaxIstanbulKartTransactionsPerMin", "Int", "100" },
                    { new Guid("00000000-0000-0000-0000-000000000003"), "SERVICE-A", "dev", true, "IsMaintenanceModeEnabled", "Boolean", "true" },
                    { new Guid("00000000-0000-0000-0000-000000000004"), "SERVICE-A", "staging", true, "PaymentGatewayUrl", "String", "https://test-odeme.ibb.istanbul" },
                    { new Guid("00000000-0000-0000-0000-000000000005"), "SERVICE-A", "staging", true, "MaxIstanbulKartTransactionsPerMin", "Int", "1000" },
                    { new Guid("00000000-0000-0000-0000-000000000006"), "SERVICE-A", "staging", true, "IsMaintenanceModeEnabled", "Boolean", "false" },
                    { new Guid("00000000-0000-0000-0000-000000000007"), "SERVICE-A", "prod", true, "PaymentGatewayUrl", "String", "https://odeme.ibb.istanbul" },
                    { new Guid("00000000-0000-0000-0000-000000000008"), "SERVICE-A", "prod", true, "MaxIstanbulKartTransactionsPerMin", "Int", "50000" },
                    { new Guid("00000000-0000-0000-0000-000000000009"), "SERVICE-A", "prod", true, "IsMaintenanceModeEnabled", "Boolean", "false" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"));

            migrationBuilder.InsertData(
                table: "Configurations",
                columns: new[] { "Id", "ApplicationName", "Environment", "IsActive", "Name", "Type", "Value" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "SERVICE-A", "prod", true, "SiteName", "String", "Kadikoy Belediyesi Tech Ekibi" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "SERVICE-A", "prod", true, "MaxUsers", "Int", "15000" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "SERVICE-A", "prod", true, "FeatureX_Enabled", "Boolean", "true" }
                });
        }
    }
}
