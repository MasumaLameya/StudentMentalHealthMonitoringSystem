using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class PHQAssessment
    {
        [Key]
        public int AssessmentId { get; set; }

        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public Student? Student { get; set; }

        [Required]
        [StringLength(50)]
        public string Semester { get; set; } = string.Empty;

        // ================= PHQ-9 Answers =================

        [Required(ErrorMessage = "Please answer Question 1.")]
        [Range(0, 3)]
        public int? Question1Score { get; set; }

        [Required(ErrorMessage = "Please answer Question 2.")]
        [Range(0, 3)]
        public int? Question2Score { get; set; }

        [Required(ErrorMessage = "Please answer Question 3.")]
        [Range(0, 3)]
        public int? Question3Score { get; set; }

        [Required(ErrorMessage = "Please answer Question 4.")]
        [Range(0, 3)]
        public int? Question4Score { get; set; }

        [Required(ErrorMessage = "Please answer Question 5.")]
        [Range(0, 3)]
        public int? Question5Score { get; set; }

        [Required(ErrorMessage = "Please answer Question 6.")]
        [Range(0, 3)]
        public int? Question6Score { get; set; }

        [Required(ErrorMessage = "Please answer Question 7.")]
        [Range(0, 3)]
        public int? Question7Score { get; set; }

        [Required(ErrorMessage = "Please answer Question 8.")]
        [Range(0, 3)]
        public int? Question8Score { get; set; }

        [Required(ErrorMessage = "Please answer Question 9.")]
        [Range(0, 3)]
        public int? Question9Score { get; set; }

        // Not included in total PHQ-9 score
        public string? FunctionalDifficulty { get; set; }

        [StringLength(2000)]
        public string? AdditionalComments { get; set; }

        // ================= Result =================

        public int TotalScore { get; set; }

        [StringLength(50)]
        public string SeverityLevel { get; set; } = string.Empty;

        public bool RequiresImmediateReview { get; set; }

        public DateTime AssessmentDate { get; set; } = DateTime.Now;
    }
}