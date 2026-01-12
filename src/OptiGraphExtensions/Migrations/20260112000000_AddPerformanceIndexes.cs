using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiGraphExtensions.Migrations
{
    /// <summary>
    /// Adds database indexes on frequently filtered columns to improve query performance.
    /// Also alters Language columns from nvarchar(max) to nvarchar(10) to enable indexing.
    /// </summary>
    public partial class AddPerformanceIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, alter Language columns to fixed size so they can be indexed
            // Language codes are typically 2-5 characters (e.g., "en", "sv", "en-US")

            migrationBuilder.AlterColumn<string>(
                name: "Language",
                table: "tbl_OptiGraphExtensions_Synonyms",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Language",
                table: "tbl_OptiGraphExtensions_PinnedResults",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // Now add indexes on filter columns

            // Synonym indexes
            migrationBuilder.CreateIndex(
                name: "IX_Synonyms_Language",
                table: "tbl_OptiGraphExtensions_Synonyms",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_Synonyms_Slot",
                table: "tbl_OptiGraphExtensions_Synonyms",
                column: "Slot");

            // Composite index for common filter pattern (language + slot)
            migrationBuilder.CreateIndex(
                name: "IX_Synonyms_Language_Slot",
                table: "tbl_OptiGraphExtensions_Synonyms",
                columns: new[] { "Language", "Slot" });

            // PinnedResult indexes
            migrationBuilder.CreateIndex(
                name: "IX_PinnedResults_Language",
                table: "tbl_OptiGraphExtensions_PinnedResults",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_PinnedResults_IsActive",
                table: "tbl_OptiGraphExtensions_PinnedResults",
                column: "IsActive");

            // PinnedResultsCollection indexes
            migrationBuilder.CreateIndex(
                name: "IX_PinnedResultsCollections_IsActive",
                table: "tbl_OptiGraphExtensions_PinnedResultsCollections",
                column: "IsActive");

            // SavedQuery indexes
            migrationBuilder.CreateIndex(
                name: "IX_SavedQueries_Name",
                table: "tbl_OptiGraphExtensions_SavedQueries",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedQueries_IsActive",
                table: "tbl_OptiGraphExtensions_SavedQueries",
                column: "IsActive");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop all indexes first
            migrationBuilder.DropIndex(
                name: "IX_SavedQueries_IsActive",
                table: "tbl_OptiGraphExtensions_SavedQueries");

            migrationBuilder.DropIndex(
                name: "IX_SavedQueries_Name",
                table: "tbl_OptiGraphExtensions_SavedQueries");

            migrationBuilder.DropIndex(
                name: "IX_PinnedResultsCollections_IsActive",
                table: "tbl_OptiGraphExtensions_PinnedResultsCollections");

            migrationBuilder.DropIndex(
                name: "IX_PinnedResults_IsActive",
                table: "tbl_OptiGraphExtensions_PinnedResults");

            migrationBuilder.DropIndex(
                name: "IX_PinnedResults_Language",
                table: "tbl_OptiGraphExtensions_PinnedResults");

            migrationBuilder.DropIndex(
                name: "IX_Synonyms_Language_Slot",
                table: "tbl_OptiGraphExtensions_Synonyms");

            migrationBuilder.DropIndex(
                name: "IX_Synonyms_Slot",
                table: "tbl_OptiGraphExtensions_Synonyms");

            migrationBuilder.DropIndex(
                name: "IX_Synonyms_Language",
                table: "tbl_OptiGraphExtensions_Synonyms");

            // Revert Language columns back to nvarchar(max)
            migrationBuilder.AlterColumn<string>(
                name: "Language",
                table: "tbl_OptiGraphExtensions_PinnedResults",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Language",
                table: "tbl_OptiGraphExtensions_Synonyms",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);
        }
    }
}
