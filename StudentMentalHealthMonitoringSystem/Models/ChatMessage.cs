using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class ChatMessage
    {
        // ================= Primary Key =================

        [Key]
        public int ChatMessageId { get; set; }


        // ================= Chat Session =================

        [Required]
        public int ChatSessionId { get; set; }


        [ForeignKey(nameof(ChatSessionId))]
        public ChatSession? ChatSession { get; set; }


        // ================= Sender =================
        // Possible values:
        // Student
        // AI

        [Required]
        [StringLength(20)]
        public string Sender { get; set; } =
            string.Empty;


        // ================= Message =================

        [Required]
        [Column(TypeName = "text")]
        public string MessageText { get; set; } =
            string.Empty;


        // ================= Message Time =================

        [Required]
        public DateTime CreatedAt { get; set; } =
            DateTime.Now;
    }
}
