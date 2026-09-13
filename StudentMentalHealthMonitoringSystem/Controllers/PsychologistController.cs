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
        private readonly EmailService _emailService;

        public PsychologistController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            CounselingSchedulerService counselingSchedulerService,
            EmailService emailService)
        {
            _context = context;
            _environment = environment;
            _counselingSchedulerService = counselingSchedulerService;
            _emailService = emailService;
        }


        // =========================================================
        // REGISTER
        // =========================================================

        // ================= Register GET (Disabled - Admin Only) =================

        [HttpGet]
        public IActionResult Register()
        {
            TempData["Error"] = "Psychologist registration is managed by System Administrators only.";
            return RedirectToAction("Login");
        }


        // ================= Register POST (Disabled - Admin Only) =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(
            Psychologist psychologist)
        {
            TempData["Error"] = "Psychologist registration is managed by System Administrators only.";
            return RedirectToAction("Login");
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


            // ================= Check Account Suspension =================
            if (psychologist.IsSuspended)
            {
                ViewBag.SuspendedError = true;
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
        // FORGOT PASSWORD
        // =========================================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (HttpContext.Session.GetInt32("PsychologistId") != null)
            {
                return RedirectToAction("Dashboard");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please enter your registered email address.";
                return View();
            }

            email = email.Trim().ToLowerInvariant();

            var psychologist = await _context.Psychologists
                .FirstOrDefaultAsync(p => p.Email.ToLower() == email);

            if (psychologist == null)
            {
                ViewBag.Error = "No psychologist account was found with this email address.";
                return View();
            }

            var otp = Random.Shared.Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);

            HttpContext.Session.SetString("Psychologist_Reset_Email", psychologist.Email);
            HttpContext.Session.SetString("Psychologist_Reset_Otp", otp);
            HttpContext.Session.SetString("Psychologist_Reset_Expiry", expiry.ToString("o"));

            try
            {
                await _emailService.SendPasswordResetOtpAsync(psychologist.Email, psychologist.FullName, otp, "Psychologist");
                TempData["SuccessMessage"] = "A 6-digit verification code (OTP) has been sent to your email. Please check your inbox.";
                return RedirectToAction("ResetPassword");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Failed to send email OTP: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            var resetEmail = HttpContext.Session.GetString("Psychologist_Reset_Email");
            if (string.IsNullOrWhiteSpace(resetEmail))
            {
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.Email = resetEmail;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string otp, string newPassword, string confirmPassword)
        {
            var resetEmail = HttpContext.Session.GetString("Psychologist_Reset_Email");
            var sessionOtp = HttpContext.Session.GetString("Psychologist_Reset_Otp");
            var expiryStr = HttpContext.Session.GetString("Psychologist_Reset_Expiry");

            if (string.IsNullOrWhiteSpace(resetEmail) || string.IsNullOrWhiteSpace(sessionOtp))
            {
                TempData["ErrorMessage"] = "Password reset session has expired. Please request a new code.";
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.Email = resetEmail;

            if (string.IsNullOrWhiteSpace(otp))
            {
                ViewBag.Error = "Please enter the 6-digit OTP code.";
                return View();
            }

            if (DateTime.TryParse(expiryStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiryTime))
            {
                if (DateTime.UtcNow > expiryTime)
                {
                    ViewBag.Error = "The verification code has expired. Please request a new code.";
                    return View();
                }
            }

            if (otp.Trim() != sessionOtp.Trim())
            {
                ViewBag.Error = "Invalid verification code. Please check and try again.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                ViewBag.Error = "Password must be at least 8 characters long.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(newPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$"))
            {
                ViewBag.Error = "Password must contain at least 1 uppercase letter, 1 lowercase letter, 1 number, and 1 special character.";
                return View();
            }

            var psychologist = await _context.Psychologists
                .FirstOrDefaultAsync(p => p.Email.ToLower() == resetEmail.ToLower());

            if (psychologist == null)
            {
                ViewBag.Error = "Psychologist account not found.";
                return View();
            }

            psychologist.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            // Clear reset session
            HttpContext.Session.Remove("Psychologist_Reset_Email");
            HttpContext.Session.Remove("Psychologist_Reset_Otp");
            HttpContext.Session.Remove("Psychologist_Reset_Expiry");

            TempData["SuccessMessage"] = "Your password has been successfully reset! Please log in with your new password.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendResetOtp()
        {
            var resetEmail = HttpContext.Session.GetString("Psychologist_Reset_Email");
            if (string.IsNullOrWhiteSpace(resetEmail))
            {
                return Json(new { success = false, message = "Session expired. Please start over." });
            }

            var psychologist = await _context.Psychologists
                .FirstOrDefaultAsync(p => p.Email.ToLower() == resetEmail.ToLower());

            if (psychologist == null)
            {
                return Json(new { success = false, message = "Psychologist account not found." });
            }

            var otp = Random.Shared.Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);

            HttpContext.Session.SetString("Psychologist_Reset_Otp", otp);
            HttpContext.Session.SetString("Psychologist_Reset_Expiry", expiry.ToString("o"));

            try
            {
                await _emailService.SendPasswordResetOtpAsync(psychologist.Email, psychologist.FullName, otp, "Psychologist");
                return Json(new { success = true, message = "A new verification code has been sent to your email." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to send email: {ex.Message}" });
            }
        }


        // =========================================================
        // DASHBOARD
        // =========================================================

        public async Task<IActionResult> Dashboard()
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

            await CounselingSchedulerService.UpdateMissedAppointmentsAsync(_context);

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

        public async Task<IActionResult> Students()
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

            await CounselingSchedulerService.UpdateMissedAppointmentsAsync(_context);


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

            if (student.IsSuspended)
            {
                TempData["Error"] = $"Student {student.FullName} is suspended. Counseling appointments cannot be scheduled for suspended students.";
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

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == counseling.StudentId);
            if (student == null || student.IsSuspended)
            {
                TempData["Error"] = $"Student {(student?.FullName ?? "record")} is suspended and cannot be scheduled for counseling appointments.";
                return RedirectToAction(
                    "Students"
                );
            }


            // ================= Validation =================

            if (!ModelState.IsValid)
            {
                ViewBag.Student = student;


                return View(counseling);
            }


            // ================= Set Values =================

            counseling.PsychologistId =
                psychologistId.Value;


            if (counseling.CounselingDate == default)
            {
                counseling.CounselingDate =
                    DateTime.Now;
            }

            if (string.IsNullOrWhiteSpace(counseling.AppointmentRoom))
            {
                counseling.AppointmentRoom =
                    "Mental Health & Counseling Center, Room 402";
            }

            if (counseling.AppointmentEndTime == default && counseling.AppointmentTime != default)
            {
                counseling.AppointmentEndTime =
                    counseling.AppointmentTime.Add(TimeSpan.FromHours(1));
            }


            // ================= Save =================

            _context.Counselings.Add(
                counseling
            );


            await _context.SaveChangesAsync();


            // ================= Send Confirmation Email =================

            try
            {
                var psychologist = await _context.Psychologists
                    .FirstOrDefaultAsync(p => p.PsychologistId == psychologistId.Value);

                if (student != null && !student.IsSuspended && !string.IsNullOrWhiteSpace(student.Email) && psychologist != null)
                {
                    var targetDate = counseling.NextFollowUpDate ?? counseling.CounselingDate;
                    var startTime = counseling.AppointmentTime != default ? counseling.AppointmentTime : new TimeSpan(10, 0, 0);
                    var endTime = counseling.AppointmentEndTime != default ? counseling.AppointmentEndTime : startTime.Add(TimeSpan.FromHours(1));

                    await _emailService.SendAppointmentConfirmationEmailAsync(
                        recipientEmail: student.Email,
                        studentName: student.FullName,
                        studentIdNumber: student.StudentIdNumber,
                        psychologistName: psychologist.FullName,
                        psychologistSpecialization: psychologist.Specialization,
                        appointmentDate: targetDate,
                        startTime: startTime,
                        endTime: endTime,
                        appointmentRoom: counseling.AppointmentRoom,
                        appointmentSource: "PsychologistDirect",
                        severityOrReason: string.IsNullOrWhiteSpace(counseling.RiskLevel) ? "Psychologist Session Consultation" : $"Clinical Risk Assessment ({counseling.RiskLevel})"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PsychologistController] Failed to send counseling email: {ex.Message}");
            }


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

            // Automatically transition any expired unassessed appointments to Missed
            await CounselingSchedulerService.UpdateMissedAppointmentsAsync(_context);

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

        // =========================================================
        // CANCEL APPOINTMENT (PSYCHOLOGIST)
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(
            int id, 
            string? reason, 
            DateTime? nextDate, 
            TimeSpan? nextTime, 
            string? nextRoom,
            string? returnUrl)
        {
            var psychologistId = HttpContext.Session.GetInt32("PsychologistId");
            if (psychologistId == null)
            {
                return RedirectToAction("Login");
            }

            // Run automated missed appointments check first
            await CounselingSchedulerService.UpdateMissedAppointmentsAsync(_context);

            var counseling = await _context.Counselings
                .Include(c => c.Student)
                .Include(c => c.Psychologist)
                .FirstOrDefaultAsync(c => c.CounselingId == id && c.PsychologistId == psychologistId.Value);

            if (counseling == null)
            {
                TempData["Error"] = "Appointment not found.";
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
            }

            if (counseling.Status == "Cancelled")
            {
                TempData["Error"] = "This appointment has already been cancelled.";
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
            }

            if (counseling.Status == "Completed")
            {
                TempData["Error"] = "Completed sessions cannot be cancelled.";
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
            }

            // Psychologist can cancel the appointment strictly before the scheduled date and time
            var now = DateTime.Now;
            bool isBeforeAppointment = counseling.CounselingDate.Date > DateTime.Today ||
                (counseling.CounselingDate.Date == DateTime.Today && counseling.AppointmentTime > now.TimeOfDay);

            if (!isBeforeAppointment)
            {
                // Date has arrived/passed, so it cannot be cancelled
                if (counseling.Status != "Completed")
                {
                    counseling.Status = "Missed";
                    await _context.SaveChangesAsync();
                }
                TempData["Error"] = "Appointments can only be cancelled before the scheduled date and time.";
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
            }

            // Update status to Cancelled
            counseling.Status = "Cancelled";
            string cancellationNote = string.IsNullOrWhiteSpace(reason)
                ? $"Cancelled by psychologist ({counseling.Psychologist?.FullName}) on {DateTime.Now:MMM dd, yyyy h:mm tt}."
                : $"Cancelled by psychologist ({counseling.Psychologist?.FullName}) on {DateTime.Now:MMM dd, yyyy h:mm tt}. Reason: {reason.Trim()}";

            counseling.Observation = string.IsNullOrWhiteSpace(counseling.Observation)
                ? cancellationNote
                : $"{counseling.Observation} | {cancellationNote}";

            await _context.SaveChangesAsync();

            // Send cancellation notification email to student
            try
            {
                if (counseling.Student != null && !counseling.Student.IsSuspended && !string.IsNullOrWhiteSpace(counseling.Student.Email))
                {
                    await _emailService.SendAppointmentCancellationEmailAsync(
                        recipientEmail: counseling.Student.Email,
                        recipientName: counseling.Student.FullName,
                        otherPartyName: counseling.Psychologist?.FullName ?? "University Psychologist",
                        appointmentDate: counseling.CounselingDate,
                        startTime: counseling.AppointmentTime,
                        endTime: counseling.AppointmentEndTime,
                        appointmentRoom: counseling.AppointmentRoom,
                        cancelledBy: $"psychologist ({counseling.Psychologist?.FullName ?? "Psychologist"})",
                        cancellationReason: reason
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PsychologistController] Failed to send cancellation email: {ex.Message}");
            }

            // Check if immediate next appointment is provided
            if (nextDate.HasValue && nextTime.HasValue)
            {
                var studentCheck = counseling.Student ?? await _context.Students.FindAsync(counseling.StudentId);
                if (studentCheck != null && studentCheck.IsSuspended)
                {
                    TempData["Success"] = $"Appointment was cancelled. However, next session cannot be scheduled because {studentCheck.FullName} is suspended.";
                    return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
                }

                if (nextDate.Value.DayOfWeek == DayOfWeek.Thursday || nextDate.Value.DayOfWeek == DayOfWeek.Friday)
                {
                    TempData["Success"] = "Appointment cancelled. However, next appointment date must be Saturday to Wednesday. Please schedule next appointment from the list.";
                    return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
                }

                if (nextDate.Value.Date < DateTime.Today)
                {
                    TempData["Success"] = "Appointment cancelled. Please select a valid future date to schedule the next appointment.";
                    return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
                }

                int assignedPsychId = counseling.PsychologistId;
                var currentPsych = await _context.Psychologists.FindAsync(assignedPsychId);
                if (currentPsych == null || currentPsych.IsSuspended)
                {
                    var firstActive = await _context.Psychologists.FirstOrDefaultAsync(p => !p.IsSuspended);
                    assignedPsychId = firstActive?.PsychologistId ?? 0;
                }

                var newCounseling = new Counseling
                {
                    StudentId = counseling.StudentId,
                    PsychologistId = assignedPsychId,
                    CounselingDate = nextDate.Value.Date,
                    AppointmentTime = nextTime.Value,
                    AppointmentEndTime = nextTime.Value.Add(TimeSpan.FromHours(1)),
                    Status = "Confirmed",
                    RiskLevel = counseling.RiskLevel,
                    AppointmentRoom = !string.IsNullOrWhiteSpace(nextRoom) ? nextRoom.Trim() : (counseling.AppointmentRoom ?? "Mental Health & Counseling Center, Room 402"),
                    AppointmentSource = "Rescheduled",
                    TriggerSource = "Psychologist Reschedule",
                    TriggerSeverity = counseling.TriggerSeverity ?? counseling.RiskLevel,
                    CreatedAt = DateTime.Now
                };

                _context.Counselings.Add(newCounseling);
                await _context.SaveChangesAsync();

                try
                {
                    if (counseling.Student != null && !counseling.Student.IsSuspended && !string.IsNullOrWhiteSpace(counseling.Student.Email))
                    {
                        await _emailService.SendAppointmentConfirmationEmailAsync(
                            recipientEmail: counseling.Student.Email,
                            studentName: counseling.Student.FullName,
                            studentIdNumber: counseling.Student.StudentIdNumber,
                            psychologistName: counseling.Psychologist?.FullName ?? "University Psychologist",
                            psychologistSpecialization: counseling.Psychologist?.Specialization,
                            appointmentDate: newCounseling.CounselingDate,
                            startTime: newCounseling.AppointmentTime,
                            endTime: newCounseling.AppointmentEndTime,
                            appointmentRoom: newCounseling.AppointmentRoom,
                            appointmentSource: "Rescheduled",
                            severityOrReason: "Rescheduled Counseling Session"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PsychologistController] Failed to send rescheduled email: {ex.Message}");
                }

                TempData["Success"] = $"Appointment cancelled and next session scheduled for {nextDate.Value:MMM dd, yyyy} at {DateTime.Today.Add(nextTime.Value):h:mm tt} successfully.";
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
            }

            TempData["Success"] = "Appointment cancelled successfully. You can schedule the next appointment date anytime.";
            return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
        }

        // =========================================================
        // SCHEDULE NEXT APPOINTMENT (FROM CANCELLED STATE OR DIRECT)
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleNextAppointment(
            int studentId,
            DateTime appointmentDate,
            TimeSpan appointmentTime,
            string? appointmentRoom,
            int? previousCounselingId,
            string? returnUrl)
        {
            var psychologistId = HttpContext.Session.GetInt32("PsychologistId");
            if (psychologistId == null)
            {
                return RedirectToAction("Login");
            }

            if (appointmentDate.DayOfWeek == DayOfWeek.Thursday || appointmentDate.DayOfWeek == DayOfWeek.Friday)
            {
                TempData["Error"] = "Counseling appointments can only be scheduled from Saturday to Wednesday.";
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
            }

            if (appointmentDate.Date < DateTime.Today || (appointmentDate.Date == DateTime.Today && appointmentTime <= DateTime.Now.TimeOfDay))
            {
                TempData["Error"] = "Please select a future appointment date and time.";
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null)
            {
                TempData["Error"] = "Student not found.";
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
            }

            if (student.IsSuspended)
            {
                TempData["Error"] = $"Student {student.FullName} is suspended. Counseling appointments cannot be scheduled for suspended students.";
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
            }

            var psychologist = await _context.Psychologists.FirstOrDefaultAsync(p => p.PsychologistId == psychologistId.Value && !p.IsSuspended);
            if (psychologist == null)
            {
                TempData["Error"] = "Your psychologist account is suspended or not found.";
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
            }

            // Check if active uncompleted session exists
            var existingActive = await _context.Counselings
                .FirstOrDefaultAsync(c => c.StudentId == studentId &&
                                          (c.Status == "Confirmed" || c.Status == "Pending") &&
                                          c.CounselingDate.Date >= DateTime.Today);

            if (existingActive != null)
            {
                TempData["Error"] = $"This student already has an active scheduled appointment on {existingActive.CounselingDate:MMM dd, yyyy}.";
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
            }

            var prevCounseling = previousCounselingId.HasValue 
                ? await _context.Counselings.FirstOrDefaultAsync(c => c.CounselingId == previousCounselingId.Value) 
                : null;

            var newCounseling = new Counseling
            {
                StudentId = studentId,
                PsychologistId = psychologistId.Value,
                CounselingDate = appointmentDate.Date,
                AppointmentTime = appointmentTime,
                AppointmentEndTime = appointmentTime.Add(TimeSpan.FromHours(1)),
                Status = "Confirmed",
                RiskLevel = prevCounseling?.RiskLevel ?? "Moderate",
                AppointmentRoom = !string.IsNullOrWhiteSpace(appointmentRoom) ? appointmentRoom.Trim() : "Mental Health & Counseling Center, Room 402",
                AppointmentSource = "Rescheduled",
                TriggerSource = "Psychologist Next Appointment",
                TriggerSeverity = prevCounseling?.TriggerSeverity ?? prevCounseling?.RiskLevel ?? "Moderate",
                ParentCounselingId = prevCounseling?.CounselingId,
                CreatedAt = DateTime.Now
            };

            _context.Counselings.Add(newCounseling);
            await _context.SaveChangesAsync();

            // Send confirmation email
            try
            {
                if (!student.IsSuspended && !string.IsNullOrWhiteSpace(student.Email))
                {
                    await _emailService.SendAppointmentConfirmationEmailAsync(
                        recipientEmail: student.Email,
                        studentName: student.FullName,
                        studentIdNumber: student.StudentIdNumber,
                        psychologistName: psychologist?.FullName ?? "University Psychologist",
                        psychologistSpecialization: psychologist?.Specialization,
                        appointmentDate: newCounseling.CounselingDate,
                        startTime: newCounseling.AppointmentTime,
                        endTime: newCounseling.AppointmentEndTime,
                        appointmentRoom: newCounseling.AppointmentRoom,
                        appointmentSource: "NextAppointment",
                        severityOrReason: "Next Counseling Session"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PsychologistController] Failed to send next appointment email: {ex.Message}");
            }

            TempData["Success"] = $"Next counseling appointment for {student.FullName} scheduled for {appointmentDate:MMM dd, yyyy} at {DateTime.Today.Add(appointmentTime):h:mm tt} successfully.";
            return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Appointment")! : returnUrl);
        }

        // =========================================================
        // SCHEDULE FOLLOW-UP APPOINTMENT (FROM APPOINTMENTS PAGE)
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleFollowUp(int counselingId, DateTime followUpDate, TimeSpan followUpTime)
        {
            var psychologistId = HttpContext.Session.GetInt32("PsychologistId");
            if (psychologistId == null)
            {
                return RedirectToAction("Login");
            }

            var currentPsychCheck = await _context.Psychologists.FirstOrDefaultAsync(p => p.PsychologistId == psychologistId.Value && !p.IsSuspended);
            if (currentPsychCheck == null)
            {
                TempData["Error"] = "Your psychologist account is suspended or not found.";
                return RedirectToAction("Appointment");
            }

            var counseling = await _context.Counselings
                .Include(c => c.Student)
                .Include(c => c.Psychologist)
                .FirstOrDefaultAsync(c => c.CounselingId == counselingId && c.PsychologistId == psychologistId.Value);

            if (counseling == null)
            {
                TempData["Error"] = "Counseling session not found.";
                return RedirectToAction("Appointment");
            }

            if (counseling.Student != null && counseling.Student.IsSuspended)
            {
                TempData["Error"] = $"Student {counseling.Student.FullName} is suspended and cannot be scheduled for follow-up appointments.";
                return RedirectToAction("Appointment");
            }

            // Enforce that follow-up appointments can only be scheduled for completed sessions
            if (counseling.Status != "Completed")
            {
                TempData["Error"] = "Follow-up appointments can only be scheduled after the counseling session is completed.";
                return RedirectToAction("Appointment");
            }

            // Overwrite Protection: Prevent scheduling another follow-up if an active one already exists
            var existingFollowUp = await _context.Counselings
                .FirstOrDefaultAsync(c => c.ParentCounselingId == counselingId && c.Status != "Cancelled");

            if (existingFollowUp != null || (counseling.NextFollowUpDate.HasValue && counseling.NextFollowUpDate.Value.Date >= DateTime.Today))
            {
                TempData["Error"] = "An active follow-up appointment is already scheduled. You cannot overwrite it. Please cancel the existing follow-up first to schedule a new one.";
                return RedirectToAction("Appointment");
            }

            // Date validation (Saturday to Wednesday only)
            if (followUpDate.Date < DateTime.Today)
            {
                TempData["Error"] = "Follow-up date cannot be in the past.";
                return RedirectToAction("Appointment");
            }

            if (followUpDate.DayOfWeek == DayOfWeek.Thursday || followUpDate.DayOfWeek == DayOfWeek.Friday)
            {
                TempData["Error"] = "Follow-up appointments can only be scheduled from Saturday to Wednesday (Thursday & Friday are weekend).";
                return RedirectToAction("Appointment");
            }

            var followUpResult = await _counselingSchedulerService.CreateFollowUpAppointmentAsync(
                counseling,
                followUpDate.Date,
                followUpTime
            );

            if (!followUpResult.Success)
            {
                TempData["Error"] = followUpResult.Message;
                return RedirectToAction("Appointment");
            }

            // Update parent counseling session tracking
            counseling.NextFollowUpDate = followUpDate.Date;
            counseling.NextFollowUpTime = followUpTime;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Follow-up appointment for {counseling.Student?.FullName} scheduled on {followUpDate:dd MMM yyyy} at {DateTime.Today.Add(followUpTime):h:mm tt}.";
            return RedirectToAction("Appointment");
        }

        // =========================================================
        // CANCEL FOLLOW-UP APPOINTMENT (FROM APPOINTMENTS PAGE)
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelFollowUp(int counselingId, string? reason)
        {
            var psychologistId = HttpContext.Session.GetInt32("PsychologistId");
            if (psychologistId == null)
            {
                return RedirectToAction("Login");
            }

            var counseling = await _context.Counselings
                .Include(c => c.Student)
                .Include(c => c.Psychologist)
                .FirstOrDefaultAsync(c => c.CounselingId == counselingId && c.PsychologistId == psychologistId.Value);

            if (counseling == null)
            {
                TempData["Error"] = "Counseling session not found.";
                return RedirectToAction("Appointment");
            }

            // Find all active follow-up child sessions
            var childFollowUps = await _context.Counselings
                .Where(c => c.ParentCounselingId == counselingId && c.Status != "Cancelled")
                .ToListAsync();

            string cancelMsg = string.IsNullOrWhiteSpace(reason)
                ? $"Follow-up cancelled by psychologist on {DateTime.Now:MMM dd, yyyy h:mm tt}."
                : $"Follow-up cancelled by psychologist on {DateTime.Now:MMM dd, yyyy h:mm tt}. Reason: {reason.Trim()}";

            foreach (var child in childFollowUps)
            {
                child.Status = "Cancelled";
                child.Observation = string.IsNullOrWhiteSpace(child.Observation)
                    ? cancelMsg
                    : $"{child.Observation} | {cancelMsg}";
            }

            // Clear parent record's next follow-up date and time
            counseling.NextFollowUpDate = null;
            counseling.NextFollowUpTime = null;
            await _context.SaveChangesAsync();

            // Send notification email to student
            if (counseling.Student != null && !counseling.Student.IsSuspended && !string.IsNullOrWhiteSpace(counseling.Student.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        counseling.Student.Email,
                        "Follow-up Counseling Session Cancelled - Student Mental Health Monitoring System",
                        $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px; line-height: 1.6;'>
                            <h2 style='color: #842029;'>Follow-up Counseling Session Cancelled</h2>
                            <p>Dear <strong>{counseling.Student.FullName}</strong>,</p>
                            <p>Your upcoming follow-up counseling session with Psychologist <strong>{counseling.Psychologist?.FullName}</strong> has been cancelled.</p>
                            {(string.IsNullOrWhiteSpace(reason) ? "" : $"<p><strong>Reason:</strong> {reason.Trim()}</p>")}
                            <p>If you need further counseling support, you may request a new appointment from your student portal.</p>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                            <small style='color: #888;'>Student Mental Health Monitoring System</small>
                        </div>"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PsychologistController] Failed to send follow-up cancellation email: {ex.Message}");
                }
            }

            TempData["Success"] = $"Follow-up appointment for {counseling.Student?.FullName} has been cancelled successfully. You can now schedule a new follow-up whenever needed.";
            return RedirectToAction("Appointment");
        }


        // =========================================================
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

            // Automatically transition any expired unassessed appointments to Missed
            await CounselingSchedulerService.UpdateMissedAppointmentsAsync(_context, _emailService);

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

            var now = DateTime.Now;
            DateTime sessionStartDateTime = counseling.CounselingDate.Date.Add(counseling.AppointmentTime);
            bool isSessionStarted = now >= sessionStartDateTime;
            ViewBag.IsSessionStarted = isSessionStarted;
            ViewBag.SessionStartDateTime = sessionStartDateTime;

            bool canCancel = (counseling.Status == "Confirmed" || counseling.Status == "Pending") &&
                (counseling.CounselingDate.Date > DateTime.Today ||
                (counseling.CounselingDate.Date == DateTime.Today && counseling.AppointmentTime > now.TimeOfDay));
            ViewBag.CanCancel = canCancel;


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


            await CounselingSchedulerService.UpdateMissedAppointmentsAsync(_context, _emailService);

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
            // HANDLE SESSION MISSED (YES OPTION)
            // =====================================================
            if (completionType == "MarkMissed" || model.IsSessionMissed == true)
            {
                if (counseling.Status == "Completed")
                {
                    TempData["Error"] = "Completed counseling sessions cannot be marked as missed.";
                    return RedirectToAction("CounselingDetails", new { id = counseling.CounselingId });
                }

                if (counseling.Status == "Cancelled")
                {
                    TempData["Error"] = "Cancelled counseling sessions cannot be marked as missed.";
                    return RedirectToAction("CounselingDetails", new { id = counseling.CounselingId });
                }

                // Process missed appointment strikes & send warning/suspension emails
                await CounselingSchedulerService.ProcessMissedAppointmentStrikeAsync(counseling, _context, _emailService);

                if (!string.IsNullOrWhiteSpace(model.MissedReason))
                {
                    counseling.Observation = model.MissedReason.Trim();
                }

                // If psychologist also filled next appointment date & time for missed student
                if (model.MissedNextDate.HasValue && model.MissedNextTime.HasValue)
                {
                    var missedStudent = counseling.Student ?? await _context.Students.FindAsync(counseling.StudentId);
                    if (missedStudent != null && !missedStudent.IsSuspended)
                    {
                        var existingChild = await _context.Counselings
                            .FirstOrDefaultAsync(c => c.ParentCounselingId == counseling.CounselingId && c.Status != "Cancelled");
                        if (existingChild == null)
                        {
                            await _counselingSchedulerService.CreateFollowUpAppointmentAsync(
                                counseling,
                                model.MissedNextDate.Value.Date,
                                model.MissedNextTime.Value
                            );
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Session has been marked as Missed for {counseling.Student?.FullName ?? "the student"}. Missed count and notifications have been processed.";
                return RedirectToAction("Appointment");
            }

            if (counseling.Status == "Missed")
            {
                TempData["Error"] = "Observation forms and clinical notes cannot be submitted for a missed appointment. Please schedule a new appointment for the student below.";
                return RedirectToAction("CounselingDetails", new { id = counseling.CounselingId });
            }

            if (counseling.Status == "Cancelled")
            {
                TempData["Error"] = "Observation forms and clinical notes cannot be submitted for a cancelled appointment. You can schedule a new appointment if needed.";
                return RedirectToAction("CounselingDetails", new { id = counseling.CounselingId });
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
            // CHECK IF UPDATE OR CREATE
            // =====================================================

            bool isUpdate = await _context.CounselingObservations.AnyAsync(o => o.CounselingId == counseling.CounselingId);

            if (completionType == "NoFollowUp" || completionType == "WithoutFollowUp")
            {
                model.FollowUpRequired = false;
                model.NextFollowUpDate = null;
                model.NextFollowUpTime = null;
            }
            else if (completionType == "WithFollowUp")
            {
                model.FollowUpRequired = model.NextFollowUpDate.HasValue;
            }
            else
            {
                model.FollowUpRequired = model.NextFollowUpDate.HasValue;
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


            var studentCheck = counseling.Student ?? await _context.Students.FindAsync(counseling.StudentId);
            if (studentCheck != null && studentCheck.IsSuspended)
            {
                model.NextFollowUpDate = null;
                model.NextFollowUpTime = null;
                model.FollowUpRequired = false;
            }

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


            observationReport.IsFinal = true;
            observationReport.FinalizedAt = DateTime.Now;
            observationReport.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            if (model.NextFollowUpDate.HasValue && model.NextFollowUpTime.HasValue)
            {
                var existingChild = await _context.Counselings
                    .FirstOrDefaultAsync(c => c.ParentCounselingId == counseling.CounselingId && c.Status != "Cancelled");
                if (existingChild == null)
                {
                    await _counselingSchedulerService.CreateFollowUpAppointmentAsync(
                        counseling,
                        model.NextFollowUpDate.Value.Date,
                        model.NextFollowUpTime.Value
                    );
                }
            }

            if (isUpdate)
            {
                TempData["Success"] = "Clinical notes and observations updated successfully.";
            }
            else if (model.NextFollowUpDate.HasValue && model.NextFollowUpTime.HasValue)
            {
                TempData["Success"] = $"Observation saved and follow-up appointment scheduled for {model.NextFollowUpDate.Value:MMM dd, yyyy} at {DateTime.Today.Add(model.NextFollowUpTime.Value):h:mm tt} successfully.";
            }
            else
            {
                TempData["Success"] = "Observation saved successfully. Session marked as completed with no follow-up needed.";
            }

            return RedirectToAction(
                "Appointment"
            );
        }


        // =========================================================
        // EDIT OBSERVATION REPORT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> EditObservationReport(int id)
        {
            var psychologistId = HttpContext.Session.GetInt32("PsychologistId");
            if (psychologistId == null)
            {
                return RedirectToAction("Login");
            }

            var report = await _context.ObservationReports
                .FirstOrDefaultAsync(r => r.ObservationReportId == id && r.PsychologistId == psychologistId.Value);

            if (report == null)
            {
                return NotFound();
            }

            var latestObs = await _context.CounselingObservations
                .Where(o => o.StudentId == report.StudentId)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            int targetCounselingId = latestObs?.CounselingId ?? report.RootCounselingId;
            return RedirectToAction("CounselingDetails", new { id = targetCounselingId });
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
        public IActionResult ObservationReports()
        {
            return RedirectToAction(nameof(StudentProgressReports));
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
            return RedirectToAction(nameof(StudentProgressReports));
        }





        // =========================================================
        // COUNSELING HISTORY
        // =========================================================

        public async Task<IActionResult> CounselingHistory(
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

            await CounselingSchedulerService.UpdateMissedAppointmentsAsync(_context);

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