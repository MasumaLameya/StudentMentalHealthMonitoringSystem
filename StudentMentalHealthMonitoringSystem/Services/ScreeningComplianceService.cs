using System;
using System.Collections.Generic;
using System.Linq;

namespace StudentMentalHealthMonitoringSystem.Services
{
    public class ScreeningStatusResult
    {
        public bool IsScreeningComplete { get; set; }
        public bool IsCleared => IsScreeningComplete;
        public bool HasPHQ { get; set; }
        public bool HasCSSRS { get; set; }
        public bool IsPHQSevere { get; set; }
        public bool IsCSSRSSevere { get; set; }
        public string ComplianceStatus { get; set; } = "Pending"; // "Completed" or "Pending"
        public string StatusBadgeText { get; set; } = string.Empty;
        public string WarningReason { get; set; } = string.Empty;
        public string PendingReason => !string.IsNullOrEmpty(WarningReason) ? WarningReason : StatusBadgeText;
        public string WarningTitle { get; set; } = string.Empty;
        public bool RequiresCSSRSBecausePHQNormal { get; set; }
        public bool RequiresPHQBecauseCSSRSNormal { get; set; }
    }

    public static class ScreeningComplianceService
    {
        /// <summary>
        /// Returns true if PHQ-9 indicates severe or moderately severe depression.
        /// </summary>
        public static bool IsPHQSevereLevel(string? severityLevel, int? score = null)
        {
            if (string.IsNullOrWhiteSpace(severityLevel)) return false;
            return severityLevel.Equals("Severe", StringComparison.OrdinalIgnoreCase) ||
                   severityLevel.Equals("Moderately Severe", StringComparison.OrdinalIgnoreCase) ||
                   severityLevel.Equals("Extremely Severe", StringComparison.OrdinalIgnoreCase) ||
                   (score.HasValue && score.Value >= 15);
        }

        /// <summary>
        /// Returns true if C-SSRS indicates moderate, severe, or high suicide safety risk.
        /// </summary>
        public static bool IsCSSRSSevereLevel(string? riskLevel)
        {
            if (string.IsNullOrWhiteSpace(riskLevel)) return false;
            return riskLevel.Equals("High", StringComparison.OrdinalIgnoreCase) ||
                   riskLevel.Equals("Moderate", StringComparison.OrdinalIgnoreCase) ||
                   riskLevel.Equals("Severe", StringComparison.OrdinalIgnoreCase) ||
                   riskLevel.Equals("Extremely Severe", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Evaluates screening completion and warning status based on clinical rules:
        /// 1. If either PHQ-9 or C-SSRS is filled and indicates severe/high risk, the other is optional (evaluated).
        /// 2. If one is filled and normal/mild, the other MUST be filled.
        /// 3. If both are filled, screening is completed.
        /// </summary>
        public static ScreeningStatusResult Evaluate(
            bool hasPHQ,
            string? phqSeverity,
            int? phqScore,
            bool hasCSSRS,
            string? cssrsRiskLevel)
        {
            bool phqSevere = hasPHQ && IsPHQSevereLevel(phqSeverity, phqScore);
            bool cssrsSevere = hasCSSRS && IsCSSRSSevereLevel(cssrsRiskLevel);

            var result = new ScreeningStatusResult
            {
                HasPHQ = hasPHQ,
                HasCSSRS = hasCSSRS,
                IsPHQSevere = phqSevere,
                IsCSSRSSevere = cssrsSevere
            };

            if (hasPHQ && hasCSSRS)
            {
                result.IsScreeningComplete = true;
                result.ComplianceStatus = "Completed";
                result.StatusBadgeText = "Completed (Both Assessments)";
            }
            else if (phqSevere)
            {
                result.IsScreeningComplete = true;
                result.ComplianceStatus = "Completed";
                result.StatusBadgeText = "Evaluated (PHQ-9 Severe Indicator)";
            }
            else if (cssrsSevere)
            {
                result.IsScreeningComplete = true;
                result.ComplianceStatus = "Completed";
                result.StatusBadgeText = "Evaluated (C-SSRS Safety Indicator)";
            }
            else if (hasPHQ && !hasCSSRS)
            {
                result.IsScreeningComplete = false;
                result.ComplianceStatus = "Pending";
                result.RequiresCSSRSBecausePHQNormal = true;
                result.StatusBadgeText = "PHQ-9 Normal — C-SSRS Required";
                result.WarningTitle = "C-SSRS Safety Screening Required";
                result.WarningReason = "Your PHQ-9 assessment is normal. Please complete the C-SSRS safety screening to finalize your semester assessment.";
            }
            else if (hasCSSRS && !hasPHQ)
            {
                result.IsScreeningComplete = false;
                result.ComplianceStatus = "Pending";
                result.RequiresPHQBecauseCSSRSNormal = true;
                result.StatusBadgeText = "C-SSRS Normal — PHQ-9 Required";
                result.WarningTitle = "PHQ-9 Depression Screening Required";
                result.WarningReason = "Your C-SSRS assessment is normal/low-risk. Please complete the PHQ-9 depression screening to finalize your semester assessment.";
            }
            else
            {
                result.IsScreeningComplete = false;
                result.ComplianceStatus = "Pending";
                result.StatusBadgeText = "Both Screenings Pending";
                result.WarningTitle = "Semester Mental Health Screening Pending";
                result.WarningReason = "Please complete your mandatory PHQ-9 and C-SSRS screening modules.";
            }

            return result;
        }
    }
}
