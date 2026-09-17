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
}
