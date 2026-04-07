using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistributedConfigHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    ApplicationName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Environment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Configurations_Name_ApplicationName_Environment",
                table: "Configurations",
                columns: new[] { "Name", "ApplicationName", "Environment" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configurations");
        }
    }
}
