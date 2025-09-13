using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiGraphExtensions.Migrations
{
    /// <inheritdoc />
    public partial class AddGraphIdToPinnedResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GraphId",
                table: "tbl_OptiGraphExtensions_PinnedResults",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GraphId",
                table: "tbl_OptiGraphExtensions_PinnedResults");
        }
    }
}
