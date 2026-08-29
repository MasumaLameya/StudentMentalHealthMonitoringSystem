using System;
using System.Collections.Generic;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class AdminAIConversationItemViewModel
    {
        public int SessionId { get; set; }
        public string SessionType { get; set; } = "Chat"; // "Chat" or "VoiceBot"
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentIdNumber { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public string RiskStatus { get; set; } = "Normal";
        public string? Summary { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string DurationFormatted { get; set; } = string.Empty;
        public int MessageOrTranscriptCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class AdminAIReportsListViewModel
    {
        public string SelectedType { get; set; } = "All"; // "All", "Chat", "VoiceBot"
        public string SelectedRisk { get; set; } = "All"; // "All", "Normal", "Moderate", "Severe", "Extremely Severe"
        public string SelectedDepartment { get; set; } = "All";
        public string? SearchTerm { get; set; }
        public List<string> AvailableDepartments { get; set; } = new List<string>();

        // KPI Metrics
        public int TotalSessions { get; set; }
        public int TotalChatSessions { get; set; }
        public int TotalVoiceSessions { get; set; }
        public int SevereOrCriticalCount { get; set; }
        public int NightTimeSessionsCount { get; set; }

        public List<AdminAIConversationItemViewModel> Sessions { get; set; } = new List<AdminAIConversationItemViewModel>();
    }

    public class ChatMessageItemViewModel
    {
        public string Sender { get; set; } = string.Empty; // "Student" or "AI"
        public string MessageText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class VoiceTranscriptItemViewModel
    {
        public string Speaker { get; set; } = string.Empty; // "Student" or "VoiceBot"
        public string TranscriptText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AdminAIConversationDetailsViewModel
    {
        public int SessionId { get; set; }
        public string SessionType { get; set; } = "Chat"; // "Chat" or "VoiceBot"
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentIdNumber { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string DurationFormatted { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string RiskStatus { get; set; } = "Normal";
        public string? ClinicalSummary { get; set; }

        public List<ChatMessageItemViewModel> ChatMessages { get; set; } = new List<ChatMessageItemViewModel>();
        public List<VoiceTranscriptItemViewModel> VoiceTranscripts { get; set; } = new List<VoiceTranscriptItemViewModel>();
    }
}
