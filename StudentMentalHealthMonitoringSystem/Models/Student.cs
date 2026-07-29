using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        [Required]
        [Display(Name = "Student ID")]
        public string StudentIdNumber { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string Phone { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? Department { get; set; }

        public string? Semester { get; set; }

        public double? Height { get; set; }

        public double? Weight { get; set; }

        public string? FinancialCondition { get; set; }

        public string? GuardianName { get; set; }

        public string? Relationship { get; set; }

        public string? GuardianPhone { get; set; }

        public string? GuardianEmail { get; set; }

        // Store image path in database
        public string? ProfileImage { get; set; }

        // Upload image (Not stored in database)
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}