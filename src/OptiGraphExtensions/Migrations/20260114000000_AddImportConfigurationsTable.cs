using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiGraphExtensions.Migrations
{
    /// <inheritdoc />
    public partial class AddImportConfigurationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_OptiGraphExtensions_ImportConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TargetSourceId = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    TargetContentType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ApiUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AuthType = table.Column<int>(type: "int", nullable: false),
                    AuthKeyOrUsername = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AuthValueOrPassword = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    FieldMappingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdFieldMapping = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LanguageRouting = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CustomHeadersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastImportAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastImportCount = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_OptiGraphExtensions_ImportConfigurations", x => x.Id);
                });

            // Add index on TargetSourceId for faster lookups
            migrationBuilder.CreateIndex(
                name: "IX_tbl_OptiGraphExtensions_ImportConfigurations_TargetSourceId",
                table: "tbl_OptiGraphExtensions_ImportConfigurations",
                column: "TargetSourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_OptiGraphExtensions_ImportConfigurations");
        }
    }
}
