using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class ChatRiskAssessment
    {
        // ================= Primary Key =================

        [Key]
        public int ChatRiskAssessmentId { get; set; }


        // ================= Chat Session =================

        [Required]
        public int ChatSessionId { get; set; }


        [ForeignKey(nameof(ChatSessionId))]
        public ChatSession? ChatSession { get; set; }


        // ================= Student =================

        [Required]
        public int StudentId { get; set; }


        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; set; }


        // ================= Chatbot Assessment Status =================
        //
        // Unified project-level values:
        //
        // Normal
        // Moderate
        // Severe
        // Extremely Severe
        //

        [Required]
        [StringLength(50)]
        public string RiskStatus { get; set; } =
            "Normal";


        // ================= Assessment Summary =================
        //
        // Short AI-generated summary explaining the
        // overall emotional context observed in the chat.
        //
        // When Severe or Extremely Severe is detected,
        // this summary will be used in the Screening Report
        // sent to the psychologist assigned to the
        // automatic counseling appointment.
        //

        [Column(TypeName = "text")]
        public string? Summary { get; set; }


        // ================= Assessment Date =================

        [Required]
        public DateTime CreatedAt { get; set; } =
            DateTime.Now;
    }
}