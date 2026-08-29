using System;
using System.Collections.Generic;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    // ==========================================
    // SCREENING CLEARANCE VIEW MODEL
    // ==========================================
    public class StudentScreeningClearanceViewModel
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string StudentIdNumber { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string SelectedSemester { get; set; } = string.Empty;
        public List<string> AvailableSemesters { get; set; } = new List<string>();

        public DateTime CheckedDate { get; set; } = DateTime.Now;

        public bool HasCompletedPHQ { get; set; }
        public DateTime? PHQCompletionDate { get; set; }
        public string PHQSeverityLevel { get; set; } = "Not Taken";

        public bool HasCompletedCSSRS { get; set; }
        public DateTime? CSSRSCompletionDate { get; set; }
        public string CSSRSRiskLevel { get; set; } = "Not Taken";

        public bool IsCleared => HasCompletedPHQ && HasCompletedCSSRS;
        public string ClearanceStatus => IsCleared ? "Cleared for Registration" : "Clearance Pending";
        public string AdministrativeRemarks { get; set; } = string.Empty;
    }

    // ==========================================
    // AI TELEHEALTH & COPING HISTORY VIEW MODELS
    // ==========================================
    public class StudentAISessionItemViewModel
    {
        public int SessionId { get; set; }
        public string SessionType { get; set; } = "AI Chat"; // "AI Chat" or "Voice Bot"
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string DurationText { get; set; } = string.Empty;
        public int TotalExchanges { get; set; }
        public string RiskStatus { get; set; } = "Normal";
        public string Summary { get; set; } = string.Empty;
        public string CopingAdvice { get; set; } = string.Empty;
    }

    public class StudentAIHistoryViewModel
    {
        public int TotalSessions { get; set; }
        public int TotalChatSessions { get; set; }
        public int TotalVoiceSessions { get; set; }
        public string DominantEmotionalState { get; set; } = "Stable";
        public string SelectedType { get; set; } = "All"; // "All", "Chat", "Voice"

        public List<StudentAISessionItemViewModel> Sessions { get; set; } = new List<StudentAISessionItemViewModel>();
    }

    public class StudentAIDetailsViewModel
    {
        public int SessionId { get; set; }
        public string SessionType { get; set; } = "AI Chat";
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string RiskStatus { get; set; } = "Normal";
        public string Summary { get; set; } = string.Empty;
        public string CopingAdvice { get; set; } = string.Empty;

        public List<ChatMessageItemViewModel> ChatMessages { get; set; } = new List<ChatMessageItemViewModel>();
        public List<VoiceTranscriptItemViewModel> VoiceTranscripts { get; set; } = new List<VoiceTranscriptItemViewModel>();
    }
}
