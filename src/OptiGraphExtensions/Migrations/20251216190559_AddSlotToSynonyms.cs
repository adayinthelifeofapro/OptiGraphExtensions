using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiGraphExtensions.Migrations
{
    public partial class AddSlotToSynonyms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Slot",
                table: "tbl_OptiGraphExtensions_Synonyms",
                type: "int",
                nullable: false,
                defaultValue: 1); // SynonymSlot.ONE
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Slot",
                table: "tbl_OptiGraphExtensions_Synonyms");
        }
    }
}
