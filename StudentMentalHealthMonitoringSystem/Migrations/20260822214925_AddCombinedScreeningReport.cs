using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentMentalHealthMonitoringSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCombinedScreeningReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeelingRiskLevel",
                table: "StudentSemesterRecords",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FeelingSummary",
                table: "StudentSemesterRecords",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ScreeningReports",
                columns: table => new
                {
                    ScreeningReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    PsychologistId = table.Column<int>(type: "int", nullable: false),
                    CounselingId = table.Column<int>(type: "int", nullable: false),
                    TriggerSource = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TriggerSeverity = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReportContent = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScreeningReports", x => x.ScreeningReportId);
                    table.ForeignKey(
                        name: "FK_ScreeningReports_Counselings_CounselingId",
                        column: x => x.CounselingId,
                        principalTable: "Counselings",
                        principalColumn: "CounselingId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScreeningReports_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScreeningReports_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningReports_CounselingId",
                table: "ScreeningReports",
                column: "CounselingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningReports_PsychologistId",
                table: "ScreeningReports",
                column: "PsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningReports_StudentId",
                table: "ScreeningReports",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScreeningReports");

            migrationBuilder.DropColumn(
                name: "FeelingRiskLevel",
                table: "StudentSemesterRecords");

            migrationBuilder.DropColumn(
                name: "FeelingSummary",
                table: "StudentSemesterRecords");
        }
    }
}
