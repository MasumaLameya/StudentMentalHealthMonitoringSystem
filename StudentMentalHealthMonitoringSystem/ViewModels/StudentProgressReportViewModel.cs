using System;
using System.Collections.Generic;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class StudentProgressReportListViewModel
    {
        public string FollowUpFilter { get; set; } = "All"; // "All", "InProgress", "Completed"
        public string DepartmentFilter { get; set; } = "All";
        public List<string> AvailableDepartments { get; set; } = new List<string>();
        public List<StudentProgressReportSummaryItem> Reports { get; set; } = new List<StudentProgressReportSummaryItem>();
    }

    public class StudentProgressReportSummaryItem
    {
        public int ObservationReportId { get; set; }
        public int RootCounselingId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentIdNumber { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }

        public int PsychologistId { get; set; }
        public string PsychologistName { get; set; } = string.Empty;

        public bool IsFinal { get; set; } // true = Completed, false = In Progress
        public string FollowUpStatusText => IsFinal ? "Follow-up Completed" : "Continuous Follow-up (In Progress)";

        public int TotalSessions { get; set; }
        public double InitialScore { get; set; }
        public double LatestScore { get; set; }
        public string OverallImprovementStatus { get; set; } = "Stable"; // "Improved", "Stable", "Deteriorated"

        public DateTime FirstSessionDate { get; set; }
        public DateTime LatestSessionDate { get; set; }
    }

    public class StudentProgressReportDetailViewModel
    {
        public int ObservationReportId { get; set; }
        public int RootCounselingId { get; set; }

        // Student Info
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentIdNumber { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }

        // Psychologist Info
        public int PsychologistId { get; set; }
        public string PsychologistName { get; set; } = string.Empty;

        // Overall Progress Metrics
        public bool IsFinal { get; set; }
        public string FollowUpStatusText => IsFinal ? "Follow-up Completed" : "Continuous Follow-up (In Progress)";

        public double InitialScore { get; set; }
        public double LatestScore { get; set; }
        public double ScoreDifference => Math.Round(LatestScore - InitialScore, 1);
        public string OverallImprovementStatus { get; set; } = "Stable"; // "Improved", "Stable", "Deteriorated"
        public string OverallImprovementBadgeClass => OverallImprovementStatus switch
        {
            "Improved" => "bg-success",
            "Deteriorated" => "bg-danger",
            _ => "bg-warning text-dark"
        };

        public int TotalSessions { get; set; }
        public DateTime FirstSessionDate { get; set; }
        public DateTime LatestSessionDate { get; set; }

        // List of Session Progress Details
        public List<SessionProgressDetail> Sessions { get; set; } = new List<SessionProgressDetail>();
    }

    public class SessionProgressDetail
    {
        public int CounselingObservationId { get; set; }
        public int CounselingId { get; set; }
        public int SessionNumber { get; set; }
        public string SessionTitle => SessionNumber == 1 ? "Session 1 (Initial Assessment)" : $"Follow-up {SessionNumber - 1} (Session {SessionNumber})";
        public DateTime SessionDate { get; set; }
        public string CounselingTime { get; set; } = string.Empty;

        public double SessionScore { get; set; } // 0 to 100
        public double PreviousScore { get; set; }
        public double ScoreChange => SessionNumber == 1 ? 0 : Math.Round(SessionScore - PreviousScore, 1);
        public string SessionImprovementStatus { get; set; } = "Baseline"; // "Baseline", "Improved", "Stable", "Deteriorated"
        public string SessionBadgeClass => SessionImprovementStatus switch
        {
            "Improved" => "bg-success",
            "Deteriorated" => "bg-danger",
            "Stable" => "bg-info text-dark",
            _ => "bg-secondary"
        };

        // Observations Inputs
        public string OverallProgressStatus { get; set; } = string.Empty;
        public string MentalHealthStatus { get; set; } = string.Empty;
        public string SafetyRisk { get; set; } = string.Empty;
        public string AcademicFunctioning { get; set; } = string.Empty;
        public string SleepCondition { get; set; } = string.Empty;
        public string SocialInteraction { get; set; } = string.Empty;
        public string DailyActivities { get; set; } = string.Empty;
        public string EmotionalRegulation { get; set; } = string.Empty;
        public string ClinicalObservation { get; set; } = string.Empty;
        public string StudentReportedImprovement { get; set; } = string.Empty;
        public string AssessmentBasis { get; set; } = string.Empty;
        public string AssessmentSummary { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
        public int? PHQScore { get; set; }
        public string? PHQProjectStatus { get; set; }
        public string? CSSRSRiskLevel { get; set; }
    }
}
