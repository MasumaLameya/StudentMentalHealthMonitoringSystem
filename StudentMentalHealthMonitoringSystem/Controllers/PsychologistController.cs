using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentMentalHealthMonitoringSystem.Data;
using StudentMentalHealthMonitoringSystem.Models;
using StudentMentalHealthMonitoringSystem.Services;
using StudentMentalHealthMonitoringSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StudentMentalHealthMonitoringSystem.Controllers
{
    public class PsychologistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly CounselingSchedulerService _counselingSchedulerService;

        public PsychologistController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            CounselingSchedulerService counselingSchedulerService)
        {
            _context = context;
            _environment = environment;
            _counselingSchedulerService = counselingSchedulerService;
        }


        // =========================================================
        // REGISTER
        // =========================================================

        // ================= Register GET =================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        // ================= Register POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            Psychologist psychologist)
        {
            // ================= Model Validation =================

            if (!ModelState.IsValid)
            {
                var errors =
                    ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                ViewBag.Errors =
                    string.Join(" | ", errors);

                return View(psychologist);
            }


            // ================= Duplicate Email =================

            if (_context.Psychologists.Any(
                p => p.Email == psychologist.Email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Email already exists."
                );

                return View(psychologist);
            }


            try
            {
                // ================= Password Hash =================

                psychologist.Password =
                    BCrypt.Net.BCrypt.HashPassword(
                        psychologist.Password
                    );


                // ================= Upload Profile Image =================

                if (psychologist.ImageFile != null &&
                    psychologist.ImageFile.Length > 0)
                {
                    var allowedExtensions =
                        new[]
                        {
                            ".jpg",
                            ".jpeg",
                            ".png"
                        };


                    var extension =
                        Path.GetExtension(
                            psychologist.ImageFile.FileName
                        )
                        .ToLower();


                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError(
                            "ImageFile",
                            "Only JPG, JPEG and PNG images are allowed."
                        );

                        return View(psychologist);
                    }


                    var uploadFolder =
                        Path.Combine(
                            _environment.WebRootPath,
                            "images",
                            "psychologists"
                        );


                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(
                            uploadFolder
                        );
                    }


                    var fileName =
                        $"{Guid.NewGuid()}{extension}";


                    var fullPath =
                        Path.Combine(
                            uploadFolder,
                            fileName
                        );


                    await using var stream =
                        new FileStream(
                            fullPath,
                            FileMode.Create
                        );


                    await psychologist.ImageFile
                        .CopyToAsync(stream);


                    psychologist.ProfileImage =
                        $"/images/psychologists/{fileName}";
                }


                // ================= Save Psychologist =================

                _context.Psychologists.Add(
                    psychologist
                );

                await _context.SaveChangesAsync();


                TempData["Success"] =
                    "Registration Successful.";


                return RedirectToAction(
                    "Login"
                );
            }
            catch (Exception ex)
            {
                return Content(
                    ex.ToString()
                );
            }
        }


        // =========================================================
        // LOGIN
        // =========================================================

        // ================= Login GET =================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        // ================= Login POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(
            string email,
            string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Invalid Email or Password";
                return View();
            }

            var cleanEmail = email.Trim().ToLower();

            // ================= Get Psychologist =================
            var psychologist = _context.Psychologists
                .FirstOrDefault(p => p.Email.ToLower() == cleanEmail);

            if (psychologist == null)
            {
                ViewBag.Error = "Invalid Email or Password";
                return View();
            }

            // ================= Password Check =================
            bool isPasswordValid = false;
            try
            {
                if (!string.IsNullOrEmpty(psychologist.Password))
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(password, psychologist.Password);
                }
            }
            catch
            {
                // Fallback to plain text check if not BCrypt hashed
                isPasswordValid = (psychologist.Password == password);
            }

            if (!isPasswordValid && psychologist.Password == password)
            {
                isPasswordValid = true;
            }

            if (!isPasswordValid)
            {
                ViewBag.Error = "Invalid Email or Password";
                return View();
            }


            // ================= Create Session =================

            HttpContext.Session.SetInt32(
                "PsychologistId",
                psychologist.PsychologistId
            );


            HttpContext.Session.SetString(
                "PsychologistName",
                psychologist.FullName
            );


            return RedirectToAction(
                "Dashboard"
            );
        }


        // =========================================================
        // DASHBOARD
        // =========================================================

        public IActionResult Dashboard()
        {
            // ================= Check Session =================

            var psychologistId =
                HttpContext.Session.GetInt32(
                    "PsychologistId"
                );


            if (psychologistId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // ================= Get Psychologist =================

            var psychologist =
                _context.Psychologists
                    .FirstOrDefault(
                        p => p.PsychologistId ==
                             psychologistId.Value
                    );


            if (psychologist == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // =====================================================
            // ASSIGNED HIGH RISK STUDENTS
            // =====================================================
            //
            // Only students assigned to this psychologist
            // through automatic serious-risk assignment.
            //
            // Trigger may come from:
            //
            // PHQ-9
            // C-SSRS
            // Feelings
            // AI Chat
            // Voice Bot
            //
            // =====================================================

            var highRiskStudents =
                _context.Counselings
                    .Where(c =>
                        c.PsychologistId ==
                            psychologistId.Value &&

                        c.AppointmentSource ==
                            "AutoAssignment" &&

                        c.Status !=
                            "Cancelled" &&

                        (
                            c.TriggerSeverity ==
                                "Severe" ||

                            c.TriggerSeverity ==
                                "Extremely Severe"
                        )
                    )
                    .Select(c =>
                        c.StudentId
                    )
                    .Distinct()
                    .Count();


            // ================= Dashboard Model =================

            var model =
                new PsychologistDashboardViewModel();


            model.Psychologist =
                psychologist;


            model.HighRiskStudents =
                highRiskStudents;


            // Today's assigned counseling appointments

            model.TodaySessions =
                _context.Counselings
                    .Count(c =>
                        c.PsychologistId ==
                            psychologistId.Value &&

                        c.CounselingDate.Date ==
                            DateTime.Today &&

                        c.Status !=
                            "Cancelled"
                    );


            // Completed by logged-in psychologist

            model.CompletedSessions =
                _context.Counselings
                    .Count(c =>
                        c.PsychologistId ==
                            psychologistId.Value &&

                        c.Status ==
                            "Completed"
                    );


            // Confirmed appointments waiting for counseling

            model.PendingSessions =
                _context.Counselings
                    .Count(c =>
                        c.PsychologistId ==
                            psychologistId.Value &&

                        c.Status ==
                            "Confirmed"
                    );


            return View(
                model
            );
        }


        // =========================================================
        // HIGH RISK STUDENTS
        // =========================================================

        public IActionResult Students()
        {
            // ================= Check Session =================

            var psychologistId =
                HttpContext.Session.GetInt32(
                    "PsychologistId"
                );


            if (psychologistId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // =====================================================
            // GET ASSIGNED HIGH RISK APPOINTMENTS
            // =====================================================

            var assignedAppointments =
                _context.Counselings

                    .Where(c =>
                        c.PsychologistId ==
                            psychologistId.Value &&

                        c.AppointmentSource ==
                            "AutoAssignment" &&

                        c.Status !=
                            "Cancelled" &&

                        (
                            c.TriggerSeverity ==
                                "Severe" ||

                            c.TriggerSeverity ==
                                "Extremely Severe"
                        )
                    )

                    .OrderByDescending(c =>
                        c.CreatedAt
                    )

                    .ToList();


            // ================= High Risk List =================

            List<PsychologistStudentViewModel>
                highRiskStudents =
                    new List<PsychologistStudentViewModel>();


            // =====================================================
            // ONE STUDENT ONLY ONCE
            // =====================================================

            var studentIds =
                assignedAppointments
                    .Select(c =>
                        c.StudentId
                    )
                    .Distinct()
                    .ToList();


            foreach (var studentId
                in studentIds)
            {
                // ================= Student =================

                var student =
                    _context.Students
                        .FirstOrDefault(
                            s =>
                                s.StudentId ==
                                    studentId
                        );


                if (student == null)
                {
                    continue;
                }


                // ================= Latest Assignment =================

                var latestAssignment =
                    assignedAppointments
                        .Where(c =>
                            c.StudentId ==
                                student.StudentId
                        )
                        .OrderByDescending(c =>
                            c.CreatedAt
                        )
                        .FirstOrDefault();


                // ================= Latest PHQ =================

                var phq =
                    _context.PHQAssessments
                        .Where(p =>
                            p.StudentId ==
                                student.StudentId
                        )
                        .OrderByDescending(
                            p => p.AssessmentDate
                        )
                        .FirstOrDefault();


                // ================= Latest C-SSRS =================

                var cssrs =
                    _context.CSSRSAssessments
                        .Where(c =>
                            c.StudentId ==
                                student.StudentId
                        )
                        .OrderByDescending(
                            c => c.AssessmentDate
                        )
                        .FirstOrDefault();


                // ================= Latest Feelings =================

                var semesterRecord =
                    _context
                        .StudentSemesterRecords
                        .Where(r =>
                            r.StudentId ==
                                student.StudentId
                        )
                        .OrderByDescending(r =>
                            r.UpdatedAt ??
                            r.SubmittedAt
                        )
                        .FirstOrDefault();


                // ================= Add Student =================

                highRiskStudents.Add(
                    new PsychologistStudentViewModel
                    {
                        Student =
                            student,

                        PHQAssessment =
                            phq,

                        CSSRSAssessment =
                            cssrs,

                        SemesterRecord =
                            semesterRecord,

                        TriggerSource =
                            latestAssignment
                                ?.TriggerSource,

                        TriggerSeverity =
                            latestAssignment
                                ?.TriggerSeverity
                    }
                );
            }


            return View(
                highRiskStudents
            );
        }


        // =========================================================
        // STUDENT DETAILS
        // =========================================================

        public IActionResult StudentDetails(
            int id)
        {
            // ================= Check Session =================

            var psychologistId =
                HttpContext.Session.GetInt32(
                    "PsychologistId"
                );


            if (psychologistId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // ================= Get Student =================

            var student =
                _context.Students
                    .FirstOrDefault(
                        s => s.StudentId == id
                    );


            if (student == null)
            {
                return RedirectToAction(
                    "Students"
                );
            }


            // ================= View Model =================

            var model =
                new PsychologistStudentViewModel();


            model.Student =
                student;


            model.PHQAssessment =
                _context.PHQAssessments
                    .Where(p =>
                        p.StudentId == id
                    )
                    .OrderByDescending(
                        p => p.AssessmentDate
                    )
                    .FirstOrDefault();


            model.CSSRSAssessment =
                _context.CSSRSAssessments
                    .Where(c =>
                        c.StudentId == id
                    )
                    .OrderByDescending(
                        c => c.AssessmentDate
                    )
                    .FirstOrDefault();


            model.SemesterRecord =
                _context.StudentSemesterRecords
                    .Where(r =>
                        r.StudentId == id
                    )
                    .OrderByDescending(
                        r => r.SubmittedAt
                    )
                    .FirstOrDefault();


            return View(model);
        }


        // =========================================================
        // COUNSELING
        // =========================================================

        // ================= Counseling GET =================

        [HttpGet]
        public IActionResult Counseling(
            int id)
        {
            // ================= Check Session =================

            var psychologistId =
                HttpContext.Session.GetInt32(
                    "PsychologistId"
                );


            if (psychologistId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // ================= Get Student =================

            var student =
                _context.Students
                    .FirstOrDefault(
                        s => s.StudentId == id
                    );


            if (student == null)
            {
                return RedirectToAction(
                    "Students"
                );
            }


            // ================= Create Counseling =================

            Counseling counseling =
                new Counseling();


            counseling.StudentId =
                student.StudentId;


            counseling.PsychologistId =
                psychologistId.Value;


            counseling.CounselingDate =
                DateTime.Now;


            ViewBag.Student =
                student;


            return View(counseling);
        }


        // ================= Counseling POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Counseling(
            Counseling counseling)
        {
            // ================= Check Session =================

            var psychologistId =
                HttpContext.Session.GetInt32(
                    "PsychologistId"
                );


            if (psychologistId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // ================= Validation =================

            if (!ModelState.IsValid)
            {
                ViewBag.Student =
                    _context.Students
                        .FirstOrDefault(
                            s => s.StudentId ==
                                 counseling.StudentId
                        );


                return View(counseling);
            }


            // ================= Set Values =================

            counseling.PsychologistId =
                psychologistId.Value;


            counseling.CounselingDate =
                DateTime.Now;


            // ================= Save =================

            _context.Counselings.Add(
                counseling
            );


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Counseling information saved successfully.";


            return RedirectToAction(
                "StudentDetails",
                new
                {
                    id =
                        counseling.StudentId
                }
            );
        }


        // =========================================================
        // APPOINTMENT
        // =========================================================

        // ================= Appointment GET =================

        [HttpGet]
        public async Task<IActionResult> Appointment()
        {
            // ================= Check Session =================

            var psychologistId =
                HttpContext.Session.GetInt32(
                    "PsychologistId"
                );


            if (psychologistId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // ================= Get Psychologist =================

            var psychologist =
                await _context.Psychologists
                    .FirstOrDefaultAsync(p =>
                        p.PsychologistId ==
                            psychologistId.Value
                    );


            if (psychologist == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // ================= Assigned Appointments =================

            var appointments =
                await _context.Counselings
                    .Include(c => c.Student)
                    .Include(c => c.Psychologist)

                    // Only logged-in psychologist
                    .Where(c =>
                        c.PsychologistId ==
                            psychologistId.Value
                    )

                    .OrderBy(c =>
                        c.CounselingDate
                    )

                    .ThenBy(c =>
                        c.AppointmentTime
                    )

                    .ToListAsync();


            // ================= Psychologist Name =================

            ViewBag.PsychologistName =
                psychologist.FullName;


            return View(
                appointments
            );
        }


        /// =========================================================
        // COUNSELING DETAILS
        // =========================================================

        // ================= Counseling Details GET =================

        [HttpGet]
        public async Task<IActionResult> CounselingDetails(
            int id)
        {
            // ================= Check Session =================

            var psychologistId =
                HttpContext.Session.GetInt32(
                    "PsychologistId"
                );


            if (psychologistId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // ================= Get Counseling =================

            var counseling =
                await _context.Counselings
                    .Include(c => c.Student)
                    .Include(c => c.Psychologist)
                    .FirstOrDefaultAsync(c =>
                        c.CounselingId == id &&
                        c.PsychologistId ==
                            psychologistId.Value
                    );


            if (counseling == null)
            {
                return NotFound();
            }


            // ================= Latest PHQ-9 =================

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


            // ================= Latest C-SSRS =================

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


            // ================= Existing Observation =================

            var existingObservation =
                await _context.CounselingObservations
                    .FirstOrDefaultAsync(o =>
                        o.CounselingId ==
                            counseling.CounselingId
                    );


            // ================= View Model =================

            var model =
                new CounselingObservationViewModel
                {
                    CounselingId =
                        counseling.CounselingId,

                    Counseling =
                        counseling,

                    LatestPHQScore =
                        latestPHQ?.TotalScore,

                    LatestPHQOfficialInterpretation =
                        latestPHQ?.SeverityLevel,

                    LatestPHQProjectStatus =
                        GetObservationPHQProjectStatus(
                            latestPHQ?.SeverityLevel
                        ),

                    LatestCSSRSRiskLevel =
                        latestCSSRS?.RiskLevel,

                    LatestCSSRSProjectStatus =
                        GetObservationCSSRSProjectStatus(
                            latestCSSRS?.RiskLevel
                        ),

                    CurrentMentalHealthStatus =
                        counseling.RiskLevel,

                    NextFollowUpDate =
                        counseling.NextFollowUpDate,

                    NextFollowUpTime =
                        counseling.NextFollowUpTime,

                    AppointmentRoom =
                        counseling.AppointmentRoom
                };


            // =====================================================
            // LOAD SAVED OBSERVATION
            // =====================================================

            if (existingObservation != null)
            {
                model.OverallProgressStatus =
                    existingObservation
                        .OverallProgressStatus;


                model.CurrentMentalHealthStatus =
                    existingObservation
                        .CurrentMentalHealthStatus;


                model.LatestPHQScore =
                    existingObservation
                        .PHQScore;


                model.LatestPHQOfficialInterpretation =
                    existingObservation
                        .PHQOfficialInterpretation;


                model.LatestPHQProjectStatus =
                    existingObservation
                        .PHQProjectStatus;


                model.LatestCSSRSRiskLevel =
                    existingObservation
                        .CSSRSRiskLevel;


                model.LatestCSSRSProjectStatus =
                    existingObservation
                        .CSSRSProjectStatus;


                model.AcademicFunctioning =
                    existingObservation
                        .AcademicFunctioning;


                model.SleepCondition =
                    existingObservation
                        .SleepCondition;


                model.SocialInteraction =
                    existingObservation
                        .SocialInteraction;


                model.DailyActivities =
                    existingObservation
                        .DailyActivities;


                model.EmotionalRegulation =
                    existingObservation
                        .EmotionalRegulation;


                model.CurrentSafetyRisk =
                    existingObservation
                        .CurrentSafetyRisk;


                model.ClinicalObservation =
                    existingObservation
                        .ClinicalObservation;


                model.StudentReportedImprovement =
                    existingObservation
                        .StudentReportedImprovement;


                model.AssessmentSummary =
                    existingObservation
                        .AssessmentSummary;


                model.FollowUpRequired =
                    existingObservation
                        .FollowUpRequired;


                if (!string.IsNullOrWhiteSpace(
                    existingObservation
                        .AssessmentBasis))
                {
                    model.AssessmentBasis =
                        existingObservation
                            .AssessmentBasis
                            .Split(
                                '|',
                                StringSplitOptions
                                    .RemoveEmptyEntries
                            )
                            .Select(x =>
                                x.Trim()
                            )
                            .ToList();
                }


                if (!string.IsNullOrWhiteSpace(
                    existingObservation
                        .RecommendedAction))
                {
                    model.RecommendedAction =
                        existingObservation
                            .RecommendedAction
                            .Split(
                                '|',
                                StringSplitOptions
                                    .RemoveEmptyEntries
                            )
                            .Select(x =>
                                x.Trim()
                            )
                            .ToList();
                }
            }


            return View(
                model
            );
        }


        // ================= Counseling Details POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CounselingDetails(
            CounselingObservationViewModel model,
            string? completionType)
        {
            // ================= Check Session =================

            var psychologistId =
                HttpContext.Session.GetInt32(
                    "PsychologistId"
                );


            if (psychologistId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // ================= Get Counseling =================

            var counseling =
                await _context.Counselings
                    .Include(c => c.Student)
                    .Include(c => c.Psychologist)
                    .FirstOrDefaultAsync(c =>
                        c.CounselingId ==
                            model.CounselingId &&

                        c.PsychologistId ==
                            psychologistId.Value
                    );


            if (counseling == null)
            {
                return NotFound();
            }


            // =====================================================
            // CLEAR AUTOMATIC MODEL VALIDATION
            // =====================================================

            ModelState.Clear();


            // =====================================================
            // PREPARE MULTIPLE SELECTION LISTS
            // =====================================================

            model.AssessmentBasis ??=
                new List<string>();


            model.RecommendedAction ??=
                new List<string>();


            // =====================================================
            // COMPLETION TYPE
            // =====================================================

            if (completionType ==
                "WithFollowUp")
            {
                model.FollowUpRequired =
                    true;
            }
            else if (completionType ==
                     "WithoutFollowUp")
            {
                model.FollowUpRequired =
                    false;


                model.NextFollowUpDate =
                    null;


                model.NextFollowUpTime =
                    null;
            }
            else
            {
                ModelState.AddModelError(
                    "",
                    "Please select how the counseling should be completed."
                );
            }


            // ================= Overall Progress =================

            if (string.IsNullOrWhiteSpace(
                model.OverallProgressStatus))
            {
                ModelState.AddModelError(
                    nameof(model.OverallProgressStatus),
                    "Please select the overall progress status."
                );
            }


            // ================= Current Mental Health =================

            if (string.IsNullOrWhiteSpace(
                model.CurrentMentalHealthStatus))
            {
                ModelState.AddModelError(
                    nameof(model.CurrentMentalHealthStatus),
                    "Please select the current mental health status."
                );
            }


            // ================= Academic Functioning =================

            if (string.IsNullOrWhiteSpace(
                model.AcademicFunctioning))
            {
                ModelState.AddModelError(
                    nameof(model.AcademicFunctioning),
                    "Please select academic functioning."
                );
            }


            // ================= Sleep Condition =================

            if (string.IsNullOrWhiteSpace(
                model.SleepCondition))
            {
                ModelState.AddModelError(
                    nameof(model.SleepCondition),
                    "Please select the sleep condition."
                );
            }


            // ================= Social Interaction =================

            if (string.IsNullOrWhiteSpace(
                model.SocialInteraction))
            {
                ModelState.AddModelError(
                    nameof(model.SocialInteraction),
                    "Please select social interaction."
                );
            }


            // ================= Daily Activities =================

            if (string.IsNullOrWhiteSpace(
                model.DailyActivities))
            {
                ModelState.AddModelError(
                    nameof(model.DailyActivities),
                    "Please select daily activities."
                );
            }


            // ================= Emotional Regulation =================

            if (string.IsNullOrWhiteSpace(
                model.EmotionalRegulation))
            {
                ModelState.AddModelError(
                    nameof(model.EmotionalRegulation),
                    "Please select emotional regulation."
                );
            }


            // ================= Current Safety Risk =================

            if (string.IsNullOrWhiteSpace(
                model.CurrentSafetyRisk))
            {
                ModelState.AddModelError(
                    nameof(model.CurrentSafetyRisk),
                    "Please select the current safety risk."
                );
            }


            // ================= Clinical Observation =================

            if (string.IsNullOrWhiteSpace(
                model.ClinicalObservation))
            {
                ModelState.AddModelError(
                    nameof(model.ClinicalObservation),
                    "Please enter the clinical observation."
                );
            }


            // ================= Student-Reported Improvement =================

            if (string.IsNullOrWhiteSpace(
                model.StudentReportedImprovement))
            {
                ModelState.AddModelError(
                    nameof(model.StudentReportedImprovement),
                    "Please select the student-reported improvement."
                );
            }


            // =====================================================
            // FOLLOW-UP VALIDATION
            // =====================================================

            if (completionType ==
                "WithFollowUp")
            {
                if (!model.NextFollowUpDate.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(model.NextFollowUpDate),
                        "Please select the follow-up date."
                    );
                }


                if (!model.NextFollowUpTime.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(model.NextFollowUpTime),
                        "Please select the follow-up time."
                    );
                }
            }


            if (model.NextFollowUpDate.HasValue &&
                model.NextFollowUpDate.Value.Date <
                    DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.NextFollowUpDate),
                    "Follow-up date cannot be in the past."
                );
            }


            // =====================================================
            // LOAD LATEST PHQ-9
            // =====================================================

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


            // =====================================================
            // LOAD LATEST C-SSRS
            // =====================================================

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


            // =====================================================
            // RELOAD DISPLAY INFORMATION
            // =====================================================

            model.Counseling =
                counseling;


            model.LatestPHQScore =
                latestPHQ?.TotalScore;


            model.LatestPHQOfficialInterpretation =
                latestPHQ?.SeverityLevel;


            model.LatestPHQProjectStatus =
                GetObservationPHQProjectStatus(
                    latestPHQ?.SeverityLevel
                );


            model.LatestCSSRSRiskLevel =
                latestCSSRS?.RiskLevel;


            model.LatestCSSRSProjectStatus =
                GetObservationCSSRSProjectStatus(
                    latestCSSRS?.RiskLevel
                );


            // =====================================================
            // VALIDATION FAILED
            // =====================================================

            if (!ModelState.IsValid)
            {
                return View(
                    model
                );
            }


            // =====================================================
            // FIND ROOT COUNSELING
            // =====================================================

            var rootCounselingId =
                await GetObservationRootCounselingIdAsync(
                    counseling.CounselingId
                );


            var rootCounseling =
                await _context.Counselings
                    .FirstOrDefaultAsync(c =>
                        c.CounselingId ==
                            rootCounselingId
                    );


            if (rootCounseling == null)
            {
                return NotFound();
            }


            // =====================================================
            // MULTIPLE SELECTION VALUES
            // =====================================================

            var assessmentBasisText =
                string.Join(
                    " | ",
                    model.AssessmentBasis
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x)
                        )
                        .Select(x =>
                            x.Trim()
                        )
                );


            var recommendedActionText =
                string.Join(
                    " | ",
                    model.RecommendedAction
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x)
                        )
                        .Select(x =>
                            x.Trim()
                        )
                );


            // =====================================================
            // CREATE / UPDATE SESSION OBSERVATION
            // =====================================================

            var observation =
                await _context.CounselingObservations
                    .FirstOrDefaultAsync(o =>
                        o.CounselingId ==
                            counseling.CounselingId
                    );


            if (observation == null)
            {
                observation =
                    new CounselingObservation
                    {
                        CounselingId =
                            counseling.CounselingId,

                        RootCounselingId =
                            rootCounselingId,

                        StudentId =
                            counseling.StudentId,

                        PsychologistId =
                            psychologistId.Value,

                        CreatedAt =
                            DateTime.Now
                    };


                _context.CounselingObservations.Add(
                    observation
                );
            }
            else
            {
                observation.UpdatedAt =
                    DateTime.Now;
            }


            // ================= Observation Data =================

            observation.OverallProgressStatus =
                model.OverallProgressStatus!
                    .Trim();


            observation.CurrentMentalHealthStatus =
                model.CurrentMentalHealthStatus!
                    .Trim();


            observation.PHQScore =
                latestPHQ?.TotalScore;


            observation.PHQOfficialInterpretation =
                latestPHQ?.SeverityLevel;


            observation.PHQProjectStatus =
                GetObservationPHQProjectStatus(
                    latestPHQ?.SeverityLevel
                );


            observation.CSSRSRiskLevel =
                latestCSSRS?.RiskLevel;


            observation.CSSRSProjectStatus =
                GetObservationCSSRSProjectStatus(
                    latestCSSRS?.RiskLevel
                );


            observation.AcademicFunctioning =
                model.AcademicFunctioning!
                    .Trim();


            observation.SleepCondition =
                model.SleepCondition!
                    .Trim();


            observation.SocialInteraction =
                model.SocialInteraction!
                    .Trim();


            observation.DailyActivities =
                model.DailyActivities!
                    .Trim();


            observation.EmotionalRegulation =
                model.EmotionalRegulation!
                    .Trim();


            observation.CurrentSafetyRisk =
                model.CurrentSafetyRisk!
                    .Trim();


            observation.AssessmentBasis =
                assessmentBasisText;


            observation.ClinicalObservation =
                model.ClinicalObservation!
                    .Trim();


            observation.StudentReportedImprovement =
                model.StudentReportedImprovement!
                    .Trim();


            observation.AssessmentSummary =
                model.AssessmentSummary?.Trim() ?? string.Empty;


            observation.RecommendedAction =
                recommendedActionText;


            observation.FollowUpRequired =
                model.FollowUpRequired == true;


            // =====================================================
            // UPDATE CURRENT COUNSELING
            // =====================================================

            counseling.RiskLevel =
                model.CurrentMentalHealthStatus!
                    .Trim();


            counseling.AppointmentRoom =
                model.AppointmentRoom?
                    .Trim();


            counseling.NextFollowUpDate =
                model.NextFollowUpDate;


            counseling.NextFollowUpTime =
                model.NextFollowUpTime;


            counseling.Status =
                "Completed";


            // =====================================================
            // CREATE / UPDATE OBSERVATION REPORT
            // =====================================================

            var observationReport =
                await _context.ObservationReports
                    .FirstOrDefaultAsync(r =>
                        r.RootCounselingId ==
                            rootCounselingId
                    );


            string initialStatus;


            if (!string.IsNullOrWhiteSpace(
                rootCounseling.TriggerSeverity))
            {
                initialStatus =
                    rootCounseling
                        .TriggerSeverity!;
            }
            else if (!string.IsNullOrWhiteSpace(
                rootCounseling.RiskLevel))
            {
                initialStatus =
                    rootCounseling
                        .RiskLevel!;
            }
            else
            {
                initialStatus =
                    model.CurrentMentalHealthStatus!;
            }


            if (observationReport == null)
            {
                observationReport =
                    new ObservationReport
                    {
                        RootCounselingId =
                            rootCounselingId,

                        StudentId =
                            counseling.StudentId,

                        PsychologistId =
                            rootCounseling
                                .PsychologistId,

                        InitialStatus =
                            initialStatus,

                        Semester =
                            rootCounseling.Student?.Semester ?? "Semester 1",

                        CreatedAt =
                            DateTime.Now,

                        UpdatedAt =
                            DateTime.Now
                    };


                _context.ObservationReports.Add(
                    observationReport
                );
            }
            else
            {
                observationReport.Semester = rootCounseling.Student?.Semester ?? observationReport.Semester;
                observationReport.UpdatedAt = DateTime.Now;
            }


            // ================= Latest Condition =================

            observationReport.CurrentStatus =
                model.CurrentMentalHealthStatus!
                    .Trim();


            observationReport.OverallProgressStatus =
                model.OverallProgressStatus!
                    .Trim();


            observationReport.CurrentSafetyRisk =
                model.CurrentSafetyRisk!
                    .Trim();


            observationReport.LatestAssessmentBasis =
                assessmentBasisText;


            observationReport.LatestRecommendedAction =
                recommendedActionText;


            observationReport.LatestConditionSummary =
                model.AssessmentSummary?.Trim() ?? string.Empty;


            observationReport.IsFinal =
                model.FollowUpRequired != true;


            observationReport.FinalizedAt =
                model.FollowUpRequired == true
                    ? null
                    : DateTime.Now;


            observationReport.UpdatedAt =
                DateTime.Now;


            // =====================================================
            // COMPLETE WITH FOLLOW-UP
            // =====================================================

            if (completionType ==
                    "WithFollowUp" &&
                model.NextFollowUpDate.HasValue &&
                model.NextFollowUpTime.HasValue)
            {
                var followUpResult =
                    await _counselingSchedulerService
                        .CreateFollowUpAppointmentAsync(
                            counseling,
                            model.NextFollowUpDate.Value,
                            model.NextFollowUpTime.Value
                        );


                // ================= Follow-up Failed =================

                if (!followUpResult.Success)
                {
                    ModelState.AddModelError(
                        "",
                        followUpResult.Message
                    );


                    return View(
                        model
                    );
                }


                // ================= Follow-up Success =================

                if (followUpResult.Created)
                {
                    TempData["Success"] =
                        "Observation saved and follow-up appointment created successfully.";
                }
                else
                {
                    TempData["Success"] =
                        "Observation saved successfully. The follow-up appointment already exists.";
                }


                return RedirectToAction(
                    "Appointment"
                );
            }


            // =====================================================
            // NO FOLLOW-UP NEEDED
            // =====================================================

            counseling.NextFollowUpDate =
                null;


            counseling.NextFollowUpTime =
                null;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Observation saved successfully. No further follow-up is required.";


            return RedirectToAction(
                "Appointment"
            );
        }


        // =========================================================
        // OBSERVATION ROOT COUNSELING
        // =========================================================

        private async Task<int>
            GetObservationRootCounselingIdAsync(
                int counselingId)
        {
            var currentCounselingId =
                counselingId;


            while (true)
            {
                var currentCounseling =
                    await _context.Counselings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c =>
                            c.CounselingId ==
                                currentCounselingId
                        );


                if (currentCounseling == null ||
                    !currentCounseling
                        .ParentCounselingId
                        .HasValue)
                {
                    return currentCounselingId;
                }


                currentCounselingId =
                    currentCounseling
                        .ParentCounselingId
                        .Value;
            }
        }


        // =========================================================
        // PHQ-9 PROJECT STATUS FOR OBSERVATION
        // =========================================================

        private string GetObservationPHQProjectStatus(
            string? severityLevel)
        {
            if (string.IsNullOrWhiteSpace(
                severityLevel))
            {
                return "Not Assessed";
            }


            if (severityLevel ==
                "Minimal")
            {
                return "Normal";
            }


            if (severityLevel ==
                    "Mild" ||
                severityLevel ==
                    "Moderate")
            {
                return "Moderate";
            }


            if (severityLevel ==
                "Moderately Severe")
            {
                return "Severe";
            }


            if (severityLevel ==
                "Severe")
            {
                return "Extremely Severe";
            }


            return "Normal";
        }


        // =========================================================
        // C-SSRS PROJECT STATUS FOR OBSERVATION
        // =========================================================

        private string GetObservationCSSRSProjectStatus(
            string? riskLevel)
        {
            if (string.IsNullOrWhiteSpace(
                riskLevel))
            {
                return "Not Assessed";
            }


            if (riskLevel ==
                "Moderate")
            {
                return "Moderate";
            }


            if (riskLevel ==
                "High")
            {
                return "Severe";
            }


            return "Normal";
        }

        // =========================================================
        // SCREENING REPORTS
        // =========================================================

        // ================= Screening Reports =================

        [HttpGet]
        public async Task<IActionResult> ScreeningReports()
        {
            // ================= Check Session =================

            var psychologistId =
                HttpContext.Session.GetInt32(
                    "PsychologistId"
                );


            if (psychologistId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // ================= Assigned Screening Reports =================

            var screeningReports =
                await _context.ScreeningReports

                    .Include(r =>
                        r.Student
                    )

                    .Include(r =>
                        r.Counseling
                    )

                    .Where(r =>
                        r.PsychologistId ==
                            psychologistId.Value
                    )

                    .OrderByDescending(r =>
                        r.CreatedAt
                    )

                    .ToListAsync();


            return View(
                screeningReports
            );
        }


        // ================= Screening Report Details =================

        [HttpGet]
        public async Task<IActionResult> ScreeningReportDetails(
            int id)
        {
            // ================= Check Session =================

            var psychologistId =
                HttpContext.Session.GetInt32(
                    "PsychologistId"
                );


            if (psychologistId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // ================= Get Screening Report =================

            var screeningReport =
                await _context.ScreeningReports

                    .Include(r =>
                        r.Student
                    )

                    .Include(r =>
                        r.Counseling
                    )

                    .FirstOrDefaultAsync(r =>
                        r.ScreeningReportId == id &&
                        r.PsychologistId ==
                            psychologistId.Value
                    );


            if (screeningReport == null)
            {
                return NotFound();
            }

            // Load all detailed screening source data for rich presentation
            var latestPHQ = await _context.PHQAssessments
                .Where(p => p.StudentId == screeningReport.StudentId)
                .OrderByDescending(p => p.AssessmentDate)
                .FirstOrDefaultAsync();

            var latestCSSRS = await _context.CSSRSAssessments
                .Where(c => c.StudentId == screeningReport.StudentId)
                .OrderByDescending(c => c.AssessmentDate)
                .FirstOrDefaultAsync();

            var latestFeeling = await _context.StudentSemesterRecords
                .Where(r => r.StudentId == screeningReport.StudentId)
                .OrderByDescending(r => r.UpdatedAt ?? r.SubmittedAt)
                .FirstOrDefaultAsync();

            var latestChatAssessment = await _context.ChatRiskAssessments
                .Where(r => r.StudentId == screeningReport.StudentId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            var latestVoiceBotReport = await _context.VoiceBotReports
                .Where(r => r.StudentId == screeningReport.StudentId)
                .OrderByDescending(r => r.LastUpdatedAt)
                .FirstOrDefaultAsync();

            ViewBag.LatestPHQ = latestPHQ;
            ViewBag.LatestCSSRS = latestCSSRS;
            ViewBag.LatestFeeling = latestFeeling;
            ViewBag.LatestChatAssessment = latestChatAssessment;
            ViewBag.LatestVoiceBotReport = latestVoiceBotReport;

            return View(
                screeningReport
            );
        }
        // =========================================================
        // OBSERVATION REPORTS
        // =========================================================

        // ================= Observation Reports =================

        [HttpGet]
        public async Task<IActionResult> ObservationReports()
        {
            // ================= Check Session =================
            var psychologistId = HttpContext.Session.GetInt32("PsychologistId");
            if (psychologistId == null)
            {
                return RedirectToAction("Login");
            }

            if (!_context.CounselingObservations.Any())
            {
                StudentMentalHealthMonitoringSystem.Data.DummyDataSeeder.SeedDummyData(_context);
            }

            // ================= Observation Reports =================
            var allReports = await _context.ObservationReports
                .Include(r => r.Student)
                .Include(r => r.Psychologist)
                .Include(r => r.RootCounseling)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();

            var userReports = allReports.Where(r => r.PsychologistId == psychologistId.Value).ToList();
            var observationReports = userReports.Any() ? userReports : allReports;

            return View(observationReports);
        }

        // ================= Observation Report Details =================

        [HttpGet]
        public async Task<IActionResult> ObservationReportDetails(int id)
        {
            // ================= Check Session =================
            var psychologistId = HttpContext.Session.GetInt32("PsychologistId");
            if (psychologistId == null)
            {
                return RedirectToAction("Login");
            }

            // ================= Get Observation Report =================
            var observationReport = await _context.ObservationReports
                .Include(r => r.Student)
                .Include(r => r.Psychologist)
                .Include(r => r.RootCounseling)
                .FirstOrDefaultAsync(r => r.ObservationReportId == id || r.StudentId == id || r.RootCounselingId == id);

            if (observationReport == null)
            {
                return NotFound();
            }


            // =====================================================
            // GET ALL COUNSELING OBSERVATIONS
            // FOR THIS COUNSELING CHAIN
            // =====================================================

            var observations =
                await _context.CounselingObservations

                    .Include(o =>
                        o.Counseling
                    )

                    .Include(o =>
                        o.Psychologist
                    )

                    .Where(o =>
                        o.RootCounselingId ==
                            observationReport.RootCounselingId
                    )

                    .OrderBy(o =>
                        o.Counseling!.CounselingDate
                    )

                    .ThenBy(o =>
                        o.Counseling!.AppointmentTime
                    )

                    .ToListAsync();


            ViewBag.Observations =
                observations;

            ViewBag.ProgressDetail =
                ProgressScoringService.BuildDetailViewModel(
                    observationReport,
                    observations
                );

            return View(
                observationReport
            );
        }
        // =========================================================
        // REPORTS
        // =========================================================

        public IActionResult Reports()
        {
            // ================= Check Session =================

            var psychologistId =
                HttpContext.Session.GetInt32(
                    "PsychologistId"
                );


            if (psychologistId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // ================= Own Counseling Records =================

            var reports =
                _context.Counselings
                    .Include(c => c.Student)
                    .Include(c => c.Psychologist)

                    .Where(c =>
                        c.PsychologistId ==
                            psychologistId.Value
                    )

                    .OrderByDescending(
                        c => c.CounselingDate
                    )

                    .ThenByDescending(
                        c => c.AppointmentTime
                    )

                    .ToList();


            return View(
                reports
            );
        }





        // =========================================================
        // COUNSELING HISTORY
        // =========================================================

        public IActionResult CounselingHistory(
            int id)
        {
            // ================= Check Session =================

            var psychologistId =
                HttpContext.Session.GetInt32(
                    "PsychologistId"
                );


            if (psychologistId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // ================= Get Student =================

            var student =
                _context.Students
                    .FirstOrDefault(
                        s => s.StudentId == id
                    );


            if (student == null)
            {
                return RedirectToAction(
                    "Students"
                );
            }


            // ================= Counseling History =================

            var history =
                _context.Counselings
                    .Where(c =>
                        c.StudentId == id
                    )
                    .Include(c =>
                        c.Psychologist
                    )
                    .OrderByDescending(
                        c => c.CounselingDate
                    )
                    .ThenByDescending(
                        c => c.AppointmentTime
                    )
                    .ToList();


            ViewBag.Student =
                student;


            return View(
                history
            );
        }


        // =========================================================
        // LOGOUT
        // =========================================================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();


            return RedirectToAction(
                "Login"
            );
        }


        // =========================================================
        // =========================================================
        // STUDENT PROGRESS & FOLLOW-UP REPORTS (PSYCHOLOGIST PATIENTS)
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> StudentProgressReports(string? followUpFilter)
        {
            var psychologistId = HttpContext.Session.GetInt32("PsychologistId");
            if (psychologistId == null)
            {
                return RedirectToAction("Login");
            }

            if (!_context.CounselingObservations.Any())
            {
                StudentMentalHealthMonitoringSystem.Data.DummyDataSeeder.SeedDummyData(_context);
            }

            var filter = string.IsNullOrWhiteSpace(followUpFilter) ? "All" : followUpFilter.Trim();

            var baseQuery = _context.ObservationReports
                .Include(r => r.Student)
                .Include(r => r.Psychologist)
                .AsQueryable();

            var psychReports = await baseQuery.Where(r => r.PsychologistId == psychologistId.Value).ToListAsync();
            var allReportsList = psychReports.Any() ? psychReports : await baseQuery.ToListAsync();

            if (filter == "InProgress")
            {
                allReportsList = allReportsList.Where(r => !r.IsFinal).ToList();
            }
            else if (filter == "Completed")
            {
                allReportsList = allReportsList.Where(r => r.IsFinal).ToList();
            }

            var summaryItems = new List<StudentProgressReportSummaryItem>();
            var processedRootIds = new HashSet<int>();

            foreach (var r in allReportsList)
            {
                processedRootIds.Add(r.RootCounselingId);

                var obsList = await _context.CounselingObservations
                    .Include(o => o.Counseling)
                    .Where(o => o.RootCounselingId == r.RootCounselingId)
                    .OrderBy(o => o.Counseling!.CounselingDate)
                    .ThenBy(o => o.Counseling!.AppointmentTime)
                    .ToListAsync();

                var detailVm = ProgressScoringService.BuildDetailViewModel(r, obsList);

                summaryItems.Add(new StudentProgressReportSummaryItem
                {
                    ObservationReportId = r.ObservationReportId,
                    RootCounselingId = r.RootCounselingId,
                    StudentId = r.StudentId,
                    StudentName = r.Student?.FullName ?? "Student",
                    StudentIdNumber = r.Student?.StudentIdNumber ?? "-",
                    Department = r.Student?.Department ?? "-",
                    Semester = r.Student?.Semester ?? "-",
                    ProfileImage = r.Student?.ProfileImage,
                    PsychologistId = r.PsychologistId,
                    PsychologistName = r.Psychologist?.FullName ?? "Psychologist",
                    IsFinal = r.IsFinal,
                    TotalSessions = detailVm.TotalSessions,
                    InitialScore = detailVm.InitialScore,
                    LatestScore = detailVm.LatestScore,
                    OverallImprovementStatus = detailVm.OverallImprovementStatus,
                    FirstSessionDate = detailVm.FirstSessionDate,
                    LatestSessionDate = detailVm.LatestSessionDate
                });
            }

            if (filter != "Completed")
            {
                var unmappedQuery = _context.Counselings
                    .Include(c => c.Student)
                    .Include(c => c.Psychologist)
                    .Where(c => c.StudentId > 0);

                var psychCounselings = await unmappedQuery.Where(c => c.PsychologistId == psychologistId.Value).ToListAsync();
                var unmappedCounselings = psychCounselings.Any() ? psychCounselings : await unmappedQuery.ToListAsync();

                var groupedByStudent = unmappedCounselings
                    .Where(c => !processedRootIds.Contains(c.CounselingId))
                    .GroupBy(c => c.StudentId);

                foreach (var group in groupedByStudent)
                {
                    var studentCounselings = group.OrderBy(c => c.CounselingDate).ThenBy(c => c.AppointmentTime).ToList();
                    var firstC = studentCounselings.First();
                    var lastC = studentCounselings.Last();
                    int rootId = firstC.CounselingId;

                    summaryItems.Add(new StudentProgressReportSummaryItem
                    {
                        ObservationReportId = 0,
                        RootCounselingId = rootId,
                        StudentId = firstC.StudentId,
                        StudentName = firstC.Student?.FullName ?? "Student",
                        StudentIdNumber = firstC.Student?.StudentIdNumber ?? "-",
                        Department = firstC.Student?.Department ?? "-",
                        Semester = firstC.Student?.Semester ?? "-",
                        ProfileImage = firstC.Student?.ProfileImage,
                        PsychologistId = firstC.PsychologistId,
                        PsychologistName = firstC.Psychologist?.FullName ?? "Psychologist",
                        IsFinal = false,
                        TotalSessions = studentCounselings.Count,
                        InitialScore = 50.0,
                        LatestScore = 50.0,
                        OverallImprovementStatus = "Stable",
                        FirstSessionDate = firstC.CounselingDate,
                        LatestSessionDate = lastC.CounselingDate
                    });
                }
            }

            var model = new StudentProgressReportListViewModel
            {
                FollowUpFilter = filter,
                DepartmentFilter = "All",
                Reports = summaryItems.OrderByDescending(x => x.LatestSessionDate).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> StudentProgressDetails(int id)
        {
            var psychologistId = HttpContext.Session.GetInt32("PsychologistId");
            if (psychologistId == null)
            {
                return RedirectToAction("Login");
            }

            var report = await _context.ObservationReports
                .Include(r => r.Student)
                .Include(r => r.Psychologist)
                .FirstOrDefaultAsync(r => r.ObservationReportId == id || r.StudentId == id || r.RootCounselingId == id);

            if (report == null)
            {
                // Fallback for direct student lookup
                var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == id);
                var counselings = await _context.Counselings
                    .Include(c => c.Psychologist)
                    .Where(c => c.StudentId == id)
                    .OrderBy(c => c.CounselingDate)
                    .ThenBy(c => c.AppointmentTime)
                    .ToListAsync();

                if (student == null || !counselings.Any())
                {
                    return NotFound();
                }

                var dummyReport = new ObservationReport
                {
                    ObservationReportId = 0,
                    RootCounselingId = counselings.First().CounselingId,
                    StudentId = student.StudentId,
                    Student = student,
                    PsychologistId = psychologistId.Value,
                    Psychologist = counselings.First().Psychologist,
                    IsFinal = false,
                    CreatedAt = counselings.First().CounselingDate,
                    UpdatedAt = counselings.Last().CounselingDate
                };

                var dummyObsList = await _context.CounselingObservations
                    .Include(o => o.Counseling)
                    .Where(o => o.StudentId == student.StudentId)
                    .OrderBy(o => o.Counseling!.CounselingDate)
                    .ThenBy(o => o.Counseling!.AppointmentTime)
                    .ToListAsync();

                var fallbackModel = ProgressScoringService.BuildDetailViewModel(dummyReport, dummyObsList);
                return View(fallbackModel);
            }

            var obsList = await _context.CounselingObservations
                .Include(o => o.Counseling)
                .Where(o => o.RootCounselingId == report.RootCounselingId)
                .OrderBy(o => o.Counseling!.CounselingDate)
                .ThenBy(o => o.Counseling!.AppointmentTime)
                .ToListAsync();

            var model = ProgressScoringService.BuildDetailViewModel(report, obsList);
            return View(model);
        }

        // =========================================================
        // PSYCHOLOGIST PROFILE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var psychologistId = HttpContext.Session.GetInt32("PsychologistId");
            if (psychologistId == null)
            {
                return RedirectToAction("Login");
            }

            var psychologist = await _context.Psychologists
                .FirstOrDefaultAsync(p => p.PsychologistId == psychologistId.Value);

            if (psychologist == null)
            {
                return RedirectToAction("Login");
            }

            // Statistics for Psychologist Profile
            ViewBag.TotalCounselings = await _context.Counselings
                .CountAsync(c => c.PsychologistId == psychologistId.Value);

            ViewBag.CompletedCounselings = await _context.Counselings
                .CountAsync(c => c.PsychologistId == psychologistId.Value && c.Status == "Completed");

            ViewBag.UpcomingCounselings = await _context.Counselings
                .CountAsync(c => c.PsychologistId == psychologistId.Value && c.Status == "Scheduled");

            ViewBag.ActivePatients = await _context.Counselings
                .Where(c => c.PsychologistId == psychologistId.Value)
                .Select(c => c.StudentId)
                .Distinct()
                .CountAsync();

            ViewBag.TotalObservationReports = await _context.ObservationReports
                .CountAsync(o => o.PsychologistId == psychologistId.Value);

            return View(psychologist);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Psychologist model)
        {
            var psychologistId = HttpContext.Session.GetInt32("PsychologistId");
            if (psychologistId == null)
            {
                return RedirectToAction("Login");
            }

            var psychologist = await _context.Psychologists
                .FirstOrDefaultAsync(p => p.PsychologistId == psychologistId.Value);

            if (psychologist == null)
            {
                return RedirectToAction("Login");
            }

            // Validate non-password fields
            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                ModelState.AddModelError("FullName", "Full Name is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Phone))
            {
                ModelState.AddModelError("Phone", "Phone number is required.");
            }

            // Update image if uploaded
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(model.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ImageFile", "Only JPG, JPEG, PNG, and WEBP images are allowed.");
                }
                else
                {
                    var uploadFolder = Path.Combine(_environment.WebRootPath, "images", "psychologists");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ImageFile.FileName)}";
                    var filePath = Path.Combine(uploadFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }

                    psychologist.ProfileImage = $"/images/psychologists/{uniqueFileName}";
                }
            }

            if (!ModelState.IsValid)
            {
                // Re-calculate statistics for the view
                ViewBag.TotalCounselings = await _context.Counselings
                    .CountAsync(c => c.PsychologistId == psychologistId.Value);
                ViewBag.CompletedCounselings = await _context.Counselings
                    .CountAsync(c => c.PsychologistId == psychologistId.Value && c.Status == "Completed");
                ViewBag.UpcomingCounselings = await _context.Counselings
                    .CountAsync(c => c.PsychologistId == psychologistId.Value && c.Status == "Scheduled");
                ViewBag.ActivePatients = await _context.Counselings
                    .Where(c => c.PsychologistId == psychologistId.Value)
                    .Select(c => c.StudentId)
                    .Distinct()
                    .CountAsync();
                ViewBag.TotalObservationReports = await _context.ObservationReports
                    .CountAsync(o => o.PsychologistId == psychologistId.Value);

                return View(psychologist);
            }

            // Update properties
            psychologist.FullName = model.FullName.Trim();
            psychologist.Phone = model.Phone.Trim();
            psychologist.Specialization = model.Specialization?.Trim();
            psychologist.Qualification = model.Qualification?.Trim();
            psychologist.Experience = model.Experience;

            // Optional password update
            if (!string.IsNullOrWhiteSpace(model.Password) && model.Password.Length >= 8)
            {
                psychologist.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.SetString("PsychologistName", psychologist.FullName);
            TempData["Success"] = "Profile updated successfully!";

            return RedirectToAction("Profile");
        }
    }
}