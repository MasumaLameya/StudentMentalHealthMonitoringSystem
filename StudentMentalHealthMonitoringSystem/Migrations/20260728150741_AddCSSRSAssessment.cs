using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentMentalHealthMonitoringSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCSSRSAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CSSRSAssessments",
                columns: table => new
                {
                    AssessmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Semester = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Question1Answer = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Question2Answer = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Question3Answer = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Question4Answer = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Question5Answer = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Question6Answer = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RecentBehavior = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    AdditionalInformation = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RiskLevel = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiresImmediateAction = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AssessmentDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CSSRSAssessments", x => x.AssessmentId);
                    table.ForeignKey(
                        name: "FK_CSSRSAssessments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CSSRSAssessments_StudentId_Semester",
                table: "CSSRSAssessments",
                columns: new[] { "StudentId", "Semester" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CSSRSAssessments");
        }
    }
}
