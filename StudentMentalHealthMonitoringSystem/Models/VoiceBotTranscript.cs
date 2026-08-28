using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class VoiceBotTranscript
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        public int VoiceBotTranscriptId { get; set; }


        // =========================================================
        // VOICE BOT SESSION
        // =========================================================

        [Required]
        public int VoiceBotSessionId { get; set; }

        public VoiceBotSession? VoiceBotSession { get; set; }


        // =========================================================
        // SPEAKER
        // Student / VoiceBot
        // =========================================================

        [Required]
        [MaxLength(30)]
        public string Speaker { get; set; }
            = string.Empty;


        // =========================================================
        // TRANSCRIPT TEXT
        // Text received from the live voice conversation
        // =========================================================

        [Required]
        [Column(TypeName = "longtext")]
        public string TranscriptText { get; set; }
            = string.Empty;


        // =========================================================
        // CREATED TIME
        // =========================================================

        [Required]
        public DateTime CreatedAt { get; set; }
            = DateTime.Now;
    }
}