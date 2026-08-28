using System.ComponentModel.DataAnnotations;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class Department
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        public int DepartmentId { get; set; }


        // =========================================================
        // DEPARTMENT NAME
        // Example:
        // CSE, EEE, Mechanical, Civil, BBA, BATHM
        // =========================================================

        [Required]
        [StringLength(100)]
        [Display(Name = "Department Name")]
        public string DepartmentName { get; set; } = string.Empty;


        // =========================================================
        // DEPARTMENT LOGIN EMAIL
        // =========================================================

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a valid email address (e.g. user@domain.com).")]
        public string Email { get; set; } = string.Empty;


        // =========================================================
        // DEPARTMENT LOGIN PASSWORD
        // =========================================================

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", ErrorMessage = "Password must contain at least 8 characters, including 1 uppercase letter, 1 lowercase letter, 1 number, and 1 special character.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;


        // =========================================================
        // DEPARTMENT PHONE
        // =========================================================

        [Required]
        [StringLength(30)]
        public string Phone { get; set; } = string.Empty;


        // =========================================================
        // HEAD OF DEPARTMENT
        // =========================================================

        [StringLength(150)]
        [Display(Name = "Head of Department")]
        public string? HeadOfDepartment { get; set; }
    }
}