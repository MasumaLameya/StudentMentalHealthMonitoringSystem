using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class Psychologist
    {
        [Key]
        public int PsychologistId { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a valid email address (e.g. user@domain.com).")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", ErrorMessage = "Password must contain at least 8 characters, including 1 uppercase letter, 1 lowercase letter, 1 number, and 1 special character.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string Phone { get; set; }

        [Display(Name = "Specialization")]
        public string? Specialization { get; set; }

        [Display(Name = "Qualification")]
        public string? Qualification { get; set; }

        [Display(Name = "Experience (Years)")]
        public int? Experience { get; set; }

        public string? ProfileImage { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}