using StudentMentalHealthMonitoringSystem.Models;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class GuardianDashboardViewModel
    {
        // ================= Student =================

        public Student Student { get; set; } = null!;


        // ================= PHQ-9 =================

        public List<PHQAssessment> PHQAssessments { get; set; }
            = new List<PHQAssessment>();


        // ================= C-SSRS =================

        public List<CSSRSAssessment> CSSRSAssessments { get; set; }
            = new List<CSSRSAssessment>();
    }
}