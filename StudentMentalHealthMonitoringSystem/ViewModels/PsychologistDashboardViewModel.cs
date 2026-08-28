using StudentMentalHealthMonitoringSystem.Models;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class PsychologistDashboardViewModel
    {
        // ================= Logged-in Psychologist =================

        public Psychologist? Psychologist { get; set; }

        // ================= Dashboard Statistics =================

        public int HighRiskStudents { get; set; }

        public int TodaySessions { get; set; }

        public int CompletedSessions { get; set; }

        public int PendingSessions { get; set; }
    }
}