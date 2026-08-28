using System.Collections.Generic;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class ScreeningAnalyticsViewModel
    {
        // Filters
        public string SelectedSemester { get; set; } = string.Empty;
        public List<string> AvailableSemesters { get; set; } = new List<string>();

        public string SelectedDepartment { get; set; } = string.Empty; // "All" or specific department name
        public List<string> AvailableDepartments { get; set; } = new List<string>();

        public bool IsAdmin { get; set; } = true;
        public string? UserDepartment { get; set; }

        // Overall Summary Metrics
        public int TotalStudentsInScope { get; set; }
        public int TotalScreeningsConducted { get; set; }
        public int TotalScreenedStudents { get; set; }

        // Overall Severity Counts
        public int NormalCount { get; set; }
        public int ModerateCount { get; set; }
        public int SevereCount { get; set; }
        public int ExtremelySevereCount { get; set; }

        // Overall Severity Percentages
        public double NormalPercentage { get; set; }
        public double ModeratePercentage { get; set; }
        public double SeverePercentage { get; set; }
        public double ExtremelySeverePercentage { get; set; }

        // Department-wise Breakdown List for Comparison Chart & Table
        public List<DepartmentSeveritySummary> DepartmentBreakdowns { get; set; } = new List<DepartmentSeveritySummary>();
    }

    public class DepartmentSeveritySummary
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int TotalScreened { get; set; }

        public int NormalCount { get; set; }
        public int ModerateCount { get; set; }
        public int SevereCount { get; set; }
        public int ExtremelySevereCount { get; set; }

        public double NormalPercentage { get; set; }
        public double ModeratePercentage { get; set; }
        public double SeverePercentage { get; set; }
        public double ExtremelySeverePercentage { get; set; }
    }
}
