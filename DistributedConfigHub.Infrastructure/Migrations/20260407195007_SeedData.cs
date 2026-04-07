using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DistributedConfigHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
