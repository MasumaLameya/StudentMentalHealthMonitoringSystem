using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class VoiceBotRiskAssessment
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        public int VoiceBotRiskAssessmentId { get; set; }


        // =========================================================
        // VOICE BOT SESSION
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
        // RISK STATUS
        // Normal / Moderate / Severe / Extremely Severe
        // =========================================================

        [Required]
        [MaxLength(50)]
        public string RiskStatus { get; set; }
            = "Normal";


        // =========================================================
        // STATUS SUMMARY
        // Short summary of the conversation context
        // when this status was generated
        // =========================================================

        [Column(TypeName = "longtext")]
        public string? Summary { get; set; }


        // =========================================================
        // CREATED TIME
        // =========================================================

        [Required]
        public DateTime CreatedAt { get; set; }
            = DateTime.Now;
    }
}
