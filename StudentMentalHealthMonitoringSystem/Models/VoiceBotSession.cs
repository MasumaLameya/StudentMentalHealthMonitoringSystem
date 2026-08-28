using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class VoiceBotSession
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        public int VoiceBotSessionId { get; set; }


        // =========================================================
        // STUDENT
        // =========================================================

        [Required]
        public int StudentId { get; set; }

        public Student? Student { get; set; }


        // =========================================================
        // LIVE MODEL
        // =========================================================

        [Required]
        [MaxLength(100)]
        public string ModelName { get; set; }
            = "gemini-3.1-flash-live-preview";


        // =========================================================
        // CURRENT LIVE STATUS
        // Normal / Moderate / Severe / Extremely Severe
        // =========================================================

        [Required]
        [MaxLength(50)]
        public string CurrentStatus { get; set; }
            = "Normal";


        // =========================================================
        // CURRENT LIVE SUMMARY
        // Updated while the conversation continues
        // =========================================================

        [Column(TypeName = "longtext")]
        public string? CurrentSummary { get; set; }


        // =========================================================
        // SESSION TIME
        // =========================================================

        [Required]
        public DateTime StartedAt { get; set; }
            = DateTime.Now;


        public DateTime? EndedAt { get; set; }


        // =========================================================
        // LAST STATUS UPDATE
        // =========================================================

        public DateTime? LastStatusUpdatedAt { get; set; }


        // =========================================================
        // SESSION STATE
        // =========================================================

        [Required]
        public bool IsActive { get; set; }
            = true;
    }
}