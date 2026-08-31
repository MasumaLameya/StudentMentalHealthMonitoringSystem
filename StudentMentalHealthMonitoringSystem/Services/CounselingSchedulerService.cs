using Microsoft.EntityFrameworkCore;
using StudentMentalHealthMonitoringSystem.Data;
using StudentMentalHealthMonitoringSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StudentMentalHealthMonitoringSystem.Services
{
    // =========================================================
    // SCHEDULER RESULT
    // =========================================================

    public class CounselingSchedulerResult
    {
        public bool Success { get; set; }

        public bool Created { get; set; }

        public string Message { get; set; } =
            string.Empty;

        public Counseling? Counseling { get; set; }
    }


    // =========================================================
    // COUNSELING SCHEDULER SERVICE
    // =========================================================

    public class CounselingSchedulerService
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public CounselingSchedulerService(
            ApplicationDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }


        // =====================================================
        // AUTO ASSIGN PSYCHOLOGIST
        // =====================================================
        //
        // This method can be called from:
        //
        // PHQ-9
        // C-SSRS
        // AI Chat
        // Feelings
        // Voice Bot
        //
        // when project severity becomes:
        //
        // Severe
        // Extremely Severe
        //
        // =====================================================

        public async Task<CounselingSchedulerResult>
            AutoAssignPsychologistAsync(
                int studentId,
                string severityLevel,
                string triggerSource)
        {
            // =================================================
            // Check Trigger Severity
            // =================================================

            if (severityLevel != "Severe" &&
                severityLevel != "Extremely Severe")
            {
                return new CounselingSchedulerResult
                {
                    Success = false,
                    Created = false,
                    Message =
                        "Automatic counseling assignment is not required for this severity level."
                };
            }


            // =================================================
            // Check Student
            // =================================================

            var student =
                await _context.Students
                    .FirstOrDefaultAsync(
                        s =>
                            s.StudentId ==
                            studentId
                    );


            if (student == null)
            {
                return new CounselingSchedulerResult
                {
                    Success = false,
                    Created = false,
                    Message =
                        "Student was not found."
                };
            }


            // =================================================
            // Existing Active Appointment Protection
            // =================================================
            //
            // If the student already has an active upcoming
            // counseling appointment (Follow-up, Auto-assignment,
            // Student-booked, or Department-assigned), do NOT
            // change the appointment or assign another psychologist.
            // Instead, update the Combined Screening Report for the
            // existing session so the assigned psychologist has full context.
            // =================================================

            var existingActiveAppointment =
                await _context.Counselings
                    .Include(c =>
                        c.Psychologist)
                    .Where(c =>
                        c.StudentId ==
                            studentId &&

                        c.Status !=
                            "Cancelled" &&

                        c.Status !=
                            "Completed" &&

                        c.CounselingDate.Date >=
                            DateTime.Today)
                    .OrderBy(c =>
                        c.CounselingDate)
                    .ThenBy(c =>
                        c.AppointmentTime)
                    .FirstOrDefaultAsync();


            if (existingActiveAppointment != null)
            {
                // =================================================
                // Update Existing Combined Screening Report
                // =================================================

                await CreateOrUpdateCombinedScreeningReportAsync(
                    existingActiveAppointment,
                    triggerSource,
                    severityLevel
                );


                return new CounselingSchedulerResult
                {
                    Success = true,
                    Created = false,
                    Message =
                        "The student already has an active counseling appointment scheduled. Existing appointment and psychologist retained.",
                    Counseling =
                        existingActiveAppointment
                };
            }


            // =================================================
            // Get Psychologists
            // =================================================

            var psychologists =
                await _context.Psychologists
                    .OrderBy(p =>
                        p.FullName)
                    .ToListAsync();


            if (!psychologists.Any())
            {
                return new CounselingSchedulerResult
                {
                    Success = false,
                    Created = false,
                    Message =
                        "No psychologist is available in the system."
                };
            }


            // =================================================
            // Fixed Counseling Slots
            // =================================================
            //
            // 09:00 AM - 10:00 AM
            // 10:00 AM - 11:00 AM
            // 11:00 AM - 12:00 PM
            //
            // =================================================

            var availableStartTimes =
                new List<TimeSpan>
                {
                    new TimeSpan(9, 0, 0),
                    new TimeSpan(10, 0, 0),
                    new TimeSpan(11, 0, 0)
                };


            // =================================================
            // Search Next 30 Days
            // =================================================

            for (int dayOffset = 0;
                 dayOffset <= 30;
                 dayOffset++)
            {
                var appointmentDate =
                    DateTime.Today
                        .AddDays(
                            dayOffset
                        );


                // =================================================
                // University Working Days
                // Monday - Friday
                // =================================================

                if (appointmentDate.DayOfWeek ==
                        DayOfWeek.Saturday ||
                    appointmentDate.DayOfWeek ==
                        DayOfWeek.Sunday)
                {
                    continue;
                }


                // =================================================
                // Check Every Fixed Slot
                // =================================================

                foreach (var startTime
                    in availableStartTimes)
                {
                    var endTime =
                        startTime.Add(
                            TimeSpan.FromHours(1)
                        );


                    // =================================================
                    // Do Not Schedule In The Past
                    // =================================================

                    var appointmentDateTime =
                        appointmentDate.Date
                            .Add(
                                startTime
                            );


                    if (appointmentDateTime <=
                        DateTime.Now)
                    {
                        continue;
                    }


                    // =================================================
                    // Student Existing Appointment Conflict
                    // =================================================
                    //
                    // StudentAvailability is NOT checked.
                    //
                    // Only real counseling appointments are
                    // checked to prevent double booking.
                    // =================================================

                    var studentAlreadyBooked =
                        await _context.Counselings
                            .AnyAsync(
                                c =>
                                    c.StudentId ==
                                        studentId &&

                                    c.CounselingDate.Date ==
                                        appointmentDate.Date &&

                                    c.Status !=
                                        "Cancelled" &&

                                    startTime <
                                        c.AppointmentEndTime &&

                                    endTime >
                                        c.AppointmentTime
                            );


                    if (studentAlreadyBooked)
                    {
                        continue;
                    }


                    // =================================================
                    // Find Free Psychologists
                    // =================================================

                    var freePsychologists =
                        new List<Psychologist>();


                    foreach (var psychologist
                        in psychologists)
                    {
                        var psychologistBooked =
                            await _context.Counselings
                                .AnyAsync(
                                    c =>
                                        c.PsychologistId ==
                                            psychologist
                                                .PsychologistId &&

                                        c.CounselingDate.Date ==
                                            appointmentDate.Date &&

                                        c.Status !=
                                            "Cancelled" &&

                                        startTime <
                                            c.AppointmentEndTime &&

                                        endTime >
                                            c.AppointmentTime
                                );


                        if (!psychologistBooked)
                        {
                            freePsychologists.Add(
                                psychologist
                            );
                        }
                    }


                    if (!freePsychologists.Any())
                    {
                        continue;
                    }


                    // =================================================
                    // Select Lowest Workload Psychologist
                    // =================================================

                    Psychologist? selectedPsychologist =
                        null;


                    int lowestAppointmentCount =
                        int.MaxValue;


                    foreach (var psychologist
                        in freePsychologists)
                    {
                        var appointmentCount =
                            await _context.Counselings
                                .CountAsync(
                                    c =>
                                        c.PsychologistId ==
                                            psychologist
                                                .PsychologistId &&

                                        c.Status !=
                                            "Cancelled"
                                );


                        if (appointmentCount <
                            lowestAppointmentCount)
                        {
                            lowestAppointmentCount =
                                appointmentCount;


                            selectedPsychologist =
                                psychologist;
                        }
                        else if (
                            appointmentCount ==
                            lowestAppointmentCount)
                        {
                            if (selectedPsychologist == null ||
                                string.Compare(
                                    psychologist.FullName,
                                    selectedPsychologist
                                        .FullName,
                                    StringComparison
                                        .OrdinalIgnoreCase
                                ) < 0)
                            {
                                selectedPsychologist =
                                    psychologist;
                            }
                        }
                    }


                    if (selectedPsychologist == null)
                    {
                        continue;
                    }


                    // =================================================
                    // Create Automatic Appointment
                    // =================================================

                    var counseling =
                        new Counseling
                        {
                            StudentId =
                                studentId,

                            PsychologistId =
                                selectedPsychologist
                                    .PsychologistId,

                            CounselingDate =
                                appointmentDate.Date,

                            AppointmentTime =
                                startTime,

                            AppointmentEndTime =
                                endTime,

                            Observation =
                                string.Empty,

                            Assessment =
                                string.Empty,

                            Recommendation =
                                string.Empty,

                            RiskLevel =
                                severityLevel,

                            Status =
                                "Confirmed",

                            AppointmentSource =
                                "AutoAssignment",

                            TriggerSource =
                                triggerSource,

                            TriggerSeverity =
                                severityLevel,

                            AppointmentRoom =
                                "Mental Health & Counseling Center, Room 402",

                            ParentCounselingId =
                                null,

                            CreatedAt =
                                DateTime.Now
                        };


                    _context.Counselings.Add(
                        counseling
                    );


                    await _context
                        .SaveChangesAsync();


                    // =================================================
                    // Create Combined Screening Report
                    // =================================================

                    await CreateOrUpdateCombinedScreeningReportAsync(
                        counseling,
                        triggerSource,
                        severityLevel
                    );


                    // =================================================
                    // Send Appointment Confirmation Email To Student
                    // =================================================

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(student.Email))
                        {
                            await _emailService.SendAppointmentConfirmationEmailAsync(
                                recipientEmail: student.Email,
                                studentName: student.FullName,
                                studentIdNumber: student.StudentIdNumber,
                                psychologistName: selectedPsychologist.FullName,
                                psychologistSpecialization: selectedPsychologist.Specialization,
                                appointmentDate: counseling.CounselingDate,
                                startTime: counseling.AppointmentTime,
                                endTime: counseling.AppointmentEndTime,
                                appointmentRoom: counseling.AppointmentRoom,
                                appointmentSource: "AutoAssignment",
                                severityOrReason: $"{triggerSource} Assessment ({severityLevel})"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CounselingSchedulerService] Failed to send auto appointment email: {ex.Message}");
                    }


                    return new CounselingSchedulerResult
                    {
                        Success = true,
                        Created = true,
                        Message =
                            "Psychologist automatically assigned successfully.",
                        Counseling =
                            counseling
                    };
                }
            }


            // =================================================
            // No Slot Found
            // =================================================

            return new CounselingSchedulerResult
            {
                Success = false,
                Created = false,
                Message =
                    "No available psychologist appointment slot was found within the next 30 days."
            };
        }


        // =====================================================
        // CREATE OR UPDATE COMBINED SCREENING REPORT
        // =====================================================

        private async Task
            CreateOrUpdateCombinedScreeningReportAsync(
                Counseling counseling,
                string triggerSource,
                string triggerSeverity)
        {
            // =================================================
            // Latest PHQ-9
            // =================================================

            var latestPHQ =
                await _context.PHQAssessments
                    .Where(p =>
                        p.StudentId ==
                            counseling.StudentId
                    )
                    .OrderByDescending(p =>
                        p.AssessmentDate
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // Latest C-SSRS
            // =================================================

            var latestCSSRS =
                await _context.CSSRSAssessments
                    .Where(c =>
                        c.StudentId ==
                            counseling.StudentId
                    )
                    .OrderByDescending(c =>
                        c.AssessmentDate
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // Latest Feelings Record
            // =================================================

            var latestFeeling =
                await _context.StudentSemesterRecords
                    .Where(r =>
                        r.StudentId ==
                            counseling.StudentId
                    )
                    .OrderByDescending(r =>
                        r.UpdatedAt ??
                        r.SubmittedAt
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // Latest AI Chat Assessment
            // =================================================

            var latestChatAssessment =
                await _context.ChatRiskAssessments
                    .Where(r =>
                        r.StudentId ==
                            counseling.StudentId
                    )
                    .OrderByDescending(r =>
                        r.CreatedAt
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // Latest Voice Bot Report
            // =================================================

            var latestVoiceBotReport =
                await _context.VoiceBotReports
                    .Where(r =>
                        r.StudentId ==
                            counseling.StudentId
                    )
                    .OrderByDescending(r =>
                        r.LastUpdatedAt
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // Build One Combined Screening Report
            // =================================================

            var reportLines =
                new List<string>();


            reportLines.Add(
                "SCREENING REPORT"
            );

            reportLines.Add(
                ""
            );


            reportLines.Add(
                $"Trigger Source: {triggerSource}"
            );

            reportLines.Add(
                $"Trigger Severity: {triggerSeverity}"
            );

            reportLines.Add(
                ""
            );


            // =================================================
            // PHQ-9 SECTION
            // =================================================

            reportLines.Add(
                "PHQ-9 QUESTIONNAIRE"
            );


            if (latestPHQ == null)
            {
                reportLines.Add(
                    "No PHQ-9 assessment available."
                );
            }
            else
            {
                reportLines.Add(
                    $"Semester: {latestPHQ.Semester}"
                );

                reportLines.Add(
                    $"Total Score: {latestPHQ.TotalScore}"
                );

                reportLines.Add(
                    $"Result: {latestPHQ.SeverityLevel}"
                );

                reportLines.Add(
                    "Answers:"
                );

                reportLines.Add(
                    $"Question 1: {latestPHQ.Question1Score}"
                );

                reportLines.Add(
                    $"Question 2: {latestPHQ.Question2Score}"
                );

                reportLines.Add(
                    $"Question 3: {latestPHQ.Question3Score}"
                );

                reportLines.Add(
                    $"Question 4: {latestPHQ.Question4Score}"
                );

                reportLines.Add(
                    $"Question 5: {latestPHQ.Question5Score}"
                );

                reportLines.Add(
                    $"Question 6: {latestPHQ.Question6Score}"
                );

                reportLines.Add(
                    $"Question 7: {latestPHQ.Question7Score}"
                );

                reportLines.Add(
                    $"Question 8: {latestPHQ.Question8Score}"
                );

                reportLines.Add(
                    $"Question 9: {latestPHQ.Question9Score}"
                );


                if (!string.IsNullOrWhiteSpace(
                    latestPHQ.FunctionalDifficulty))
                {
                    reportLines.Add(
                        $"Functional Difficulty: {latestPHQ.FunctionalDifficulty}"
                    );
                }


                if (!string.IsNullOrWhiteSpace(
                    latestPHQ.AdditionalComments))
                {
                    reportLines.Add(
                        $"Additional Comments: {latestPHQ.AdditionalComments}"
                    );
                }
            }


            reportLines.Add(
                ""
            );


            // =================================================
            // C-SSRS SECTION
            // =================================================

            reportLines.Add(
                "C-SSRS QUESTIONNAIRE"
            );


            if (latestCSSRS == null)
            {
                reportLines.Add(
                    "No C-SSRS assessment available."
                );
            }
            else
            {
                reportLines.Add(
                    $"Semester: {latestCSSRS.Semester}"
                );

                reportLines.Add(
                    $"Result: {latestCSSRS.RiskLevel}"
                );

                reportLines.Add(
                    "Answers:"
                );

                reportLines.Add(
                    $"Question 1: {(latestCSSRS.Question1Answer == true ? "Yes" : "No")}"
                );

                reportLines.Add(
                    $"Question 2: {(latestCSSRS.Question2Answer == true ? "Yes" : "No")}"
                );

                reportLines.Add(
                    $"Question 3: {(latestCSSRS.Question3Answer == true ? "Yes" : "No")}"
                );

                reportLines.Add(
                    $"Question 4: {(latestCSSRS.Question4Answer == true ? "Yes" : "No")}"
                );

                reportLines.Add(
                    $"Question 5: {(latestCSSRS.Question5Answer == true ? "Yes" : "No")}"
                );

                reportLines.Add(
                    $"Question 6: {(latestCSSRS.Question6Answer == true ? "Yes" : "No")}"
                );


                if (latestCSSRS.RecentBehavior.HasValue)
                {
                    reportLines.Add(
                        $"Recent Behaviour Information: {(latestCSSRS.RecentBehavior.Value ? "Yes" : "No")}"
                    );
                }


                if (!string.IsNullOrWhiteSpace(
                    latestCSSRS.AdditionalInformation))
                {
                    reportLines.Add(
                        $"Additional Information: {latestCSSRS.AdditionalInformation}"
                    );
                }
            }


            reportLines.Add(
                ""
            );


            // =================================================
            // FEELINGS SECTION
            // =================================================

            reportLines.Add(
                "FEELINGS ANALYSIS"
            );


            if (latestFeeling == null)
            {
                reportLines.Add(
                    "No feelings analysis available."
                );
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(
                    latestFeeling.FeelingRiskLevel))
                {
                    reportLines.Add(
                        $"Risk Level: {latestFeeling.FeelingRiskLevel}"
                    );
                }
                else
                {
                    reportLines.Add(
                        "Risk Level: Not Available"
                    );
                }


                if (!string.IsNullOrWhiteSpace(
                    latestFeeling.FeelingSummary))
                {
                    reportLines.Add(
                        $"Summary: {latestFeeling.FeelingSummary}"
                    );
                }
                else
                {
                    reportLines.Add(
                        "Summary: Not Available"
                    );
                }
            }


            reportLines.Add(
                ""
            );


            // =================================================
            // AI CHAT SECTION
            // =================================================

            reportLines.Add(
                "AI CHAT ASSESSMENT"
            );


            if (latestChatAssessment == null)
            {
                reportLines.Add(
                    "No AI chat assessment available."
                );
            }
            else
            {
                reportLines.Add(
                    $"Risk Level: {latestChatAssessment.RiskStatus}"
                );


                if (!string.IsNullOrWhiteSpace(
                    latestChatAssessment.Summary))
                {
                    reportLines.Add(
                        $"Summary: {latestChatAssessment.Summary}"
                    );
                }
                else
                {
                    reportLines.Add(
                        "Summary: Not Available"
                    );
                }
            }


            reportLines.Add(
                ""
            );


            // =================================================
            // VOICE BOT SECTION
            // =================================================

            reportLines.Add(
                "VOICE BOT ASSESSMENT"
            );


            if (latestVoiceBotReport == null)
            {
                reportLines.Add(
                    "No Voice Bot assessment available."
                );
            }
            else
            {
                // =============================================
                // Use Final Status if the Voice Bot session
                // is completed.
                //
                // Otherwise use the latest Current Status.
                // =============================================

                var voiceStatus =
                    latestVoiceBotReport.IsFinal &&
                    !string.IsNullOrWhiteSpace(
                        latestVoiceBotReport.FinalStatus
                    )
                        ? latestVoiceBotReport.FinalStatus
                        : latestVoiceBotReport.CurrentStatus;


                var voiceSummary =
                    latestVoiceBotReport.IsFinal &&
                    !string.IsNullOrWhiteSpace(
                        latestVoiceBotReport.FinalSummary
                    )
                        ? latestVoiceBotReport.FinalSummary
                        : latestVoiceBotReport.CurrentSummary;


                reportLines.Add(
                    $"Risk Level: {voiceStatus}"
                );


                if (!string.IsNullOrWhiteSpace(
                    voiceSummary))
                {
                    reportLines.Add(
                        $"Summary: {voiceSummary}"
                    );
                }
                else
                {
                    reportLines.Add(
                        "Summary: Not Available"
                    );
                }


                reportLines.Add(
                    $"Last Updated: {latestVoiceBotReport.LastUpdatedAt:dd MMM yyyy, h:mm tt}"
                );


                reportLines.Add(
                    latestVoiceBotReport.IsFinal
                        ? "Report Status: Final"
                        : "Report Status: Live / Ongoing"
                );
            }


            var reportContent =
                string.Join(
                    Environment.NewLine,
                    reportLines
                );


            // =================================================
            // One Report Per Counseling Appointment
            // =================================================

            var existingReport =
                await _context.ScreeningReports
                    .FirstOrDefaultAsync(
                        r =>
                            r.CounselingId ==
                                counseling.CounselingId
                    );


            // =================================================
            // Update Existing Combined Report
            // =================================================

            if (existingReport != null)
            {
                existingReport.TriggerSource =
                    triggerSource;

                existingReport.TriggerSeverity =
                    triggerSeverity;

                existingReport.ReportContent =
                    reportContent;


                await _context
                    .SaveChangesAsync();


                return;
            }


            // =================================================
            // Create New Combined Report
            // =================================================

            var screeningReport =
                new ScreeningReport
                {
                    StudentId =
                        counseling.StudentId,

                    PsychologistId =
                        counseling.PsychologistId,

                    CounselingId =
                        counseling.CounselingId,

                    TriggerSource =
                        triggerSource,

                    TriggerSeverity =
                        triggerSeverity,

                    ReportContent =
                        reportContent,

                    CreatedAt =
                        DateTime.Now
                };


            _context.ScreeningReports.Add(
                screeningReport
            );


            await _context
                .SaveChangesAsync();
        }


        // =====================================================
        // CREATE FOLLOW-UP APPOINTMENT
        // =====================================================

        public async Task<CounselingSchedulerResult>
            CreateFollowUpAppointmentAsync(
                Counseling currentCounseling,
                DateTime followUpDate,
                TimeSpan followUpTime)
        {
            // =================================================
            // Validate Date
            // =================================================

            followUpDate =
                followUpDate.Date;


            if (followUpDate <
                DateTime.Today)
            {
                return new CounselingSchedulerResult
                {
                    Success = false,
                    Created = false,
                    Message =
                        "Follow-up date cannot be in the past."
                };
            }


            // =================================================
            // Working Day Check
            // Monday - Friday
            // =================================================

            if (followUpDate.DayOfWeek ==
                    DayOfWeek.Saturday ||
                followUpDate.DayOfWeek ==
                    DayOfWeek.Sunday)
            {
                return new CounselingSchedulerResult
                {
                    Success = false,
                    Created = false,
                    Message =
                        "Follow-up appointments can only be scheduled from Monday to Friday."
                };
            }


            // =================================================
            // Fixed Slot Check
            // =================================================

            var validStartTimes =
                new List<TimeSpan>
                {
                    new TimeSpan(9, 0, 0),
                    new TimeSpan(10, 0, 0),
                    new TimeSpan(11, 0, 0)
                };


            if (!validStartTimes.Contains(
                followUpTime))
            {
                return new CounselingSchedulerResult
                {
                    Success = false,
                    Created = false,
                    Message =
                        "Please select a valid follow-up time: 9:00 AM, 10:00 AM, or 11:00 AM."
                };
            }


            // =================================================
            // Follow-up End Time
            // =================================================

            var followUpEndTime =
                followUpTime.Add(
                    TimeSpan.FromHours(1)
                );


            // =================================================
            // Future Date / Time Check
            // =================================================

            var followUpDateTime =
                followUpDate
                    .Add(
                        followUpTime
                    );


            if (followUpDateTime <=
                DateTime.Now)
            {
                return new CounselingSchedulerResult
                {
                    Success = false,
                    Created = false,
                    Message =
                        "Please select a future follow-up date and time."
                };
            }


            // =================================================
            // Prevent Duplicate Follow-Up
            // =================================================

            var existingFollowUp =
                await _context.Counselings
                    .FirstOrDefaultAsync(
                        c =>
                            c.ParentCounselingId ==
                                currentCounseling
                                    .CounselingId &&

                            c.Status !=
                                "Cancelled"
                    );


            if (existingFollowUp != null)
            {
                await _context
                    .SaveChangesAsync();


                return new CounselingSchedulerResult
                {
                    Success = true,
                    Created = false,
                    Message =
                        "A follow-up appointment already exists for this counseling session.",
                    Counseling =
                        existingFollowUp
                };
            }


            // =================================================
            // Check Psychologist Exists
            // =================================================

            var psychologistExists =
                await _context.Psychologists
                    .AnyAsync(
                        p =>
                            p.PsychologistId ==
                                currentCounseling
                                    .PsychologistId
                    );


            if (!psychologistExists)
            {
                return new CounselingSchedulerResult
                {
                    Success = false,
                    Created = false,
                    Message =
                        "Psychologist was not found."
                };
            }


            // =================================================
            // Check Student Appointment Conflict
            // =================================================
            //
            // StudentAvailability is NOT checked.
            //
            // Only existing counseling appointments are
            // checked.
            // =================================================

            var studentConflict =
                await _context.Counselings
                    .AnyAsync(
                        c =>
                            c.CounselingId !=
                                currentCounseling
                                    .CounselingId &&

                            c.StudentId ==
                                currentCounseling
                                    .StudentId &&

                            c.CounselingDate.Date ==
                                followUpDate &&

                            c.Status !=
                                "Cancelled" &&

                            followUpTime <
                                c.AppointmentEndTime &&

                            followUpEndTime >
                                c.AppointmentTime
                    );


            if (studentConflict)
            {
                return new CounselingSchedulerResult
                {
                    Success = false,
                    Created = false,
                    Message =
                        "The student already has another counseling appointment during the selected follow-up time."
                };
            }


            // =================================================
            // Check Psychologist Appointment Conflict
            // =================================================

            var psychologistConflict =
                await _context.Counselings
                    .AnyAsync(
                        c =>
                            c.CounselingId !=
                                currentCounseling
                                    .CounselingId &&

                            c.PsychologistId ==
                                currentCounseling
                                    .PsychologistId &&

                            c.CounselingDate.Date ==
                                followUpDate &&

                            c.Status !=
                                "Cancelled" &&

                            followUpTime <
                                c.AppointmentEndTime &&

                            followUpEndTime >
                                c.AppointmentTime
                    );


            if (psychologistConflict)
            {
                return new CounselingSchedulerResult
                {
                    Success = false,
                    Created = false,
                    Message =
                        "The psychologist already has another counseling appointment during the selected follow-up time."
                };
            }


            // =================================================
            // Save Follow-up Information In Current Session
            // =================================================

            currentCounseling.NextFollowUpDate =
                followUpDate;


            currentCounseling.NextFollowUpTime =
                followUpTime;


            // =================================================
            // Create New Follow-Up Counseling Record
            // =================================================

            var followUpCounseling =
                new Counseling
                {
                    StudentId =
                        currentCounseling.StudentId,

                    PsychologistId =
                        currentCounseling
                            .PsychologistId,

                    CounselingDate =
                        followUpDate,

                    AppointmentTime =
                        followUpTime,

                    AppointmentEndTime =
                        followUpEndTime,

                    Observation =
                        string.Empty,

                    Assessment =
                        string.Empty,

                    Recommendation =
                        string.Empty,

                    RiskLevel =
                        currentCounseling.RiskLevel,

                    Status =
                        "Confirmed",

                    ParentCounselingId =
                        currentCounseling
                            .CounselingId,

                    AppointmentSource =
                        "FollowUp",

                    TriggerSource =
                        "Psychologist Follow-up",

                    TriggerSeverity =
                        currentCounseling
                            .RiskLevel,

                    AppointmentRoom =
                        currentCounseling.AppointmentRoom ??
                        "Mental Health & Counseling Center, Room 402",

                    CreatedAt =
                        DateTime.Now
                };


            _context.Counselings.Add(
                followUpCounseling
            );


            // =================================================
            // Save Current Completed Counseling
            // AND New Follow-Up Appointment Together
            // =================================================

            await _context
                .SaveChangesAsync();


            // =================================================
            // Send Follow-Up Appointment Email To Student
            // =================================================

            try
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentId == currentCounseling.StudentId);
                var psychologist = await _context.Psychologists
                    .FirstOrDefaultAsync(p => p.PsychologistId == currentCounseling.PsychologistId);

                if (student != null && !string.IsNullOrWhiteSpace(student.Email) && psychologist != null)
                {
                    await _emailService.SendAppointmentConfirmationEmailAsync(
                        recipientEmail: student.Email,
                        studentName: student.FullName,
                        studentIdNumber: student.StudentIdNumber,
                        psychologistName: psychologist.FullName,
                        psychologistSpecialization: psychologist.Specialization,
                        appointmentDate: followUpCounseling.CounselingDate,
                        startTime: followUpCounseling.AppointmentTime,
                        endTime: followUpCounseling.AppointmentEndTime,
                        appointmentRoom: followUpCounseling.AppointmentRoom,
                        appointmentSource: "FollowUp",
                        severityOrReason: "Follow-up Counseling Session"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CounselingSchedulerService] Failed to send follow-up appointment email: {ex.Message}");
            }


            return new CounselingSchedulerResult
            {
                Success = true,
                Created = true,
                Message =
                    "Follow-up appointment created successfully.",
                Counseling =
                    followUpCounseling
            };
        }
    }
}