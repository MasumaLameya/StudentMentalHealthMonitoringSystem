using System.Collections.Generic;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class PsychologistWorkloadItemViewModel
    {
        public int PsychologistId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public int TotalAssignedCases { get; set; }
        public int CompletedSessions { get; set; }
        public int ConfirmedPendingSessions { get; set; }
        public int CancelledSessions { get; set; }
        public int FollowUpCasesCount { get; set; }
        public double CapacityUtilizationPercentage { get; set; }
    }

    public class SlotOccupancyViewModel
    {
        public string SlotName { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public double PercentageOfTotal { get; set; }
    }

    public class PsychologistWorkloadReportViewModel
    {
        public int TotalPsychologists { get; set; }
        public int TotalCounselingSessions { get; set; }
        public int CompletedSessions { get; set; }
        public int ConfirmedPendingSessions { get; set; }
        public double AverageSessionsPerPsychologist { get; set; }

        public List<SlotOccupancyViewModel> SlotOccupancies { get; set; } = new List<SlotOccupancyViewModel>();
        public List<PsychologistWorkloadItemViewModel> Psychologists { get; set; } = new List<PsychologistWorkloadItemViewModel>();
    }
}
