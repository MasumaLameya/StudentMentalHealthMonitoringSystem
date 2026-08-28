using System;
using System.Collections.Generic;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    // =========================================================
    // DEPARTMENT RISK REPORT VIEW MODEL
    // =========================================================
    // This ViewModel contains the complete High-Risk report
    // information for the logged-in department.
    // =========================================================

    public class DepartmentRiskReportViewModel
    {
        // =====================================================
        // DEPARTMENT INFORMATION
        // =====================================================

        public string DepartmentName { get; set; }
            = string.Empty;


        // =====================================================
        // TOTAL STUDENTS
        // =====================================================

        public int TotalStudents { get; set; }


        // =====================================================
        // HIGH-RISK STUDENTS
        // =====================================================
        // A student is included when a serious screening result
        // has Severe or Extremely Severe project severity.
        // =====================================================

        public int HighRiskStudents { get; set; }


        // =====================================================
        // HIGH-RISK STUDENT LIST
        // =====================================================

        public List<DepartmentHighRiskStudentViewModel> Students
        { get; set; }
            = new List<DepartmentHighRiskStudentViewModel>();
    }



    // =========================================================
    // INDIVIDUAL HIGH-RISK STUDENT INFORMATION
    // =========================================================

    public class DepartmentHighRiskStudentViewModel
    {
        // =====================================================
        // STUDENT ID
        // =====================================================

        public int StudentId { get; set; }


        // =====================================================
        // STUDENT ID NUMBER
        // =====================================================

        public string StudentIdNumber { get; set; }
            = string.Empty;


        // =====================================================
        // STUDENT NAME
        // =====================================================

        public string FullName { get; set; }
            = string.Empty;


        // =====================================================
        // STUDENT EMAIL
        // =====================================================

        public string Email { get; set; }
            = string.Empty;


        // =====================================================
        // GUARDIAN NAME
        // =====================================================

        public string? GuardianName { get; set; }


        // =====================================================
        // GUARDIAN EMAIL
        // =====================================================

        public string? GuardianEmail { get; set; }


        // =====================================================
        // HIGH-RISK SEMESTER
        // =====================================================

        public string HighRiskSemester { get; set; }
            = string.Empty;


        // =====================================================
        // RISK LEVEL
        // =====================================================

        public string RiskLevel { get; set; }
            = string.Empty;


        // =====================================================
        // ASSESSMENT DATE
        // =====================================================

        public DateTime AssessmentDate { get; set; }


        // =====================================================
        // TRIGGER SOURCE
        // =====================================================

        public string? TriggerSource { get; set; }


        // =====================================================
        // TRIGGER SEVERITY
        // =====================================================

        public string? TriggerSeverity { get; set; }
    }
}