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
        public string ActiveSemester
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Semester))
                {
                    var sem = Semester.Trim();
                    if (sem.StartsWith("Semester", StringComparison.OrdinalIgnoreCase) ||
                        sem.All(char.IsDigit) ||
                        !(sem.StartsWith("Spring", StringComparison.OrdinalIgnoreCase) ||
                          sem.StartsWith("Summer", StringComparison.OrdinalIgnoreCase) ||
                          sem.StartsWith("Fall", StringComparison.OrdinalIgnoreCase)) ||
                        !sem.Contains("20"))
                    {
                        return GetCurrentActiveSemester();
                    }
                    return sem;
                }
                return GetCurrentActiveSemester();
            }
        }


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


        // =====================================================
        // ACCOUNT STATUS
        // =====================================================

        // When true, the student cannot login.
        // Admin can suspend / unsuspend from the Admin panel.

        public bool IsSuspended { get; set; } = false;


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