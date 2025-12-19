using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiGraphExtensions.Migrations
{
    public partial class RemoveStopWordsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only drop the table if it exists (handles fresh installs where table was never created)
            migrationBuilder.Sql(@"
                IF OBJECT_ID('tbl_OptiGraphExtensions_StopWords', 'U') IS NOT NULL
                BEGIN
                    DROP TABLE [tbl_OptiGraphExtensions_StopWords]
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // StopWords feature is deprecated - do not recreate the table on rollback
        }
    }
}
