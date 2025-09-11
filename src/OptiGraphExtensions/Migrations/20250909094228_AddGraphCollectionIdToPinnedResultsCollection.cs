using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiGraphExtensions.Migrations
{
    /// <inheritdoc />
    public partial class AddGraphCollectionIdToPinnedResultsCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GraphCollectionId",
                table: "tbl_OptiGraphExtensions_PinnedResultsCollections",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GraphCollectionId",
                table: "tbl_OptiGraphExtensions_PinnedResultsCollections");
        }
    }
}
