using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using StudentMentalHealthMonitoringSystem.Models;

namespace StudentMentalHealthMonitoringSystem.Data
{
    public static class DummyDataSeeder
    {
        public static void SeedDummyData(ApplicationDbContext context)
        {
            string defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@1234");

            // 1. Ensure Psychologist exists and reset password to 123456
            var psychologist = context.Psychologists.FirstOrDefault(p => p.Email == "psychologist@smhms.com") 
                               ?? context.Psychologists.FirstOrDefault();

            if (psychologist == null)
            {
                psychologist = new Psychologist
                {
                    FullName = "Dr. Farhana Ahmed",
                    Email = "psychologist@smhms.com",
                    Password = defaultPasswordHash,
                    Phone = "01700000001",
                    Specialization = "Clinical Psychology & Counseling",
                    Qualification = "Ph.D. in Clinical Psychology",
                    Experience = 8
                };
                context.Psychologists.Add(psychologist);
            }
            else
            {
                // Force reset password to 123456 hash so login works seamlessly
                psychologist.Email = "psychologist@smhms.com";
                psychologist.Password = defaultPasswordHash;
            }

            // Also reset password for all existing psychologists if any
            foreach (var p in context.Psychologists.ToList())
            {
                p.Password = defaultPasswordHash;
            }
            context.SaveChanges();

            // Define the 10 Dummy Students:
            // 7 Completed (Finished follow-up), 3 Continuous (In Progress)
            // Dept distribution: CSE (5), EEE (2), BBA (2), Civil (1)
            var studentConfigs = new[]
            {
                new { Index = 1,  Name = "Student 1",  Dept = "CSE",   IdNum = "2026-CSE-001", IsFinal = true },
                new { Index = 2,  Name = "Student 2",  Dept = "CSE",   IdNum = "2026-CSE-002", IsFinal = true },
                new { Index = 3,  Name = "Student 3",  Dept = "CSE",   IdNum = "2026-CSE-003", IsFinal = true },
                new { Index = 4,  Name = "Student 4",  Dept = "CSE",   IdNum = "2026-CSE-004", IsFinal = true },
                new { Index = 5,  Name = "Student 5",  Dept = "CSE",   IdNum = "2026-CSE-005", IsFinal = false }, // Continuous
                new { Index = 6,  Name = "Student 6",  Dept = "EEE",   IdNum = "2026-EEE-001", IsFinal = true },
                new { Index = 7,  Name = "Student 7",  Dept = "EEE",   IdNum = "2026-EEE-002", IsFinal = true },
                new { Index = 8,  Name = "Student 8",  Dept = "BBA",   IdNum = "2026-BBA-001", IsFinal = true },
                new { Index = 9,  Name = "Student 9",  Dept = "BBA",   IdNum = "2026-BBA-002", IsFinal = false }, // Continuous
                new { Index = 10, Name = "Student 10", Dept = "Civil", IdNum = "2026-CIV-001", IsFinal = false }  // Continuous
            };

            foreach (var cfg in studentConfigs)
            {
                string email = $"student{cfg.Index}@gmail.com";
                var student = context.Students.FirstOrDefault(s => s.Email == email);

                string activeTrimester = Student.GetCurrentActiveSemester();
                if (student == null)
                {
                    student = new Student
                    {
                        FullName = cfg.Name,
                        Email = email,
                        Password = defaultPasswordHash,
                        Phone = $"017000000{cfg.Index:D2}",
                        Department = cfg.Dept,
                        Semester = activeTrimester,
                        AdmissionYear = 2024,
                        StudentIdNumber = cfg.IdNum,
                        Gender = cfg.Index % 2 == 0 ? "Female" : "Male",
                        DateOfBirth = new DateTime(2002, 1 + (cfg.Index % 11), 10 + cfg.Index)
                    };
                    context.Students.Add(student);
                    context.SaveChanges(); // get StudentId
                }
                else
                {
                    student.Password = defaultPasswordHash;
                    student.Department = cfg.Dept;
                    student.StudentIdNumber = cfg.IdNum;
                    if (student.AdmissionYear == null || student.AdmissionYear == 0)
                    {
                        student.AdmissionYear = 2024;
                    }
                    if (string.IsNullOrWhiteSpace(student.Semester) || 
                        student.Semester.StartsWith("Semester", StringComparison.OrdinalIgnoreCase) || 
                        student.Semester.Equals("Spring 2026", StringComparison.OrdinalIgnoreCase) ||
                        !student.Semester.Contains("20"))
                    {
                        student.Semester = activeTrimester;
                    }
                    context.SaveChanges();
                }

                // Check if observations already exist for this student
                bool hasObservations = context.CounselingObservations.Any(o => o.StudentId == student.StudentId);
                if (hasObservations)
                {
                    continue; // Skip seeding sessions for this student if already present
                }

                // 3. Seed Screening Assessment Data for Analytics
                if (!context.PHQAssessments.Any(p => p.StudentId == student.StudentId))
                {
                    var phq = new PHQAssessment
                    {
                        StudentId = student.StudentId,
                        AssessmentDate = DateTime.Now.AddDays(-60),
                        Semester = "Spring 2026",
                        Question1Score = cfg.IsFinal ? 2 : 1,
                        Question2Score = cfg.IsFinal ? 2 : 1,
                        Question3Score = cfg.IsFinal ? 2 : 1,
                        Question4Score = cfg.IsFinal ? 2 : 2,
                        Question5Score = cfg.IsFinal ? 2 : 1,
                        Question6Score = cfg.IsFinal ? 2 : 2,
                        Question7Score = cfg.IsFinal ? 2 : 2,
                        Question8Score = cfg.IsFinal ? 2 : 2,
                        Question9Score = cfg.IsFinal ? 2 : 2,
                        TotalScore = cfg.IsFinal ? 18 : 14,
                        SeverityLevel = cfg.IsFinal ? "Severe" : "Moderate"
                    };
                    context.PHQAssessments.Add(phq);
                }

                if (!context.CSSRSAssessments.Any(c => c.StudentId == student.StudentId))
                {
                    var cssrs = new CSSRSAssessment
                    {
                        StudentId = student.StudentId,
                        AssessmentDate = DateTime.Now.AddDays(-60),
                        Semester = "Spring 2026",
                        Question1Answer = true,
                        Question2Answer = true,
                        Question3Answer = cfg.IsFinal,
                        Question4Answer = false,
                        Question5Answer = false,
                        Question6Answer = false,
                        RiskLevel = cfg.IsFinal ? "Moderate Risk" : "Low Risk"
                    };
                    context.CSSRSAssessments.Add(cssrs);
                }
                context.SaveChanges();

                // 4. Seed Counseling Sessions & Observations
                int sessionCount = cfg.IsFinal ? 4 : 2;
                int rootCounselingId = 0;

                for (int s = 1; s <= sessionCount; s++)
                {
                    var cDate = DateTime.Now.AddDays(-60 + (s * 12));
                    var counseling = new Counseling
                    {
                        StudentId = student.StudentId,
                        PsychologistId = psychologist.PsychologistId,
                        CounselingDate = cDate,
                        AppointmentTime = new TimeSpan(10 + s, 0, 0),
                        AppointmentRoom = "Room 304, Mental Health Center",
                        Status = "Completed",
                        TriggerSeverity = s == 1 ? (cfg.IsFinal ? "Severe" : "Moderate") : null,
                        RiskLevel = s == 1 ? "Moderate Risk" : "No Current Risk"
                    };
                    context.Counselings.Add(counseling);
                    context.SaveChanges();

                    if (s == 1)
                    {
                        rootCounselingId = counseling.CounselingId;
                    }

                    // Progress stages for observation
                    string mentalHealthStatus;
                    string overallProgress;
                    string safetyRisk;
                    string funcStatus;
                    string reportedImp;

                    if (s == 1)
                    {
                        mentalHealthStatus = cfg.IsFinal ? "Severe" : "Moderate";
                        overallProgress = "No Significant Change";
                        safetyRisk = "Moderate Risk";
                        funcStatus = "Significantly Impaired";
                        reportedImp = "No Improvement Yet";
                    }
                    else if (s == 2)
                    {
                        mentalHealthStatus = "Moderate";
                        overallProgress = "Improving";
                        safetyRisk = "Low Risk";
                        funcStatus = "Moderately Impaired";
                        reportedImp = "Slight Improvement";
                    }
                    else if (s == 3)
                    {
                        mentalHealthStatus = "Moderate";
                        overallProgress = "Partially Improved";
                        safetyRisk = "No Current Risk";
                        funcStatus = "Slightly Impaired";
                        reportedImp = "Moderate Improvement";
                    }
                    else
                    {
                        mentalHealthStatus = "Normal";
                        overallProgress = "Improving";
                        safetyRisk = "No Current Risk";
                        funcStatus = "Normal Functioning";
                        reportedImp = "Significant Improvement";
                    }

                    var observation = new CounselingObservation
                    {
                        CounselingId = counseling.CounselingId,
                        RootCounselingId = rootCounselingId,
                        StudentId = student.StudentId,
                        PsychologistId = psychologist.PsychologistId,
                        OverallProgressStatus = overallProgress,
                        CurrentMentalHealthStatus = mentalHealthStatus,
                        CurrentSafetyRisk = safetyRisk,
                        AcademicFunctioning = funcStatus,
                        SleepCondition = funcStatus,
                        SocialInteraction = funcStatus,
                        DailyActivities = funcStatus,
                        EmotionalRegulation = funcStatus,
                        StudentReportedImprovement = reportedImp,
                        AssessmentBasis = "Clinical Interview | PHQ-9 Baseline | Behavioral Observation",
                        ClinicalObservation = $"Session {s} observation notes: Student demonstrated good rapport, active participation in cognitive reframing techniques, and reported steady mood stabilization.",
                        AssessmentSummary = $"Session {s} summary: Mental health status evaluated as {mentalHealthStatus} with overall progress recorded as {overallProgress}.",
                        RecommendedAction = s < sessionCount ? "Schedule Follow-up Counseling | Continue CBT Exercises" : "Discharge from active counseling | Self-care routine",
                        CreatedAt = cDate
                    };
                    context.CounselingObservations.Add(observation);
                    context.SaveChanges();
                }

                // 5. Seed Observation Report summary
                var obsReport = context.ObservationReports.FirstOrDefault(r => r.StudentId == student.StudentId);
                if (obsReport == null)
                {
                    obsReport = new ObservationReport
                    {
                        RootCounselingId = rootCounselingId,
                        StudentId = student.StudentId,
                        PsychologistId = psychologist.PsychologistId,
                        InitialStatus = cfg.IsFinal ? "Severe" : "Moderate",
                        CurrentStatus = cfg.IsFinal ? "Normal" : "Moderate",
                        OverallProgressStatus = cfg.IsFinal ? "Improving" : "Improving",
                        CurrentSafetyRisk = "No Current Risk",
                        LatestAssessmentBasis = "Clinical Interview | PHQ-9 Baseline | Behavioral Observation",
                        LatestRecommendedAction = cfg.IsFinal ? "Discharge from active counseling | Self-care routine" : "Schedule Follow-up Counseling | Continue CBT Exercises",
                        LatestConditionSummary = cfg.IsFinal ? "Student has achieved recovery and normal functioning." : "Student is actively participating in counseling with steady progress.",
                        IsFinal = cfg.IsFinal,
                        FinalizedAt = cfg.IsFinal ? DateTime.Now.AddDays(-12) : null,
                        CreatedAt = DateTime.Now.AddDays(-48),
                        UpdatedAt = DateTime.Now.AddDays(-60 + (sessionCount * 12))
                    };
                    context.ObservationReports.Add(obsReport);
                }
                else
                {
                    obsReport.RootCounselingId = rootCounselingId;
                    obsReport.InitialStatus = cfg.IsFinal ? "Severe" : "Moderate";
                    obsReport.CurrentStatus = cfg.IsFinal ? "Normal" : "Moderate";
                    obsReport.OverallProgressStatus = "Improving";
                    obsReport.CurrentSafetyRisk = "No Current Risk";
                    obsReport.LatestAssessmentBasis = "Clinical Interview | PHQ-9 Baseline | Behavioral Observation";
                    obsReport.LatestRecommendedAction = cfg.IsFinal ? "Discharge from active counseling | Self-care routine" : "Schedule Follow-up Counseling | Continue CBT Exercises";
                    obsReport.LatestConditionSummary = cfg.IsFinal ? "Student has achieved recovery and normal functioning." : "Student is actively participating in counseling with steady progress.";
                    obsReport.IsFinal = cfg.IsFinal;
                    obsReport.FinalizedAt = cfg.IsFinal ? DateTime.Now.AddDays(-12) : null;
                    obsReport.UpdatedAt = DateTime.Now.AddDays(-60 + (sessionCount * 12));
                }
                context.SaveChanges();
            }
        }
    }
}
