using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudentMentalHealthMonitoringSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RiskLevel",
                table: "Counselings",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Recommendation",
                table: "Counselings",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Observation",
                table: "Counselings",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Assessment",
                table: "Counselings",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "AppointmentTime",
                table: "Counselings",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(TimeSpan),
                oldType: "time(6)",
                oldNullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "AppointmentEndTime",
                table: "Counselings",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "Counselings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Counselings",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedAt",
                table: "Counselings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "RequestedEndTime",
                table: "Counselings",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "RequestedStartTime",
                table: "Counselings",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DepartmentName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Password = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HeadOfDepartment = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PsychologistAvailabilities",
                columns: table => new
                {
                    PsychologistAvailabilityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PsychologistId = table.Column<int>(type: "int", nullable: false),
                    Day = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsychologistAvailabilities", x => x.PsychologistAvailabilityId);
                    table.ForeignKey(
                        name: "FK_PsychologistAvailabilities_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StudentAvailabilities",
                columns: table => new
                {
                    StudentAvailabilityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Day = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAvailabilities", x => x.StudentAvailabilityId);
                    table.ForeignKey(
                        name: "FK_StudentAvailabilities_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "DepartmentId", "DepartmentName", "Email", "HeadOfDepartment", "Password", "Phone" },
                values: new object[,]
                {
                    { 1, "CSE", "cse@smhms.com", "CSE Department Head", "123456", "0000000000" },
                    { 2, "EEE", "eee@smhms.com", "EEE Department Head", "123456", "0000000000" },
                    { 3, "Mechanical", "mechanical@smhms.com", "Mechanical Department Head", "123456", "0000000000" },
                    { 4, "Civil", "civil@smhms.com", "Civil Department Head", "123456", "0000000000" },
                    { 5, "BBA", "bba@smhms.com", "BBA Department Head", "123456", "0000000000" },
                    { 6, "BATHM", "bathm@smhms.com", "BATHM Department Head", "123456", "0000000000" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PsychologistAvailabilities_PsychologistId",
                table: "PsychologistAvailabilities",
                column: "PsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAvailabilities_StudentId",
                table: "StudentAvailabilities",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "PsychologistAvailabilities");

            migrationBuilder.DropTable(
                name: "StudentAvailabilities");

            migrationBuilder.DropColumn(
                name: "AppointmentEndTime",
                table: "Counselings");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "Counselings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Counselings");

            migrationBuilder.DropColumn(
                name: "RequestedAt",
                table: "Counselings");

            migrationBuilder.DropColumn(
                name: "RequestedEndTime",
                table: "Counselings");

            migrationBuilder.DropColumn(
                name: "RequestedStartTime",
                table: "Counselings");

            migrationBuilder.UpdateData(
                table: "Counselings",
                keyColumn: "RiskLevel",
                keyValue: null,
                column: "RiskLevel",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "RiskLevel",
                table: "Counselings",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Counselings",
                keyColumn: "Recommendation",
                keyValue: null,
                column: "Recommendation",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Recommendation",
                table: "Counselings",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Counselings",
                keyColumn: "Observation",
                keyValue: null,
                column: "Observation",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Observation",
                table: "Counselings",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Counselings",
                keyColumn: "Assessment",
                keyValue: null,
                column: "Assessment",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Assessment",
                table: "Counselings",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "AppointmentTime",
                table: "Counselings",
                type: "time(6)",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time(6)");
        }
    }
}
