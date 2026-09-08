using System;
using System.Collections.Generic;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class StudentScreeningComplianceItem
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // PHQ-9 Status
        public bool HasPHQ { get; set; }
        public int? PHQScore { get; set; }
        public string PHQSeverity { get; set; } = "Pending";
        public DateTime? PHQDate { get; set; }

        // C-SSRS Status
        public bool HasCSSRS { get; set; }
        public string CSSRSRiskLevel { get; set; } = "Pending";
        public DateTime? CSSRSDate { get; set; }

        // Overall Compliance & Clinical Rule
        public bool IsPHQSevere => HasPHQ && Services.ScreeningComplianceService.IsPHQSevereLevel(PHQSeverity, PHQScore);
        public bool IsCSSRSSevere => HasCSSRS && Services.ScreeningComplianceService.IsCSSRSSevereLevel(CSSRSRiskLevel);
        public bool IsFullyScreened => (HasPHQ && HasCSSRS) || IsPHQSevere || IsCSSRSSevere;
        public bool HasPendingWarning => !IsFullyScreened;
        public string ComplianceStatus => IsFullyScreened ? "Completed" : "Pending";
        public string ComplianceStatusDescription
        {
            get
            {
                if (HasPHQ && HasCSSRS) return "Completed (Both Modules)";
                if (IsPHQSevere) return "Evaluated (PHQ-9 Severe Indicator)";
                if (IsCSSRSSevere) return "Evaluated (C-SSRS Safety Indicator)";
                if (HasPHQ && !HasCSSRS) return "PHQ-9 Normal — C-SSRS Required";
                if (HasCSSRS && !HasPHQ) return "C-SSRS Normal — PHQ-9 Required";
                return "Both Modules Pending";
            }
        }
    }

    public class ScreeningComplianceViewModel
    {
        public string Title { get; set; } = "Semester Screening Compliance";
        public string SelectedDepartment { get; set; } = "All";
        public string SelectedSemester { get; set; } = "All";
        public string SelectedStatus { get; set; } = "All";

        public int TotalStudents { get; set; }
        public int CompletedStudents { get; set; }
        public int PendingStudents { get; set; }
        public double CompliancePercentage => TotalStudents > 0 ? Math.Round((double)CompletedStudents / TotalStudents * 100, 1) : 0;

        public List<StudentScreeningComplianceItem> Students { get; set; } = new List<StudentScreeningComplianceItem>();
        public List<string> AvailableSemesters { get; set; } = new List<string>();
        public List<string> AvailableDepartments { get; set; } = new List<string>();
    }
}
