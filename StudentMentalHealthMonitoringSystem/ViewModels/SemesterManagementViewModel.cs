using System;
using System.Collections.Generic;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class SemesterTransitionLogItem
    {
        public int LogId { get; set; }
        public string FromSemester { get; set; } = string.Empty;
        public string ToSemester { get; set; } = string.Empty;
        public int TotalStudentsCount { get; set; }
        public int PromotedStudentsCount { get; set; }
        public int BlockedStudentsCount { get; set; }
        public DateTime TransitionDate { get; set; } = DateTime.Now;
        public string InitiatedBy { get; set; } = "Admin Governance";
    }

    public class SemesterManagementViewModel
    {
        public string CurrentActiveSemester { get; set; } = "Semester 1";
        public string NextProposedSemester { get; set; } = "Semester 2";
        public DateTime SemesterStartDate { get; set; } = DateTime.Now.AddMonths(-4);
        public DateTime SemesterEndDate { get; set; } = DateTime.Now;

        public int TotalStudents { get; set; }
        public int CompliantStudents { get; set; }
        public int NonCompliantStudents { get; set; }
        public double CompliancePercentage => TotalStudents > 0 ? Math.Round((double)CompliantStudents / TotalStudents * 100, 1) : 0;

        public List<string> AvailableSemesters { get; set; } = new List<string>
        {
            "Semester 1", "Semester 2", "Semester 3", "Semester 4",
            "Semester 5", "Semester 6", "Semester 7", "Semester 8",
            "Semester 9", "Semester 10", "Semester 11", "Semester 12"
        };

        public List<StudentScreeningComplianceItem> BlockedStudents { get; set; } = new List<StudentScreeningComplianceItem>();
        public List<SemesterTransitionLogItem> TransitionLogs { get; set; } = new List<SemesterTransitionLogItem>();
    }
}
