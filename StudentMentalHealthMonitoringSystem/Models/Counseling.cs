using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class Counseling
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        public int CounselingId { get; set; }


        // =========================================================
        // STUDENT INFORMATION
        // =========================================================

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public Student? Student { get; set; }


        // =========================================================
        // PSYCHOLOGIST INFORMATION
        // =========================================================

        [Required]
        public int PsychologistId { get; set; }

        [ForeignKey("PsychologistId")]
        public Psychologist? Psychologist { get; set; }


        // =========================================================
        // COUNSELING DATE
        // =========================================================

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Counseling Date")]
        public DateTime CounselingDate { get; set; }


        // =========================================================
        // APPOINTMENT START TIME
        // =========================================================

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Appointment Start Time")]
        public TimeSpan AppointmentTime { get; set; }


        // =========================================================
        // APPOINTMENT END TIME
        // =========================================================

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Appointment End Time")]
        public TimeSpan AppointmentEndTime { get; set; }


        // =========================================================
        // COUNSELING OBSERVATION
        // Psychologist can add observation after counseling
        // =========================================================

        [Display(Name = "Observation")]
        public string? Observation { get; set; }


        // =========================================================
        // COUNSELING ASSESSMENT
        // =========================================================

        [Display(Name = "Assessment")]
        public string? Assessment { get; set; }


        // =========================================================
        // COUNSELING RECOMMENDATION
        // =========================================================

        [Display(Name = "Recommendation")]
        public string? Recommendation { get; set; }


        // =========================================================
        // RISK LEVEL
        //
        // Project Unified Levels:
        // Normal
        // Moderate
        // Severe
        // Extremely Severe
        // =========================================================

        [Display(Name = "Risk Level")]
        public string? RiskLevel { get; set; }


        // =========================================================
        // NEXT FOLLOW-UP DATE
        // =========================================================

        [Display(Name = "Next Follow-up Date")]
        [DataType(DataType.Date)]
        public DateTime? NextFollowUpDate { get; set; }


        // =========================================================
        // NEXT FOLLOW-UP TIME
        // =========================================================

        [Display(Name = "Next Follow-up Time")]
        [DataType(DataType.Time)]
        public TimeSpan? NextFollowUpTime { get; set; }


        // =========================================================
        // PARENT COUNSELING
        //
        // Used to connect follow-up sessions.
        //
        // Example:
        // Session 1 -> null
        // Session 2 -> Session 1 CounselingId
        // Session 3 -> Session 2 CounselingId
        // =========================================================

        public int? ParentCounselingId { get; set; }


        // =========================================================
        // APPOINTMENT SOURCE
        //
        // Possible examples:
        // StudentRequest
        // AutoAssignment
        // FollowUp
        // =========================================================

        [StringLength(50)]
        public string? AppointmentSource { get; set; }


        // =========================================================
        // TRIGGER SOURCE
        //
        // Possible examples:
        // PHQ-9
        // C-SSRS
        // AI Chat
        // Feelings
        // Voice Bot
        // Psychologist Follow-up
        // =========================================================

        [StringLength(100)]
        public string? TriggerSource { get; set; }


        // =========================================================
        // TRIGGER SEVERITY
        //
        // Severe
        // Extremely Severe
        // =========================================================

        [StringLength(50)]
        public string? TriggerSeverity { get; set; }


        // =========================================================
        // APPOINTMENT ROOM
        // =========================================================

        [Display(Name = "Appointment Room")]
        public string? AppointmentRoom { get; set; }


        // =========================================================
        // COUNSELING STATUS
        //
        // Possible values:
        // Pending
        // Confirmed
        // Completed
        // Cancelled
        // =========================================================

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";


        // =========================================================
        // CREATED DATE
        // =========================================================

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}