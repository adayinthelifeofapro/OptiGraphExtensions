using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiGraphExtensions.Migrations
{
    /// <inheritdoc />
    public partial class AddPinnedResultsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_OptiGraphExtensions_PinnedResultsCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_OptiGraphExtensions_PinnedResultsCollections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_OptiGraphExtensions_PinnedResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Phrases = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_OptiGraphExtensions_PinnedResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_OptiGraphExtensions_PinnedResults_tbl_OptiGraphExtensions_PinnedResultsCollections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "tbl_OptiGraphExtensions_PinnedResultsCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_OptiGraphExtensions_PinnedResults_CollectionId",
                table: "tbl_OptiGraphExtensions_PinnedResults",
                column: "CollectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_OptiGraphExtensions_PinnedResults");

            migrationBuilder.DropTable(
                name: "tbl_OptiGraphExtensions_PinnedResultsCollections");
        }
    }
}
