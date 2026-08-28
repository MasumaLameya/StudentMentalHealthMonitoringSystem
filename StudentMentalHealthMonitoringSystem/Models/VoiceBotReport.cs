using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class VoiceBotReport
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        public int VoiceBotReportId { get; set; }


        // =========================================================
        // VOICE BOT SESSION
        // One report for one voice session
        // =========================================================

        [Required]
        public int VoiceBotSessionId { get; set; }

        public VoiceBotSession? VoiceBotSession { get; set; }


        // =========================================================
        // STUDENT
        // =========================================================

        [Required]
        public int StudentId { get; set; }

        public Student? Student { get; set; }


        // =========================================================
        // CURRENT LIVE STATUS
        // Normal / Moderate / Severe / Extremely Severe
        // Continuously updated during the live conversation
        // =========================================================

        [Required]
        [MaxLength(50)]
        public string CurrentStatus { get; set; }
            = "Normal";


        // =========================================================
        // CURRENT LIVE SUMMARY
        // Continuously updated during the live conversation
        // =========================================================

        [Column(TypeName = "longtext")]
        public string? CurrentSummary { get; set; }


        // =========================================================
        // LAST LIVE REPORT UPDATE
        // =========================================================

        [Required]
        public DateTime LastUpdatedAt { get; set; }
            = DateTime.Now;


        // =========================================================
        // FINAL STATUS
        // Set when the live voice session ends
        // =========================================================

        [MaxLength(50)]
        public string? FinalStatus { get; set; }


        // =========================================================
        // FINAL SUMMARY
        // Set when the live voice session ends
        // =========================================================

        [Column(TypeName = "longtext")]
        public string? FinalSummary { get; set; }


        // =========================================================
        // REPORT STATE
        // =========================================================

        [Required]
        public bool IsFinal { get; set; }
            = false;


        // =========================================================
        // FINALIZED TIME
        // =========================================================

        public DateTime? FinalizedAt { get; set; }
    }
}