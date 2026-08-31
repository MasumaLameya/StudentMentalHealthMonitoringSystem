using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class StudentProfileViewModel
    {
        public int StudentId { get; set; }

        [Display(Name = "Student ID Number")]
        public string StudentIdNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [Display(Name = "Academic Department")]
        public string? Department { get; set; }

        [Display(Name = "Admission / Batch Year")]
        public int? AdmissionYear { get; set; }

        [Display(Name = "Current Semester")]
        public string? Semester { get; set; }

        [Display(Name = "Height (cm/ft)")]
        public double? Height { get; set; }

        [Display(Name = "Weight (kg)")]
        public double? Weight { get; set; }

        [Display(Name = "Financial Condition")]
        public string? FinancialCondition { get; set; }

        // ================= Guardian Information =================

        [Display(Name = "Guardian Full Name")]
        public string? GuardianName { get; set; }

        [Display(Name = "Relationship with Guardian")]
        public string? Relationship { get; set; }

        [Display(Name = "Guardian Phone Number")]
        public string? GuardianPhone { get; set; }

        [EmailAddress(ErrorMessage = "Invalid guardian email address format.")]
        [Display(Name = "Guardian Email Address")]
        public string? GuardianEmail { get; set; }

        // ================= Profile Picture =================

        public string? ProfileImage { get; set; }

        [Display(Name = "Upload New Profile Image")]
        public IFormFile? ImageFile { get; set; }

        // ================= Password Change (Optional) =================

        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        public string? ConfirmNewPassword { get; set; }
    }
}
