using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class ObservationReport
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        public int ObservationReportId { get; set; }


        // =========================================================
        // ROOT COUNSELING
        //
        // One counseling chain = one Observation Report
        // =========================================================

        [Required]
        public int RootCounselingId { get; set; }

        [ForeignKey(nameof(RootCounselingId))]
        public Counseling? RootCounseling { get; set; }


        // =========================================================
        // STUDENT
        // =========================================================

        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; set; }

        private string? _semester;

        [NotMapped]
        public string Semester
        {
            get => _semester ?? Student?.Semester ?? "Semester 1";
            set => _semester = value;
        }


        // =========================================================
        // PSYCHOLOGIST
        // =========================================================

        [Required]
        public int PsychologistId { get; set; }

        [ForeignKey(nameof(PsychologistId))]
        public Psychologist? Psychologist { get; set; }


        // =========================================================
        // INITIAL CONDITION
        //
        // Original serious condition when the counseling
        // chain started.
        // =========================================================

        [Required]
        [StringLength(50)]
        public string InitialStatus { get; set; }
            = "Not Assessed";


        // =========================================================
        // CURRENT / LATEST CONDITION
        // =========================================================

        [Required]
        [StringLength(50)]
        public string CurrentStatus { get; set; }
            = "Not Assessed";


        // =========================================================
        // LATEST OVERALL PROGRESS
        // =========================================================

        [Required]
        [StringLength(50)]
        public string OverallProgressStatus { get; set; }
            = string.Empty;


        // =========================================================
        // LATEST SAFETY STATUS
        // =========================================================

        [Required]
        [StringLength(50)]
        public string CurrentSafetyRisk { get; set; }
            = string.Empty;


        // =========================================================
        // LATEST ASSESSMENT BASIS
        // =========================================================

        [Required]
        [Column(TypeName = "text")]
        public string LatestAssessmentBasis { get; set; }
            = string.Empty;


        // =========================================================
        // LATEST RECOMMENDED ACTION
        // =========================================================

        [Required]
        [Column(TypeName = "text")]
        public string LatestRecommendedAction { get; set; }
            = string.Empty;


        // =========================================================
        // LATEST / FINAL CONDITION SUMMARY
        // =========================================================

        [Required]
        [Column(TypeName = "text")]
        public string LatestConditionSummary { get; set; }
            = string.Empty;


        // =========================================================
        // REPORT STATUS
        //
        // false = more follow-up may continue
        // true  = No Follow-up Needed / report finalized
        // =========================================================

        public bool IsFinal { get; set; } = false;


        public DateTime? FinalizedAt { get; set; }


        // =========================================================
        // CREATED / UPDATED
        // =========================================================

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;


        public DateTime UpdatedAt { get; set; }
            = DateTime.Now;
    }
}