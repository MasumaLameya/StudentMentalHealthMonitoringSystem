using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class StudentAvailability
    {
        [Key]
        public int StudentAvailabilityId { get; set; }

        // ================= Student =================

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public Student? Student { get; set; }

        // ================= Availability =================

        [Required]
        public string Day { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        // ================= Created =================

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}