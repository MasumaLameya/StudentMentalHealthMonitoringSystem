using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentMentalHealthMonitoringSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPHQAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PHQAssessments",
                columns: table => new
                {
                    AssessmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Semester = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Question1Score = table.Column<int>(type: "int", nullable: false),
                    Question2Score = table.Column<int>(type: "int", nullable: false),
                    Question3Score = table.Column<int>(type: "int", nullable: false),
                    Question4Score = table.Column<int>(type: "int", nullable: false),
                    Question5Score = table.Column<int>(type: "int", nullable: false),
                    Question6Score = table.Column<int>(type: "int", nullable: false),
                    Question7Score = table.Column<int>(type: "int", nullable: false),
                    Question8Score = table.Column<int>(type: "int", nullable: false),
                    Question9Score = table.Column<int>(type: "int", nullable: false),
                    FunctionalDifficulty = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdditionalComments = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalScore = table.Column<int>(type: "int", nullable: false),
                    SeverityLevel = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiresImmediateReview = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AssessmentDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PHQAssessments", x => x.AssessmentId);
                    table.ForeignKey(
                        name: "FK_PHQAssessments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PHQAssessments_StudentId_Semester",
                table: "PHQAssessments",
                columns: new[] { "StudentId", "Semester" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PHQAssessments");
        }
    }
}
