using System;
using System.Collections.Generic;
using System.Linq;
using StudentMentalHealthMonitoringSystem.Models;
using StudentMentalHealthMonitoringSystem.ViewModels;

namespace StudentMentalHealthMonitoringSystem.Services
{
    public static class ProgressScoringService
    {
        /// <summary>
        /// Calculates a standardized recovery/progress score between 0.0 and 100.0 based on stage inputs in CounselingObservation.
        /// Higher score indicates optimal mental health / recovery. Lower score indicates higher risk / severity.
        /// </summary>
        public static double CalculateObservationScore(CounselingObservation obs)
        {
            if (obs == null) return 50.0;

            // 1. Current Mental Health Status (Max 25 pts)
            double mentalHealthPts = obs.CurrentMentalHealthStatus switch
            {
                "Normal" => 25.0,
                "Moderate" => 16.0,
                "Severe" => 8.0,
                "Extremely Severe" => 0.0,
                _ => 15.0
            };

            // 2. Current Safety Risk (Max 25 pts)
            double safetyRiskPts = obs.CurrentSafetyRisk switch
            {
                "No Current Risk" => 25.0,
                "Low Risk" => 18.0,
                "Moderate Risk" => 10.0,
                "High/Urgent Risk" => 0.0,
                _ => 15.0
            };

            // 3. Functional Status (5 components, Max 25 pts total -> 5 pts each)
            double FuncScore(string val) => val switch
            {
                "Normal" => 5.0,
                "Moderately Affected" => 3.3,
                "Severely Affected" => 1.6,
                "Extremely Affected" => 0.0,
                _ => 3.0
            };

            double functionalPts = FuncScore(obs.AcademicFunctioning)
                                 + FuncScore(obs.SleepCondition)
                                 + FuncScore(obs.SocialInteraction)
                                 + FuncScore(obs.DailyActivities)
                                 + FuncScore(obs.EmotionalRegulation);

            // 4. Progress & Student Reported Improvement (Max 25 pts -> 12.5 pts each)
            double overallProgressPts = obs.OverallProgressStatus switch
            {
                "Improving" => 12.5,
                "Stable" => 9.5,
                "Partially Improved" => 9.5,
                "No Significant Change" => 6.0,
                "Deteriorating" => 0.0,
                _ => 6.0
            };

            double studentImprovementPts = obs.StudentReportedImprovement switch
            {
                "Significantly Improved" => 12.5,
                "Partially Improved" => 9.5,
                "No Change" => 6.0,
                "Unable to Determine" => 6.0,
                "Condition Worsened" => 0.0,
                _ => 6.0
            };

            double totalScore = mentalHealthPts + safetyRiskPts + functionalPts + overallProgressPts + studentImprovementPts;
            return Math.Round(Math.Clamp(totalScore, 0.0, 100.0), 1);
        }

        /// <summary>
        /// Compares current session score vs previous session score to return session improvement indicator.
        /// </summary>
        public static string DetermineSessionImprovement(double currentScore, double previousScore, bool isFirstSession)
        {
            if (isFirstSession) return "Initial";

            double diff = currentScore - previousScore;
            if (diff >= 2.0) return "Improved";
            if (diff <= -2.0) return "Deteriorated";
            return "Stable";
        }

        /// <summary>
        /// Compares latest session score vs initial session score to return overall improvement indicator.
        /// </summary>
        public static string DetermineOverallImprovement(double latestScore, double initialScore)
        {
            double diff = latestScore - initialScore;
            if (diff >= 3.0) return "Improved";
            if (diff <= -3.0) return "Deteriorated";
            return "Stable";
        }

        /// <summary>
        /// Builds complete StudentProgressReportDetailViewModel from ObservationReport and associated list of CounselingObservations.
        /// </summary>
        public static StudentProgressReportDetailViewModel BuildDetailViewModel(
            ObservationReport report,
            List<CounselingObservation> observations)
        {
            var sortedObservations = observations
                .OrderBy(o => o.Counseling?.CounselingDate ?? o.CreatedAt)
                .ThenBy(o => o.Counseling?.AppointmentTime ?? TimeSpan.Zero)
                .ToList();

            var sessionDetails = new List<SessionProgressDetail>();
            double prevScore = 0.0;

            for (int i = 0; i < sortedObservations.Count; i++)
            {
                var obs = sortedObservations[i];
                double score = CalculateObservationScore(obs);
                string impStatus = DetermineSessionImprovement(score, prevScore, i == 0);
                prevScore = score;

                var counselingDate = obs.Counseling?.CounselingDate ?? obs.CreatedAt;
                var counselingTimeStr = obs.Counseling != null
                    ? DateTime.Today.Add(obs.Counseling.AppointmentTime).ToString("h:mm tt")
                    : "-";

                sessionDetails.Add(new SessionProgressDetail
                {
                    CounselingObservationId = obs.CounselingObservationId,
                    CounselingId = obs.CounselingId,
                    SessionNumber = i + 1,
                    SessionDate = counselingDate,
                    CounselingTime = counselingTimeStr,
                    SessionScore = score,
                    SessionImprovementStatus = impStatus,
                    OverallProgressStatus = obs.OverallProgressStatus,
                    MentalHealthStatus = obs.CurrentMentalHealthStatus,
                    SafetyRisk = obs.CurrentSafetyRisk,
                    AcademicFunctioning = obs.AcademicFunctioning,
                    SleepCondition = obs.SleepCondition,
                    SocialInteraction = obs.SocialInteraction,
                    DailyActivities = obs.DailyActivities,
                    EmotionalRegulation = obs.EmotionalRegulation,
                    ClinicalObservation = obs.ClinicalObservation,
                    StudentReportedImprovement = obs.StudentReportedImprovement
                });
            }

            double initialScore = sessionDetails.Any() ? sessionDetails.First().SessionScore : 50.0;
            double latestScore = sessionDetails.Any() ? sessionDetails.Last().SessionScore : 50.0;
            string overallImprovement = DetermineOverallImprovement(latestScore, initialScore);

            return new StudentProgressReportDetailViewModel
            {
                ObservationReportId = report.ObservationReportId,
                RootCounselingId = report.RootCounselingId,
                StudentId = report.StudentId,
                StudentName = report.Student?.FullName ?? "Student",
                StudentIdNumber = report.Student?.StudentIdNumber ?? "-",
                Department = report.Student?.Department ?? "-",
                Semester = report.Student?.Semester ?? "-",
                Email = report.Student?.Email ?? "-",
                Phone = report.Student?.Phone ?? "-",
                ProfileImage = report.Student?.ProfileImage,
                PsychologistId = report.PsychologistId,
                PsychologistName = report.Psychologist?.FullName ?? "Psychologist",
                IsFinal = report.IsFinal,
                InitialScore = initialScore,
                LatestScore = latestScore,
                OverallImprovementStatus = overallImprovement,
                TotalSessions = sessionDetails.Count,
                FirstSessionDate = sessionDetails.FirstOrDefault()?.SessionDate ?? report.CreatedAt,
                LatestSessionDate = sessionDetails.LastOrDefault()?.SessionDate ?? report.UpdatedAt,
                Sessions = sessionDetails
            };
        }
    }
}
