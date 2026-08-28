using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentMentalHealthMonitoringSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCounselingAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppointmentRoom",
                table: "Counselings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "AppointmentTime",
                table: "Counselings",
                type: "time(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppointmentRoom",
                table: "Counselings");

            migrationBuilder.DropColumn(
                name: "AppointmentTime",
                table: "Counselings");
        }
    }
}
