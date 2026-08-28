using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class PsychologistAvailability
    {
        [Key]
        public int PsychologistAvailabilityId { get; set; }

        // ================= Psychologist =================

        [Required]
        public int PsychologistId { get; set; }

        [ForeignKey("PsychologistId")]
        public Psychologist? Psychologist { get; set; }

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
