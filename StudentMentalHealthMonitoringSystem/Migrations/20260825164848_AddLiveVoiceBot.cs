using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentMentalHealthMonitoringSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveVoiceBot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VoiceBotSessions",
                columns: table => new
                {
                    VoiceBotSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ModelName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentSummary = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastStatusUpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceBotSessions", x => x.VoiceBotSessionId);
                    table.ForeignKey(
                        name: "FK_VoiceBotSessions_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VoiceBotReports",
                columns: table => new
                {
                    VoiceBotReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VoiceBotSessionId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentSummary = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FinalStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FinalSummary = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsFinal = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    FinalizedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceBotReports", x => x.VoiceBotReportId);
                    table.ForeignKey(
                        name: "FK_VoiceBotReports_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoiceBotReports_VoiceBotSessions_VoiceBotSessionId",
                        column: x => x.VoiceBotSessionId,
                        principalTable: "VoiceBotSessions",
                        principalColumn: "VoiceBotSessionId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VoiceBotRiskAssessments",
                columns: table => new
                {
                    VoiceBotRiskAssessmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VoiceBotSessionId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    RiskStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Summary = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceBotRiskAssessments", x => x.VoiceBotRiskAssessmentId);
                    table.ForeignKey(
                        name: "FK_VoiceBotRiskAssessments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoiceBotRiskAssessments_VoiceBotSessions_VoiceBotSessionId",
                        column: x => x.VoiceBotSessionId,
                        principalTable: "VoiceBotSessions",
                        principalColumn: "VoiceBotSessionId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VoiceBotTranscripts",
                columns: table => new
                {
                    VoiceBotTranscriptId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VoiceBotSessionId = table.Column<int>(type: "int", nullable: false),
                    Speaker = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TranscriptText = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceBotTranscripts", x => x.VoiceBotTranscriptId);
                    table.ForeignKey(
                        name: "FK_VoiceBotTranscripts_VoiceBotSessions_VoiceBotSessionId",
                        column: x => x.VoiceBotSessionId,
                        principalTable: "VoiceBotSessions",
                        principalColumn: "VoiceBotSessionId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceBotReports_StudentId_LastUpdatedAt",
                table: "VoiceBotReports",
                columns: new[] { "StudentId", "LastUpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceBotReports_VoiceBotSessionId",
                table: "VoiceBotReports",
                column: "VoiceBotSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoiceBotRiskAssessments_StudentId_CreatedAt",
                table: "VoiceBotRiskAssessments",
                columns: new[] { "StudentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceBotRiskAssessments_VoiceBotSessionId_CreatedAt",
                table: "VoiceBotRiskAssessments",
                columns: new[] { "VoiceBotSessionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceBotSessions_StudentId_IsActive",
                table: "VoiceBotSessions",
                columns: new[] { "StudentId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceBotTranscripts_VoiceBotSessionId_CreatedAt",
                table: "VoiceBotTranscripts",
                columns: new[] { "VoiceBotSessionId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoiceBotReports");

            migrationBuilder.DropTable(
                name: "VoiceBotRiskAssessments");

            migrationBuilder.DropTable(
                name: "VoiceBotTranscripts");

            migrationBuilder.DropTable(
                name: "VoiceBotSessions");
        }
    }
}
