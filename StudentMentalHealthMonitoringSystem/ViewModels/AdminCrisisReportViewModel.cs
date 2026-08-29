using System;
using System.Collections.Generic;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class CrisisEscalationItemViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentIdNumber { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public string TriggerSource { get; set; } = string.Empty;
        public string SeverityLevel { get; set; } = string.Empty;
        public string TriggerDetails { get; set; } = string.Empty;
        public DateTime TriggerDate { get; set; }
        public string? AssignedPsychologistName { get; set; }
        public int? CounselingId { get; set; }
        public string CounselingStatus { get; set; } = "Unassigned";
        public DateTime? CounselingDate { get; set; }
        public bool IsOverdue { get; set; }
    }

    public class AdminCrisisReportViewModel
    {
        public string SelectedSource { get; set; } = "All";
        public string SelectedDepartment { get; set; } = "All";
        public string SelectedStatus { get; set; } = "All";
        public List<string> AvailableDepartments { get; set; } = new List<string>();

        // KPI metrics
        public int TotalCrisisEvents { get; set; }
        public int ExtremelySevereCount { get; set; }
        public int OverdueInterventionsCount { get; set; }
        public int ResolvedInterventionsCount { get; set; }

        public List<CrisisEscalationItemViewModel> Items { get; set; } = new List<CrisisEscalationItemViewModel>();
    }
}
