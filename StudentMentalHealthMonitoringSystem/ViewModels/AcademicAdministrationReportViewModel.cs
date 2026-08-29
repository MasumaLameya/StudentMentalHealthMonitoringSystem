using System.Collections.Generic;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class DepartmentAcademicStatusItem
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int ClearedStudents { get; set; }
        public int BlockedStudents { get; set; }
        public int HighRiskStudents { get; set; }
        public double ClearancePercentage { get; set; }
        public int NoticesDispatched { get; set; }
    }

    public class StudentAcademicRosterItem
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentIdNumber { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public string ScreeningStatus { get; set; } = "Pending"; // "Completed" or "Non-Compliant"
        public string RegistrationClearance { get; set; } = "Blocked"; // "Cleared" or "Blocked"
        public string MentalHealthSeverity { get; set; } = "Normal";
        public bool HasPHQ { get; set; }
        public bool HasCSSRS { get; set; }
        public string ActionRequired { get; set; } = string.Empty;
    }

    public class AcademicAdministrationReportViewModel
    {
        public string SelectedDepartment { get; set; } = "All";
        public string SelectedStatus { get; set; } = "All"; // "All", "Cleared", "Blocked"
        public string SelectedSemester { get; set; } = "Overall";
        public string? SearchTerm { get; set; }

        public List<string> AvailableDepartments { get; set; } = new List<string>();
        public List<string> AvailableSemesters { get; set; } = new List<string>();

        // KPI Metrics
        public int TotalCampusStudents { get; set; }
        public int TotalClearedStudents { get; set; }
        public int TotalBlockedStudents { get; set; }
        public int TotalHighRiskMonitored { get; set; }
        public double OverallCampusClearanceRate { get; set; }

        public List<DepartmentAcademicStatusItem> DepartmentStatuses { get; set; } = new List<DepartmentAcademicStatusItem>();
        public List<StudentAcademicRosterItem> StudentRoster { get; set; } = new List<StudentAcademicRosterItem>();
    }
}
