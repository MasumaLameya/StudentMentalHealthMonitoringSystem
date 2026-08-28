using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class ChatSession
    {
        // ================= Primary Key =================

        [Key]
        public int ChatSessionId { get; set; }


        // ================= Student =================

        [Required]
        public int StudentId { get; set; }


        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; set; }


        // ================= Session Start =================

        [Required]
        public DateTime StartedAt { get; set; } =
            DateTime.Now;


        // ================= Session End =================

        public DateTime? EndedAt { get; set; }


        // ================= Active Status =================

        public bool IsActive { get; set; } =
            true;


        // =====================================================
        // CONVERSATION MEMORY SUMMARY
        // =====================================================
        // Older conversation context can be summarized here.
        // Gemini can receive this summary together with
        // recent chat messages to preserve conversation context.
        // =====================================================

        [Column(TypeName = "text")]
        public string? Summary { get; set; }


        // ================= Created Messages =================

        public ICollection<ChatMessage> ChatMessages { get; set; }
            = new List<ChatMessage>();


        // ================= Risk Assessments =================

        public ICollection<ChatRiskAssessment> RiskAssessments { get; set; }
            = new List<ChatRiskAssessment>();
    }
}