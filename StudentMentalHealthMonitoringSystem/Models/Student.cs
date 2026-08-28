using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class Student
    {
        // ================= Primary Key =================

        [Key]
        public int StudentId { get; set; }


        // ================= Student Information =================

        [Required]
        [Display(Name = "Student ID")]
        public string StudentIdNumber { get; set; } =
            string.Empty;


        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } =
            string.Empty;


        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a valid email address (e.g. user@domain.com).")]
        public string Email { get; set; } =
            string.Empty;


        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", ErrorMessage = "Password must contain at least 8 characters, including 1 uppercase letter, 1 lowercase letter, 1 number, and 1 special character.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } =
            string.Empty;


        [Required]
        public string Phone { get; set; } =
            string.Empty;


        // ================= Personal Information =================

        public DateTime? DateOfBirth { get; set; }


        public string? Gender { get; set; }


        public string? Department { get; set; }


        [Display(Name = "Academic Year / Batch Year")]
        public int? AdmissionYear { get; set; }


        public string? Semester { get; set; }


        public static string GetCurrentActiveSemester(DateTime? date = null)
        {
            var targetDate = date ?? DateTime.Now;
            int month = targetDate.Month;
            int year = targetDate.Year;

            string term = month switch
            {
                >= 1 and <= 4 => "Spring",
                >= 5 and <= 8 => "Summer",
                _ => "Fall"
            };

            return $"{term} {year}";
        }


        [NotMapped]
        public string ActiveSemester => !string.IsNullOrWhiteSpace(Semester) ? Semester : GetCurrentActiveSemester();


        public double? Height { get; set; }


        public double? Weight { get; set; }


        public string? FinancialCondition { get; set; }


        // ================= Guardian Information =================

        public string? GuardianName { get; set; }


        public string? Relationship { get; set; }


        public string? GuardianPhone { get; set; }


        public string? GuardianEmail { get; set; }


        // =====================================================
        // AI CHAT LATEST ASSESSMENT
        // =====================================================

        // Latest chatbot monitoring status.
        //
        // Possible values:
        // Normal
        // Stressed
        // Possible Depression
        // Possible High Risk

        [StringLength(50)]
        public string? LatestChatRiskStatus { get; set; }


        // ================= Latest Assessment Update Time =================

        public DateTime? LatestChatRiskUpdatedAt { get; set; }


        // =====================================================
        // PROFILE IMAGE
        // =====================================================

        // Store image path in database

        public string? ProfileImage { get; set; }


        // Upload image
        // Not stored in database

        [NotMapped]
        public IFormFile? ImageFile { get; set; }


        // =====================================================
        // AI CHAT RELATIONSHIPS
        // =====================================================

        // Student can have multiple AI chat sessions.

        public ICollection<ChatSession> ChatSessions { get; set; }
            = new List<ChatSession>();


        // Student can have multiple chatbot assessments.

        public ICollection<ChatRiskAssessment> ChatRiskAssessments { get; set; }
            = new List<ChatRiskAssessment>();
    }
}