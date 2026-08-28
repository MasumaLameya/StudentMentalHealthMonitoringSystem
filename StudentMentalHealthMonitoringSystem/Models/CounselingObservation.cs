using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class CounselingObservation
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        public int CounselingObservationId { get; set; }


        // =========================================================
        // COUNSELING SESSION
        // One counseling session = one Observation
        // =========================================================

        [Required]
        public int CounselingId { get; set; }

        [ForeignKey(nameof(CounselingId))]
        public Counseling? Counseling { get; set; }


        // =========================================================
        // ROOT COUNSELING
        //
        // Used to identify the complete counseling chain.
        //
        // Example:
        // Session 1 = RootCounselingId 10
        // Session 2 = RootCounselingId 10
        // Session 3 = RootCounselingId 10
        // =========================================================

        [Required]
        public int RootCounselingId { get; set; }


        // =========================================================
        // STUDENT
        // =========================================================

        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; set; }


        // =========================================================
        // PSYCHOLOGIST
        // =========================================================

        [Required]
        public int PsychologistId { get; set; }

        [ForeignKey(nameof(PsychologistId))]
        public Psychologist? Psychologist { get; set; }


        // =========================================================
        // 1. OVERALL PROGRESS STATUS
        //
        // Stable
        // Improving
        // Partially Improved
        // No Significant Change
        // Deteriorating
        // =========================================================

        [Required]
        [StringLength(50)]
        public string OverallProgressStatus { get; set; }
            = string.Empty;


        // =========================================================
        // 2. CURRENT MENTAL HEALTH STATUS
        //
        // Normal
        // Moderate
        // Severe
        // Extremely Severe
        // =========================================================

        [Required]
        [StringLength(50)]
        public string CurrentMentalHealthStatus { get; set; }
            = string.Empty;


        // =========================================================
        // 3. PHQ-9 SNAPSHOT
        // Automatically loaded from latest assessment
        // =========================================================

        public int? PHQScore { get; set; }


        [StringLength(100)]
        public string? PHQOfficialInterpretation { get; set; }


        [StringLength(50)]
        public string? PHQProjectStatus { get; set; }


        // =========================================================
        // 3. C-SSRS SNAPSHOT
        // Automatically loaded from latest assessment
        // =========================================================

        [StringLength(50)]
        public string? CSSRSRiskLevel { get; set; }


        [StringLength(50)]
        public string? CSSRSProjectStatus { get; set; }


        // =========================================================
        // 4. FUNCTIONAL STATUS
        // =========================================================

        [Required]
        [StringLength(50)]
        public string AcademicFunctioning { get; set; }
            = string.Empty;


        [Required]
        [StringLength(50)]
        public string SleepCondition { get; set; }
            = string.Empty;


        [Required]
        [StringLength(50)]
        public string SocialInteraction { get; set; }
            = string.Empty;


        [Required]
        [StringLength(50)]
        public string DailyActivities { get; set; }
            = string.Empty;


        [Required]
        [StringLength(50)]
        public string EmotionalRegulation { get; set; }
            = string.Empty;


        // =========================================================
        // 5. CURRENT SAFETY RISK
        //
        // No Current Risk
        // Low Risk
        // Moderate Risk
        // High/Urgent Risk
        // =========================================================

        [Required]
        [StringLength(50)]
        public string CurrentSafetyRisk { get; set; }
            = string.Empty;


        // =========================================================
        // 6. ASSESSMENT BASIS
        //
        // Multiple selected values are stored together.
        // =========================================================

        [Required]
        [Column(TypeName = "text")]
        public string AssessmentBasis { get; set; }
            = string.Empty;


        // =========================================================
        // 7. CLINICAL OBSERVATION
        // =========================================================

        [Required]
        [Column(TypeName = "text")]
        public string ClinicalObservation { get; set; }
            = string.Empty;


        // =========================================================
        // 8. STUDENT-REPORTED IMPROVEMENT
        // =========================================================

        [Required]
        [StringLength(50)]
        public string StudentReportedImprovement { get; set; }
            = string.Empty;


        // =========================================================
        // 9. ASSESSMENT SUMMARY
        // =========================================================

        [Required]
        [Column(TypeName = "text")]
        public string AssessmentSummary { get; set; }
            = string.Empty;


        // =========================================================
        // 10. RECOMMENDED ACTION
        //
        // Multiple selected values are stored together.
        // =========================================================

        [Required]
        [Column(TypeName = "text")]
        public string RecommendedAction { get; set; }
            = string.Empty;


        // =========================================================
        // 11. FOLLOW-UP REQUIRED
        //
        // Follow-up date/time/room remain in Counseling.
        // =========================================================

        public bool FollowUpRequired { get; set; }


        // =========================================================
        // CREATED / UPDATED
        // =========================================================

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;


        public DateTime? UpdatedAt { get; set; }
    }
}
