using StudentMentalHealthMonitoringSystem.Models;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class AdminDashboardViewModel
    {
        // ================= Admin Information =================

        public Admin? Admin { get; set; }

        // ================= Dashboard Statistics =================

        public int TotalStudents { get; set; }

        public int HighRiskStudents { get; set; }

        public int TotalPsychologists { get; set; }

        public int TotalDepartments { get; set; }

        public int TotalCounselingSessions { get; set; }

        public int PendingCounselingSessions { get; set; }
    }
}