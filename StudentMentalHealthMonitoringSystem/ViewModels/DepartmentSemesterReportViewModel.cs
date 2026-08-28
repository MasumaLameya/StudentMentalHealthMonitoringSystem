using System;
using System.Collections.Generic;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class DepartmentSemesterReportViewModel
    {
        public int ReportId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string SemesterTitle { get; set; } = string.Empty;
        public DateTime ReportGeneratedDate { get; set; } = DateTime.Now;

        // Overall Student Counts
        public int TotalStudents { get; set; }
        public int PromotedStudents { get; set; }
        public int BlockedStudents { get; set; }
        public double CompliancePercentage => TotalStudents > 0 ? Math.Round((double)PromotedStudents / TotalStudents * 100, 1) : 0;

        // Risk Level Breakdown
        public int NormalRiskCount { get; set; }
        public int ModerateRiskCount { get; set; }
        public int SevereRiskCount { get; set; }
        public int ExtremelySevereRiskCount { get; set; }

        // Counseling & Screening Stats
        public int TotalCounselingSessions { get; set; }
        public int ActiveObservationReportsCount { get; set; }
        public int ImprovedPatientsCount { get; set; }

        // Summary Text
        public string ExecutiveSummary { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;

        // Breakdown List of Non-Compliant Students
        public List<StudentScreeningComplianceItem> NonCompliantStudents { get; set; } = new List<StudentScreeningComplianceItem>();
    }
}
