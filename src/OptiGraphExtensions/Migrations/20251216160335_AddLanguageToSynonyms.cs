using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiGraphExtensions.Migrations
{
    public partial class AddLanguageToSynonyms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "tbl_OptiGraphExtensions_Synonyms",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "tbl_OptiGraphExtensions_Synonyms");
        }
    }
}
