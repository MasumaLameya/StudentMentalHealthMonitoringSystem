using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentMentalHealthMonitoringSystem.Data;
using StudentMentalHealthMonitoringSystem.Models;
using StudentMentalHealthMonitoringSystem.Services;

namespace StudentMentalHealthMonitoringSystem.Controllers
{
    public class VoiceBotController : Controller
    {
        // =========================================================
        // DATABASE
        // =========================================================

        private readonly ApplicationDbContext _context;


        // =========================================================
        // CONFIGURATION
        // =========================================================

        private readonly IConfiguration _configuration;


        // =========================================================
        // GEMINI LIVE VOICE SERVICE
        // =========================================================

        private readonly GeminiLiveVoiceService
            _geminiLiveVoiceService;


        // =========================================================
        // COUNSELING SCHEDULER
        // =========================================================

        private readonly CounselingSchedulerService
            _counselingSchedulerService;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public VoiceBotController(
            ApplicationDbContext context,
            IConfiguration configuration,
            GeminiLiveVoiceService geminiLiveVoiceService,
            CounselingSchedulerService counselingSchedulerService)
        {
            _context =
                context;

            _configuration =
                configuration;

            _geminiLiveVoiceService =
                geminiLiveVoiceService;

            _counselingSchedulerService =
                counselingSchedulerService;
        }


        // =========================================================
        // VOICE BOT HOME
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // ================= Check Student Session =================

            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );


            if (studentId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Student"
                );
            }


            // ================= Get Student =================

            var student =
                await _context.Students
                    .FirstOrDefaultAsync(
                        s =>
                            s.StudentId ==
                                studentId.Value
                    );


            if (student == null)
            {
                return RedirectToAction(
                    "Login",
                    "Student"
                );
            }


            // =====================================================
            // GET ACTIVE VOICE SESSION
            // =====================================================

            var activeSession =
                await _context.VoiceBotSessions

                    .Where(s =>
                        s.StudentId ==
                            studentId.Value &&

                        s.IsActive
                    )

                    .OrderByDescending(s =>
                        s.StartedAt
                    )

                    .FirstOrDefaultAsync();


            // =====================================================
            // GET CURRENT REPORT
            // =====================================================

            VoiceBotReport? currentReport =
                null;


            if (activeSession != null)
            {
                currentReport =
                    await _context.VoiceBotReports

                        .FirstOrDefaultAsync(r =>
                            r.VoiceBotSessionId ==
                                activeSession
                                    .VoiceBotSessionId &&

                            r.StudentId ==
                                studentId.Value
                        );
            }


            // ================= Data For View =================

            ViewBag.StudentName =
                student.FullName;


            ViewBag.ActiveSessionId =
                activeSession?.VoiceBotSessionId;


            ViewBag.CurrentStatus =
                currentReport?.CurrentStatus
                ?? activeSession?.CurrentStatus
                ?? "Normal";


            ViewBag.CurrentSummary =
                currentReport?.CurrentSummary
                ?? activeSession?.CurrentSummary
                ?? string.Empty;


            return View();
        }


        // =========================================================
        // START VOICE SESSION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartSession()
        {
            // ================= Check Student Session =================

            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );


            if (studentId == null)
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Your student session has expired."
                    }
                );
            }


            // ================= Check Student =================

            var studentExists =
                await _context.Students
                    .AnyAsync(
                        s =>
                            s.StudentId ==
                                studentId.Value
                    );


            if (!studentExists)
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Student account was not found."
                    }
                );
            }


            // =====================================================
            // GET LIVE VOICE MODEL
            // =====================================================

            var liveModelName =
                _configuration[
                    "Gemini:LiveVoiceModel"
                ];


            if (string.IsNullOrWhiteSpace(
                liveModelName))
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Gemini Live Voice model is not configured."
                    }
                );
            }


            liveModelName =
                liveModelName.Trim();


            // =====================================================
            // CHECK EXISTING ACTIVE SESSION
            // =====================================================

            var voiceSession =
                await _context.VoiceBotSessions

                    .Where(s =>
                        s.StudentId ==
                            studentId.Value &&

                        s.IsActive
                    )

                    .OrderByDescending(s =>
                        s.StartedAt
                    )

                    .FirstOrDefaultAsync();


            // =====================================================
            // CREATE NEW SESSION
            // =====================================================

            if (voiceSession == null)
            {
                voiceSession =
                    new VoiceBotSession
                    {
                        StudentId =
                            studentId.Value,

                        ModelName =
                            liveModelName,

                        CurrentStatus =
                            "Normal",

                        CurrentSummary =
                            string.Empty,

                        StartedAt =
                            DateTime.Now,

                        LastStatusUpdatedAt =
                            DateTime.Now,

                        IsActive =
                            true
                    };


                _context.VoiceBotSessions.Add(
                    voiceSession
                );


                await _context.SaveChangesAsync();
            }
            else
            {
                // =================================================
                // Keep active session model synchronized with
                // current appsettings.json configuration.
                // =================================================

                if (voiceSession.ModelName !=
                    liveModelName)
                {
                    voiceSession.ModelName =
                        liveModelName;


                    await _context.SaveChangesAsync();
                }
            }


            // =====================================================
            // CREATE / GET REPORT
            // One report per live session
            // =====================================================

            var report =
                await _context.VoiceBotReports

                    .FirstOrDefaultAsync(r =>
                        r.VoiceBotSessionId ==
                            voiceSession
                                .VoiceBotSessionId
                    );


            if (report == null)
            {
                report =
                    new VoiceBotReport
                    {
                        VoiceBotSessionId =
                            voiceSession
                                .VoiceBotSessionId,

                        StudentId =
                            studentId.Value,

                        CurrentStatus =
                            voiceSession
                                .CurrentStatus,

                        CurrentSummary =
                            voiceSession
                                .CurrentSummary,

                        LastUpdatedAt =
                            DateTime.Now,

                        FinalStatus =
                            null,

                        FinalSummary =
                            null,

                        IsFinal =
                            false,

                        FinalizedAt =
                            null
                    };


                _context.VoiceBotReports.Add(
                    report
                );


                await _context.SaveChangesAsync();
            }


            // ================= Success =================

            return Json(
                new
                {
                    success = true,

                    sessionId =
                        voiceSession
                            .VoiceBotSessionId,

                    status =
                        report.CurrentStatus,

                    summary =
                        report.CurrentSummary
                        ?? string.Empty
                }
            );
        }


        // =========================================================
        // GET SHORT-LIVED GEMINI LIVE TOKEN
        // =========================================================
        //
        // Flow:
        //
        // Browser
        //   ↓ authenticated request
        //
        // ASP.NET Backend
        //   ↓ permanent Gemini API key
        //
        // Gemini Auth Token API
        //   ↓ short-lived token
        //
        // Browser
        //   ↓ temporary token
        //
        // Gemini Live API
        //
        // Permanent API key never goes to the browser.
        //
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetLiveToken(
            int sessionId)
        {
            // ================= Student Session =================

            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );


            if (studentId == null)
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Your student session has expired."
                    }
                );
            }


            // =====================================================
            // VERIFY ACTIVE SESSION OWNERSHIP
            // =====================================================

            var voiceSession =
                await _context.VoiceBotSessions

                    .FirstOrDefaultAsync(
                        s =>
                            s.VoiceBotSessionId ==
                                sessionId &&

                            s.StudentId ==
                                studentId.Value &&

                            s.IsActive,

                        HttpContext.RequestAborted
                    );


            if (voiceSession == null)
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Active voice session was not found."
                    }
                );
            }


            // =====================================================
            // VALIDATE MODEL
            // =====================================================

            if (string.IsNullOrWhiteSpace(
                voiceSession.ModelName))
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Gemini Live Voice model is not configured."
                    }
                );
            }


            try
            {
                // =================================================
                // Create single-use short-lived token.
                // Implemented inside GeminiLiveVoiceService.
                // =================================================

                var token =
                    await _geminiLiveVoiceService
                        .CreateEphemeralTokenAsync(
                            voiceSession.ModelName,
                            HttpContext.RequestAborted
                        );


                if (string.IsNullOrWhiteSpace(
                    token))
                {
                    return Json(
                        new
                        {
                            success = false,

                            message =
                                "Gemini Live token was not returned."
                        }
                    );
                }


                // ================= Success =================

                return Json(
                    new
                    {
                        success = true,

                        token =
                            token,

                        model =
                            voiceSession.ModelName
                    }
                );
            }
            catch (Exception ex)
            {
                // =================================================
                // Server-side diagnostic
                // =================================================

                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "GEMINI LIVE TOKEN ERROR"
                );

                Console.WriteLine(
                    ex.ToString()
                );

                Console.WriteLine(
                    "========================================"
                );


                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Unable to prepare the Gemini Live connection."
                    }
                );
            }
        }


        // =========================================================
        // SAVE COMPLETED LIVE TRANSCRIPT
        // =========================================================
        //
        // Browser receives Gemini input/output transcription.
        //
        // After a completed turn:
        //
        // Student transcript
        // VoiceBot transcript
        //
        // are saved here.
        //
        // Raw microphone audio is NOT stored.
        //
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTranscript(
            int sessionId,
            string speaker,
            string text)
        {
            // ================= Student Session =================

            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );


            if (studentId == null)
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Your student session has expired."
                    }
                );
            }


            // =====================================================
            // VERIFY ACTIVE SESSION OWNERSHIP
            // =====================================================

            var voiceSession =
                await _context.VoiceBotSessions

                    .FirstOrDefaultAsync(
                        s =>
                            s.VoiceBotSessionId ==
                                sessionId &&

                            s.StudentId ==
                                studentId.Value &&

                            s.IsActive,

                        HttpContext.RequestAborted
                    );


            if (voiceSession == null)
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Active voice session was not found."
                    }
                );
            }


            // =====================================================
            // VALIDATE SPEAKER
            // =====================================================

            speaker =
                speaker?.Trim()
                ?? string.Empty;


            if (speaker !=
                    "Student" &&
                speaker !=
                    "VoiceBot")
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Invalid transcript speaker."
                    }
                );
            }


            // =====================================================
            // VALIDATE TEXT
            // =====================================================

            text =
                text?.Trim()
                ?? string.Empty;


            if (string.IsNullOrWhiteSpace(
                text))
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Transcript text is empty."
                    }
                );
            }


            // =====================================================
            // AVOID EXACT DUPLICATE TRANSCRIPT
            // =====================================================

            var latestTranscript =
                await _context
                    .VoiceBotTranscripts

                    .Where(t =>
                        t.VoiceBotSessionId ==
                            sessionId &&

                        t.Speaker ==
                            speaker
                    )

                    .OrderByDescending(t =>
                        t.CreatedAt
                    )

                    .FirstOrDefaultAsync(
                        HttpContext.RequestAborted
                    );


            if (latestTranscript != null &&
                latestTranscript.TranscriptText ==
                    text)
            {
                return Json(
                    new
                    {
                        success = true,

                        transcriptId =
                            latestTranscript
                                .VoiceBotTranscriptId
                    }
                );
            }


            // =====================================================
            // SAVE TRANSCRIPT
            // =====================================================

            var transcript =
                new VoiceBotTranscript
                {
                    VoiceBotSessionId =
                        sessionId,

                    Speaker =
                        speaker,

                    TranscriptText =
                        text,

                    CreatedAt =
                        DateTime.Now
                };


            _context.VoiceBotTranscripts.Add(
                transcript
            );


            await _context.SaveChangesAsync(
                HttpContext.RequestAborted
            );


            return Json(
                new
                {
                    success = true,

                    transcriptId =
                        transcript
                            .VoiceBotTranscriptId
                }
            );
        }


        // =========================================================
        // UPDATE CURRENT STATUS
        // =========================================================
        //
        // Called after completed conversation turns.
        //
        // Uses all transcripts saved so far.
        //
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int sessionId,
            int? studentTranscriptId = null)
        {
            // ================= Student Session =================

            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );


            if (studentId == null)
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Your student session has expired."
                    }
                );
            }


            // ================= Verify Voice Session =================

            var voiceSession =
                await _context.VoiceBotSessions

                    .FirstOrDefaultAsync(
                        s =>
                            s.VoiceBotSessionId ==
                                sessionId &&

                            s.StudentId ==
                                studentId.Value &&

                            s.IsActive,

                        HttpContext.RequestAborted
                    );


            if (voiceSession == null)
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Active voice session was not found."
                    }
                );
            }


            try
            {
                var result =
                    await AnalyzeAndSaveStatusAsync(
                        voiceSession,
                        HttpContext.RequestAborted,
                        studentTranscriptId
                    );


                return Json(
                    new
                    {
                        success = true,

                        status =
                            result.Status,

                        summary =
                            result.Summary,

                        correctedStudentText =
                            result
                                .CorrectedLatestStudentText,

                        studentTranscriptId =
                            studentTranscriptId,

                        updatedAt =
                            DateTime.Now
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "VOICE BOT STATUS ANALYSIS ERROR"
                );

                Console.WriteLine(
                    ex.ToString()
                );

                Console.WriteLine(
                    "========================================"
                );


                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Voice status analysis failed: " +
                            ex.Message
                    }
                );
            }
        }


        // =========================================================
        // END VOICE SESSION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndSession(
            int sessionId)
        {
            // ================= Student Session =================

            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );


            if (studentId == null)
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Your student session has expired."
                    }
                );
            }


            // ================= Get Voice Session =================

            var voiceSession =
                await _context.VoiceBotSessions

                    .FirstOrDefaultAsync(
                        s =>
                            s.VoiceBotSessionId ==
                                sessionId &&

                            s.StudentId ==
                                studentId.Value,

                        HttpContext.RequestAborted
                    );


            if (voiceSession == null)
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Voice session was not found."
                    }
                );
            }


            // =====================================================
            // GET REPORT
            // =====================================================

            var report =
                await _context.VoiceBotReports

                    .FirstOrDefaultAsync(
                        r =>
                            r.VoiceBotSessionId ==
                                voiceSession
                                    .VoiceBotSessionId &&

                            r.StudentId ==
                                studentId.Value,

                        HttpContext.RequestAborted
                    );


            // =====================================================
            // ALREADY FINALIZED
            // =====================================================

            if (!voiceSession.IsActive &&
                report != null &&
                report.IsFinal)
            {
                return Json(
                    new
                    {
                        success = true,

                        reportId =
                            report.VoiceBotReportId,

                        finalStatus =
                            report.FinalStatus,

                        finalSummary =
                            report.FinalSummary,

                        redirectUrl =
                            Url.Action(
                                "Report",
                                "VoiceBot",
                                new
                                {
                                    id =
                                        report
                                            .VoiceBotReportId
                                }
                            )
                    }
                );
            }


            // =====================================================
            // FINAL STATUS ANALYSIS
            // =====================================================

            try
            {
                await AnalyzeAndSaveStatusAsync(
                    voiceSession,
                    HttpContext.RequestAborted
                );
            }
            catch (Exception ex)
            {
                // =================================================
                // Keep latest successful status if final analysis
                // temporarily fails.
                // =================================================

                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "VOICE BOT FINAL ANALYSIS ERROR"
                );

                Console.WriteLine(
                    ex.ToString()
                );

                Console.WriteLine(
                    "========================================"
                );
            }


            // =====================================================
            // RELOAD REPORT
            // =====================================================

            report =
                await _context.VoiceBotReports

                    .FirstOrDefaultAsync(
                        r =>
                            r.VoiceBotSessionId ==
                                voiceSession
                                    .VoiceBotSessionId &&

                            r.StudentId ==
                                studentId.Value,

                        HttpContext.RequestAborted
                    );


            // =====================================================
            // REPORT SHOULD EXIST
            // =====================================================

            if (report == null)
            {
                report =
                    new VoiceBotReport
                    {
                        VoiceBotSessionId =
                            voiceSession
                                .VoiceBotSessionId,

                        StudentId =
                            studentId.Value,

                        CurrentStatus =
                            voiceSession
                                .CurrentStatus,

                        CurrentSummary =
                            voiceSession
                                .CurrentSummary,

                        LastUpdatedAt =
                            DateTime.Now,

                        FinalStatus =
                            null,

                        FinalSummary =
                            null,

                        IsFinal =
                            false,

                        FinalizedAt =
                            null
                    };


                _context.VoiceBotReports.Add(
                    report
                );
            }


            // =====================================================
            // FINALIZE SESSION
            // =====================================================

            voiceSession.IsActive =
                false;


            voiceSession.EndedAt =
                DateTime.Now;


            // =====================================================
            // FINALIZE REPORT
            // =====================================================

            report.CurrentStatus =
                voiceSession.CurrentStatus;


            report.CurrentSummary =
                voiceSession.CurrentSummary;


            report.FinalStatus =
                voiceSession.CurrentStatus;


            report.FinalSummary =
                voiceSession.CurrentSummary;


            report.LastUpdatedAt =
                DateTime.Now;


            report.IsFinal =
                true;


            report.FinalizedAt =
                DateTime.Now;


            await _context.SaveChangesAsync(
                HttpContext.RequestAborted
            );


            // =====================================================
            // FINAL AUTO COUNSELING CHECK
            // =====================================================

            if (voiceSession.CurrentStatus ==
                    "Severe" ||
                voiceSession.CurrentStatus ==
                    "Extremely Severe")
            {
                try
                {
                    await _counselingSchedulerService
                        .AutoAssignPsychologistAsync(
                            studentId.Value,
                            voiceSession
                                .CurrentStatus,
                            "Voice Bot"
                        );
                }
                catch
                {
                    // =================================================
                    // Voice report remains finalized even if
                    // scheduling temporarily fails.
                    // =================================================
                }
            }


            // ================= Success =================

            return Json(
                new
                {
                    success = true,

                    reportId =
                        report.VoiceBotReportId,

                    finalStatus =
                        report.FinalStatus,

                    finalSummary =
                        report.FinalSummary,

                    redirectUrl =
                        Url.Action(
                            "Report",
                            "VoiceBot",
                            new
                            {
                                id =
                                    report
                                        .VoiceBotReportId
                            }
                        )
                }
            );
        }


        // =========================================================
        // VIEW VOICE BOT REPORT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Report(
            int id)
        {
            // ================= Student Session =================

            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );


            if (studentId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Student"
                );
            }


            // ================= Get Own Report Only =================

            var report =
                await _context.VoiceBotReports

                    .Include(r =>
                        r.Student
                    )

                    .Include(r =>
                        r.VoiceBotSession
                    )

                    .FirstOrDefaultAsync(r =>
                        r.VoiceBotReportId ==
                            id &&

                        r.StudentId ==
                            studentId.Value
                    );


            if (report == null)
            {
                return NotFound();
            }


            // =====================================================
            // STATUS HISTORY
            // =====================================================

            var riskHistory =
                await _context
                    .VoiceBotRiskAssessments

                    .Where(r =>
                        r.VoiceBotSessionId ==
                            report
                                .VoiceBotSessionId &&

                        r.StudentId ==
                            studentId.Value
                    )

                    .OrderBy(r =>
                        r.CreatedAt
                    )

                    .ToListAsync();


            ViewBag.RiskHistory =
                riskHistory;


            return View(
                report
            );
        }


        // =========================================================
        // PRIVATE:
        // ANALYZE + SAVE LIVE STATUS
        // =========================================================

        private async Task<VoiceBotAnalysisResult>
            AnalyzeAndSaveStatusAsync(
                VoiceBotSession voiceSession,
                CancellationToken cancellationToken,
                int? targetStudentTranscriptId = null)
        {
            // =====================================================
            // GET COMPLETE CURRENT TRANSCRIPT
            // =====================================================

            var transcripts =
                await _context
                    .VoiceBotTranscripts

                    .Where(t =>
                        t.VoiceBotSessionId ==
                            voiceSession
                                .VoiceBotSessionId
                    )

                    .OrderBy(t =>
                        t.CreatedAt
                    )

                    .ToListAsync(
                        cancellationToken
                    );


            // =====================================================
            // TARGET STUDENT TURN + ANALYSIS SNAPSHOT
            // =====================================================
            //
            // A queued live update stays attached to the exact
            // Student speech that triggered it.
            //
            // EndSession passes no ID and therefore still analyzes
            // the complete current transcript.
            //
            // =====================================================

            VoiceBotTranscript? targetStudentTranscript =
                null;


            var transcriptsForAnalysis =
                transcripts;


            if (
                targetStudentTranscriptId.HasValue
            )
            {
                targetStudentTranscript =
                    transcripts
                        .FirstOrDefault(t =>
                            t.VoiceBotTranscriptId ==
                                targetStudentTranscriptId.Value &&

                            t.Speaker ==
                                "Student"
                        );


                if (
                    targetStudentTranscript ==
                    null
                )
                {
                    throw new InvalidOperationException(
                        "The Student transcript for status analysis was not found."
                    );
                }


                transcriptsForAnalysis =
                    transcripts
                        .Where(t =>
                            t.VoiceBotTranscriptId <=
                                targetStudentTranscript
                                    .VoiceBotTranscriptId
                        )
                        .ToList();
            }
            else
            {
                targetStudentTranscript =
                    transcripts

                        .Where(t =>
                            t.Speaker ==
                                "Student"
                        )

                        .OrderByDescending(t =>
                            t.VoiceBotTranscriptId
                        )

                        .FirstOrDefault();
            }


            // =====================================================
            // ANALYZE WITH GEMINI
            // =====================================================

            var analysis =
                await _geminiLiveVoiceService
                    .AnalyzeCurrentConversationAsync(
                        transcriptsForAnalysis,
                        cancellationToken
                    );


            // =====================================================
            // APPLY CONSERVATIVE TRANSCRIPT CORRECTION
            // =====================================================

            if (
                targetStudentTranscript != null &&
                !string.IsNullOrWhiteSpace(
                    analysis
                        .CorrectedLatestStudentText
                ) &&
                !string.Equals(
                    targetStudentTranscript
                        .TranscriptText,
                    analysis
                        .CorrectedLatestStudentText,
                    StringComparison.Ordinal
                )
            )
            {
                targetStudentTranscript.TranscriptText =
                    analysis
                        .CorrectedLatestStudentText;
            }


            // =====================================================
            // UPDATE SESSION CURRENT STATUS
            // =====================================================

            voiceSession.CurrentStatus =
                analysis.Status;


            voiceSession.CurrentSummary =
                analysis.Summary;


            voiceSession.LastStatusUpdatedAt =
                DateTime.Now;


            // =====================================================
            // UPDATE LIVE REPORT
            // =====================================================

            var report =
                await _context.VoiceBotReports

                    .FirstOrDefaultAsync(
                        r =>
                            r.VoiceBotSessionId ==
                                voiceSession
                                    .VoiceBotSessionId,

                        cancellationToken
                    );


            if (report == null)
            {
                report =
                    new VoiceBotReport
                    {
                        VoiceBotSessionId =
                            voiceSession
                                .VoiceBotSessionId,

                        StudentId =
                            voiceSession.StudentId,

                        CurrentStatus =
                            analysis.Status,

                        CurrentSummary =
                            analysis.Summary,

                        LastUpdatedAt =
                            DateTime.Now,

                        IsFinal =
                            false
                    };


                _context.VoiceBotReports.Add(
                    report
                );
            }
            else
            {
                report.CurrentStatus =
                    analysis.Status;


                report.CurrentSummary =
                    analysis.Summary;


                report.LastUpdatedAt =
                    DateTime.Now;
            }


            // =====================================================
            // SAVE STATUS HISTORY
            // Only save when latest snapshot changed.
            // =====================================================

            var latestRisk =
                await _context
                    .VoiceBotRiskAssessments

                    .Where(r =>
                        r.VoiceBotSessionId ==
                            voiceSession
                                .VoiceBotSessionId
                    )

                    .OrderByDescending(r =>
                        r.CreatedAt
                    )

                    .FirstOrDefaultAsync(
                        cancellationToken
                    );


            var shouldCreateHistory =
                latestRisk == null ||

                latestRisk.RiskStatus !=
                    analysis.Status ||

                latestRisk.Summary !=
                    analysis.Summary;


            if (shouldCreateHistory)
            {
                _context
                    .VoiceBotRiskAssessments
                    .Add(
                        new VoiceBotRiskAssessment
                        {
                            VoiceBotSessionId =
                                voiceSession
                                    .VoiceBotSessionId,

                            StudentId =
                                voiceSession
                                    .StudentId,

                            RiskStatus =
                                analysis.Status,

                            Summary =
                                analysis.Summary,

                            CreatedAt =
                                DateTime.Now
                        }
                    );
            }


            // ================= Save =================

            await _context.SaveChangesAsync(
                cancellationToken
            );


            // =====================================================
            // AUTO COUNSELING
            // =====================================================

            if (analysis.Status ==
                    "Severe" ||
                analysis.Status ==
                    "Extremely Severe")
            {
                try
                {
                    await _counselingSchedulerService
                        .AutoAssignPsychologistAsync(
                            voiceSession.StudentId,
                            analysis.Status,
                            "Voice Bot"
                        );
                }
                catch
                {
                    // =================================================
                    // Status/report remains saved even if appointment
                    // scheduling temporarily fails.
                    // =================================================
                }
            }


            return analysis;
        }
    }
}