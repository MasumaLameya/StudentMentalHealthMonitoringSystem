using System.Collections.Generic;
using StudentMentalHealthMonitoringSystem.Models;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class DepartmentViewModel
    {
        // =========================================================
        // DEPARTMENT INFORMATION
        // =========================================================

        public string DepartmentName { get; set; }
            = string.Empty;


        // =========================================================
        // TOTAL STUDENTS
        // =========================================================

        public int TotalStudents { get; set; }


        // =========================================================
        // HIGH-RISK STUDENTS
        // =========================================================

        public int HighRiskStudents { get; set; }


        // =========================================================
        // TOTAL COUNSELING SESSIONS
        // =========================================================

        public int TotalCounselingSessions { get; set; }


        // =========================================================
        // UPCOMING FOLLOW-UPS
        // =========================================================

        public int UpcomingFollowUps { get; set; }


        // =========================================================
        // UPCOMING COUNSELING APPOINTMENTS
        // =========================================================

        public List<Counseling> UpcomingCounselings { get; set; }
            = new List<Counseling>();
    }
}