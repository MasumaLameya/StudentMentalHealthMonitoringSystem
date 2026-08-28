using StudentMentalHealthMonitoringSystem.Models;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class PsychologistStudentViewModel
    {
        // ================= Student Information =================

        public Student Student { get; set; }

        // ================= PHQ Assessment =================

        public PHQAssessment? PHQAssessment { get; set; }

        // ================= C-SSRS Assessment =================

        public CSSRSAssessment? CSSRSAssessment { get; set; }

        // ================= Semester Record =================

        public StudentSemesterRecord? SemesterRecord { get; set; }

        // ================= Assignment Trigger Source =================

        public string? TriggerSource { get; set; }

        // ================= Assignment Trigger Severity =================

        public string? TriggerSeverity { get; set; }
    }
}