using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class CSSRSAssessment
    {
        [Key]
        public int AssessmentId { get; set; }

        // Logged-in Student ID
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public Student? Student { get; set; }

        // Current Semester
        [Required]
        [StringLength(50)]
        public string Semester { get; set; } = string.Empty;

        // ================= C-SSRS Answers =================

        [Required(ErrorMessage = "Please answer Question 1.")]
        public bool? Question1Answer { get; set; }

        [Required(ErrorMessage = "Please answer Question 2.")]
        public bool? Question2Answer { get; set; }

        [Required(ErrorMessage = "Please answer Question 3.")]
        public bool? Question3Answer { get; set; }

        [Required(ErrorMessage = "Please answer Question 4.")]
        public bool? Question4Answer { get; set; }

        [Required(ErrorMessage = "Please answer Question 5.")]
        public bool? Question5Answer { get; set; }

        [Required(ErrorMessage = "Please answer Question 6.")]
        public bool? Question6Answer { get; set; }

        // Question 6 Yes হলে
        // recent behaviour information
        public bool? RecentBehavior { get; set; }

        [StringLength(2000)]
        public string? AdditionalInformation { get; set; }

        // ================= Result =================

        [StringLength(50)]
        public string RiskLevel { get; set; } = string.Empty;

        public bool RequiresImmediateAction { get; set; }

        public DateTime AssessmentDate { get; set; } = DateTime.Now;
    }
}