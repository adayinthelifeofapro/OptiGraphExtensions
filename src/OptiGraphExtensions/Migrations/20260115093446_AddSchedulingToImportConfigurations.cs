using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiGraphExtensions.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingToImportConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveFailures",
                table: "tbl_OptiGraphExtensions_ImportConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastImportError",
                table: "tbl_OptiGraphExtensions_ImportConfigurations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LastImportSuccess",
                table: "tbl_OptiGraphExtensions_ImportConfigurations",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRetries",
                table: "tbl_OptiGraphExtensions_ImportConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "tbl_OptiGraphExtensions_ImportConfigurations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextScheduledRunAt",
                table: "tbl_OptiGraphExtensions_ImportConfigurations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificationEmail",
                table: "tbl_OptiGraphExtensions_ImportConfigurations",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleDayOfMonth",
                table: "tbl_OptiGraphExtensions_ImportConfigurations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleDayOfWeek",
                table: "tbl_OptiGraphExtensions_ImportConfigurations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleFrequency",
                table: "tbl_OptiGraphExtensions_ImportConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleIntervalValue",
                table: "tbl_OptiGraphExtensions_ImportConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ScheduleTimeOfDay",
                table: "tbl_OptiGraphExtensions_ImportConfigurations",
                type: "time",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_OptiGraphExtensions_ImportExecutionHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ItemsReceived = table.Column<int>(type: "int", nullable: false),
                    ItemsImported = table.Column<int>(type: "int", nullable: false),
                    ItemsSkipped = table.Column<int>(type: "int", nullable: false),
                    ItemsFailed = table.Column<int>(type: "int", nullable: false),
                    DurationTicks = table.Column<long>(type: "bigint", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Warnings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WasRetry = table.Column<bool>(type: "bit", nullable: false),
                    RetryAttempt = table.Column<int>(type: "int", nullable: false),
                    WasScheduled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_OptiGraphExtensions_ImportExecutionHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_OptiGraphExtensions_ImportExecutionHistory_tbl_OptiGraphExtensions_ImportConfigurations_ImportConfigurationId",
                        column: x => x.ImportConfigurationId,
                        principalTable: "tbl_OptiGraphExtensions_ImportConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Note: Indexes for Synonyms, SavedQueries, PinnedResults, PinnedResultsCollections,
            // and ImportConfigurations.TargetSourceId already exist from previous migrations.
            // Only create new indexes for scheduling and history.

            migrationBuilder.CreateIndex(
                name: "IX_tbl_OptiGraphExtensions_ImportConfigurations_NextRetryAt",
                table: "tbl_OptiGraphExtensions_ImportConfigurations",
                column: "NextRetryAt");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_OptiGraphExtensions_ImportConfigurations_NextScheduledRunAt",
                table: "tbl_OptiGraphExtensions_ImportConfigurations",
                column: "NextScheduledRunAt");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_OptiGraphExtensions_ImportExecutionHistory_ImportConfigurationId",
                table: "tbl_OptiGraphExtensions_ImportExecutionHistory",
                column: "ImportConfigurationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_OptiGraphExtensions_ImportExecutionHistory");

            // Only drop indexes created by this migration
            migrationBuilder.DropIndex(
                name: "IX_tbl_OptiGraphExtensions_ImportConfigurations_NextRetryAt",
                table: "tbl_OptiGraphExtensions_ImportConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_tbl_OptiGraphExtensions_ImportConfigurations_NextScheduledRunAt",
                table: "tbl_OptiGraphExtensions_ImportConfigurations");

            migrationBuilder.DropColumn(
                name: "ConsecutiveFailures",
                table: "tbl_OptiGraphExtensions_ImportConfigurations");

            migrationBuilder.DropColumn(
                name: "LastImportError",
                table: "tbl_OptiGraphExtensions_ImportConfigurations");

            migrationBuilder.DropColumn(
                name: "LastImportSuccess",
                table: "tbl_OptiGraphExtensions_ImportConfigurations");

            migrationBuilder.DropColumn(
                name: "MaxRetries",
                table: "tbl_OptiGraphExtensions_ImportConfigurations");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "tbl_OptiGraphExtensions_ImportConfigurations");

            migrationBuilder.DropColumn(
                name: "NextScheduledRunAt",
                table: "tbl_OptiGraphExtensions_ImportConfigurations");

            migrationBuilder.DropColumn(
                name: "NotificationEmail",
                table: "tbl_OptiGraphExtensions_ImportConfigurations");

            migrationBuilder.DropColumn(
                name: "ScheduleDayOfMonth",
                table: "tbl_OptiGraphExtensions_ImportConfigurations");

            migrationBuilder.DropColumn(
                name: "ScheduleDayOfWeek",
                table: "tbl_OptiGraphExtensions_ImportConfigurations");

            migrationBuilder.DropColumn(
                name: "ScheduleFrequency",
                table: "tbl_OptiGraphExtensions_ImportConfigurations");

            migrationBuilder.DropColumn(
                name: "ScheduleIntervalValue",
                table: "tbl_OptiGraphExtensions_ImportConfigurations");

            migrationBuilder.DropColumn(
                name: "ScheduleTimeOfDay",
                table: "tbl_OptiGraphExtensions_ImportConfigurations");
        }
    }
}
