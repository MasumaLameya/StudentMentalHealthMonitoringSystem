using System;
using System.Collections.Generic;
using System.Linq;
using StudentMentalHealthMonitoringSystem.Models;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class StudentSessionHistoryItemViewModel
    {
        public int CounselingId { get; set; }
        public DateTime CounselingDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public TimeSpan AppointmentEndTime { get; set; }
        public string Status { get; set; } = "Confirmed";
        public string? AppointmentRoom { get; set; }
        public string? AppointmentSource { get; set; }
        public string? TriggerSource { get; set; }
        public string? TriggerSeverity { get; set; }
        public string? ObservationNote { get; set; }
        public bool CanCancel { get; set; }

        // Assigned Psychologist Info
        public int PsychologistId { get; set; }
        public string PsychologistName { get; set; } = string.Empty;
        public string? PsychologistSpecialization { get; set; }
        public string? PsychologistEmail { get; set; }
        public string? PsychologistProfileImage { get; set; }

        // Associated Individual Session Observation
        public CounselingObservation? Observation { get; set; }

        // Root Observation Report (if any)
        public ObservationReport? ObservationReport { get; set; }

        // Display Helpers
        public string FormattedDate => CounselingDate.ToString("dddd, MMM dd, yyyy");
        public string FormattedTimeSlot => $"{DateTime.Today.Add(AppointmentTime):h:mm tt} - {DateTime.Today.Add(AppointmentEndTime):h:mm tt}";

        public string StatusBadgeClass => Status switch
        {
            "Completed" => "bg-success text-white",
            "Confirmed" => "bg-warning text-dark",
            "Cancelled" => "bg-danger text-white",
            "Missed" => "bg-secondary text-white",
            _ => "bg-light text-dark border"
        };

        public string StatusIcon => Status switch
        {
            "Completed" => "bi-check-circle-fill",
            "Confirmed" => "bi-clock-fill",
            "Cancelled" => "bi-x-circle-fill",
            "Missed" => "bi-calendar-x-fill",
            _ => "bi-info-circle-fill"
        };
    }

    public class StudentSessionHistoryViewModel
    {
        public Student Student { get; set; } = null!;
        public List<StudentSessionHistoryItemViewModel> Sessions { get; set; } = new();

        public int TotalSessions => Sessions.Count;
        public int CompletedSessions => Sessions.Count(s => s.Status == "Completed");
        public int UpcomingSessions => Sessions.Count(s => s.Status == "Confirmed" || s.Status == "Pending");
        public int MissedSessions => Sessions.Count(s => s.Status == "Missed");
        public int CancelledSessions => Sessions.Count(s => s.Status == "Cancelled");
    }
}
