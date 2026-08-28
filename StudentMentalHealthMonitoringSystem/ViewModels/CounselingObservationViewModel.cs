using StudentMentalHealthMonitoringSystem.Models;
using System;
using System.Collections.Generic;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class CounselingObservationViewModel
    {
        // =========================================================
        // COUNSELING
        // =========================================================

        public int CounselingId { get; set; }

        public Counseling? Counseling { get; set; }


        // =========================================================
        // LATEST PHQ-9
        // =========================================================

        public int? LatestPHQScore { get; set; }

        public string? LatestPHQOfficialInterpretation { get; set; }

        public string? LatestPHQProjectStatus { get; set; }


        // =========================================================
        // LATEST C-SSRS
        // =========================================================

        public string? LatestCSSRSRiskLevel { get; set; }

        public string? LatestCSSRSProjectStatus { get; set; }


        // =========================================================
        // 1. OVERALL PROGRESS STATUS
        // =========================================================

        public string? OverallProgressStatus { get; set; }


        // =========================================================
        // 2. CURRENT MENTAL HEALTH STATUS
        // =========================================================

        public string? CurrentMentalHealthStatus { get; set; }


        // =========================================================
        // 4. FUNCTIONAL STATUS
        // =========================================================

        public string? AcademicFunctioning { get; set; }

        public string? SleepCondition { get; set; }

        public string? SocialInteraction { get; set; }

        public string? DailyActivities { get; set; }

        public string? EmotionalRegulation { get; set; }


        // =========================================================
        // 5. CURRENT SAFETY RISK
        // =========================================================

        public string? CurrentSafetyRisk { get; set; }


        // =========================================================
        // 6. ASSESSMENT BASIS
        // Multiple selections
        // =========================================================

        public List<string> AssessmentBasis { get; set; }
            = new List<string>();


        // =========================================================
        // 7. CLINICAL OBSERVATION
        // =========================================================

        public string? ClinicalObservation { get; set; }


        // =========================================================
        // 8. STUDENT-REPORTED IMPROVEMENT
        // =========================================================

        public string? StudentReportedImprovement { get; set; }


        // =========================================================
        // 9. ASSESSMENT SUMMARY
        // =========================================================

        public string? AssessmentSummary { get; set; }


        // =========================================================
        // 10. RECOMMENDED ACTION
        // Multiple selections
        // =========================================================

        public List<string> RecommendedAction { get; set; }
            = new List<string>();


        // =========================================================
        // 11. FOLLOW-UP
        // =========================================================

        public bool? FollowUpRequired { get; set; }

        public DateTime? NextFollowUpDate { get; set; }

        public TimeSpan? NextFollowUpTime { get; set; }

        public string? AppointmentRoom { get; set; }
    }
}
