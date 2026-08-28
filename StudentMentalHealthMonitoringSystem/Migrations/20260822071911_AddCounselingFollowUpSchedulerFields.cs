using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentMentalHealthMonitoringSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCounselingFollowUpSchedulerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppointmentSource",
                table: "Counselings",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "NextFollowUpTime",
                table: "Counselings",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentCounselingId",
                table: "Counselings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriggerSeverity",
                table: "Counselings",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TriggerSource",
                table: "Counselings",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppointmentSource",
                table: "Counselings");

            migrationBuilder.DropColumn(
                name: "NextFollowUpTime",
                table: "Counselings");

            migrationBuilder.DropColumn(
                name: "ParentCounselingId",
                table: "Counselings");

            migrationBuilder.DropColumn(
                name: "TriggerSeverity",
                table: "Counselings");

            migrationBuilder.DropColumn(
                name: "TriggerSource",
                table: "Counselings");
        }
    }
}
