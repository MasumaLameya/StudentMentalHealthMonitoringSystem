using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class StudentSemesterRecord
    {
        [Key]
        public int RecordId { get; set; }

        // Logged-in Student
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public Student? Student { get; set; }

        // Student Current Semester
        [Required]
        [StringLength(50)]
        public string Semester { get; set; } = string.Empty;

        // ================= Share Feelings =================

        [StringLength(
            3000,
            ErrorMessage = "Feelings cannot be more than 3000 characters."
        )]
        public string? FeelingText { get; set; }


        // ================= Feelings Analysis =================
        //
        // Unified project-level values:
        //
        // Normal
        // Moderate
        // Severe
        // Extremely Severe
        //

        [StringLength(50)]
        public string? FeelingRiskLevel { get; set; }


        // Short generated summary of the student's
        // feelings analysis.
        //
        // If Severe or Extremely Severe is detected,
        // this summary will be included in the
        // Screening Report for the psychologist
        // assigned to the automatic appointment.

        [Column(TypeName = "text")]
        public string? FeelingSummary { get; set; }


        // ================= Available Time =================

        [StringLength(20)]
        public string? AvailableDay { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? StartTime { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? EndTime { get; set; }

        // ================= Record Information =================

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}