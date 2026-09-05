using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentMentalHealthMonitoringSystem.Data;
using StudentMentalHealthMonitoringSystem.Models;
using StudentMentalHealthMonitoringSystem.Services;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace StudentMentalHealthMonitoringSystem.Controllers
{
    public class GuardianController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public GuardianController(
            ApplicationDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }


        // =====================================================
        // GUARDIAN LOGIN - GET
        // =====================================================

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("GuardianStudentId") != null)
            {
                return RedirectToAction("Dashboard");
            }

            return View();
        }


        // =====================================================
        // GUARDIAN LOGIN (REQUEST OTP) - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
            {
                ViewBag.Error = "Please enter the Student ID Number.";
                return View();
            }

            studentId = studentId.Trim();

            // Find student using Student ID Number
            var student = await _context.Students
                .FirstOrDefaultAsync(
                    s => s.StudentIdNumber.ToLower() == studentId.ToLower()
                );

            if (student == null)
            {
                ViewBag.Error = "No student record was found with this Student ID.";
                return View();
            }

            // Check if Guardian Email is registered
            if (string.IsNullOrWhiteSpace(student.GuardianEmail))
            {
                ViewBag.Error = "No guardian email address is registered on file for this student. Please contact university administration or have the student update their guardian profile.";
                return View();
            }

            // Generate 6-digit OTP
            var otp = Random.Shared.Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);

            // Store temporary login state in Session
            HttpContext.Session.SetInt32("GuardianLogin_StudentId", student.StudentId);
            HttpContext.Session.SetString("GuardianLogin_StudentName", student.FullName);
            HttpContext.Session.SetString("GuardianLogin_StudentIdNumber", student.StudentIdNumber ?? "");
            HttpContext.Session.SetString("GuardianLogin_GuardianEmail", student.GuardianEmail.Trim());
            HttpContext.Session.SetString("GuardianLogin_GuardianName", student.GuardianName ?? "");
            HttpContext.Session.SetString("GuardianLogin_Otp", otp);
            HttpContext.Session.SetString("GuardianLogin_Expiry", expiry.ToString("o"));

            try
            {
                await _emailService.SendGuardianLoginOtpAsync(
                    student.GuardianEmail.Trim(),
                    student.GuardianName,
                    student.FullName,
                    student.StudentIdNumber,
                    otp
                );

                TempData["SuccessMessage"] = $"A 6-digit login verification code (OTP) has been sent to the guardian email ({MaskEmail(student.GuardianEmail)}).";
                return RedirectToAction("VerifyOtp");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Failed to dispatch OTP email: {ex.Message}";
                return View();
            }
        }


        // =====================================================
        // VERIFY OTP - GET
        // =====================================================

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var studentId = HttpContext.Session.GetInt32("GuardianLogin_StudentId");
            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            ViewBag.StudentName = HttpContext.Session.GetString("GuardianLogin_StudentName");
            ViewBag.StudentIdNumber = HttpContext.Session.GetString("GuardianLogin_StudentIdNumber");
            ViewBag.MaskedEmail = MaskEmail(HttpContext.Session.GetString("GuardianLogin_GuardianEmail"));

            return View();
        }


        // =====================================================
        // VERIFY OTP - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(string otp)
        {
            var studentId = HttpContext.Session.GetInt32("GuardianLogin_StudentId");
            var studentName = HttpContext.Session.GetString("GuardianLogin_StudentName");
            var studentIdNumber = HttpContext.Session.GetString("GuardianLogin_StudentIdNumber");
            var sessionOtp = HttpContext.Session.GetString("GuardianLogin_Otp");
            var expiryStr = HttpContext.Session.GetString("GuardianLogin_Expiry");

            if (studentId == null || string.IsNullOrWhiteSpace(sessionOtp))
            {
                TempData["ErrorMessage"] = "Login session has expired. Please enter the Student ID again.";
                return RedirectToAction("Login");
            }

            ViewBag.StudentName = studentName;
            ViewBag.StudentIdNumber = studentIdNumber;
            ViewBag.MaskedEmail = MaskEmail(HttpContext.Session.GetString("GuardianLogin_GuardianEmail"));

            if (string.IsNullOrWhiteSpace(otp))
            {
                ViewBag.Error = "Please enter the 6-digit OTP code.";
                return View();
            }

            if (DateTime.TryParse(expiryStr, null, DateTimeStyles.RoundtripKind, out var expiryTime))
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

            // Authentication Successful -> Set Guardian Session
            HttpContext.Session.SetInt32("GuardianStudentId", studentId.Value);
            HttpContext.Session.SetString("GuardianStudentName", studentName ?? "");
            HttpContext.Session.SetString("GuardianStudentIdNumber", studentIdNumber ?? "");

            // Clear temporary OTP state
            HttpContext.Session.Remove("GuardianLogin_StudentId");
            HttpContext.Session.Remove("GuardianLogin_StudentName");
            HttpContext.Session.Remove("GuardianLogin_StudentIdNumber");
            HttpContext.Session.Remove("GuardianLogin_GuardianEmail");
            HttpContext.Session.Remove("GuardianLogin_GuardianName");
            HttpContext.Session.Remove("GuardianLogin_Otp");
            HttpContext.Session.Remove("GuardianLogin_Expiry");

            return RedirectToAction("Dashboard");
        }


        // =====================================================
        // RESEND OTP - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp()
        {
            var studentId = HttpContext.Session.GetInt32("GuardianLogin_StudentId");
            var guardianEmail = HttpContext.Session.GetString("GuardianLogin_GuardianEmail");
            var guardianName = HttpContext.Session.GetString("GuardianLogin_GuardianName");
            var studentName = HttpContext.Session.GetString("GuardianLogin_StudentName");
            var studentIdNumber = HttpContext.Session.GetString("GuardianLogin_StudentIdNumber");

            if (studentId == null || string.IsNullOrWhiteSpace(guardianEmail))
            {
                return Json(new { success = false, message = "Session expired. Please enter Student ID again." });
            }

            var otp = Random.Shared.Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);

            HttpContext.Session.SetString("GuardianLogin_Otp", otp);
            HttpContext.Session.SetString("GuardianLogin_Expiry", expiry.ToString("o"));

            try
            {
                await _emailService.SendGuardianLoginOtpAsync(
                    guardianEmail,
                    guardianName,
                    studentName ?? "Student",
                    studentIdNumber ?? "",
                    otp
                );

                return Json(new { success = true, message = "A new verification code has been sent to the guardian email." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to send email: {ex.Message}" });
            }
        }


        // =====================================================
        // GUARDIAN DASHBOARD
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // Get logged-in student's ID
            var studentId = HttpContext.Session.GetInt32("GuardianStudentId");

            // Guardian is not logged in
            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            // Find student
            var student = await _context.Students
                .FirstOrDefaultAsync(
                    s => s.StudentId == studentId.Value
                );

            // Student does not exist
            if (student == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

            // Automated missed appointments check
            await CounselingSchedulerService.UpdateMissedAppointmentsAsync(_context);

            // Fetch counseling appointments for student
            var appointments = await _context.Counselings
                .Include(c => c.Psychologist)
                .Where(c => c.StudentId == studentId.Value)
                .OrderByDescending(c => c.CounselingDate)
                .ThenByDescending(c => c.AppointmentTime)
                .ToListAsync();

            var now = DateTime.Now;
            var upcomingAppointments = appointments
                .Where(c => (c.Status == "Confirmed" || c.Status == "Pending") &&
                            (c.CounselingDate.Date > DateTime.Today ||
                            (c.CounselingDate.Date == DateTime.Today && c.AppointmentEndTime >= now.TimeOfDay)))
                .OrderBy(c => c.CounselingDate)
                .ThenBy(c => c.AppointmentTime)
                .ToList();

            // PHQ-9 REPORTS
            var phqReports = await _context.PHQAssessments
                .Where(p => p.StudentId == studentId.Value)
                .OrderByDescending(p => p.AssessmentDate)
                .ToListAsync();

            // C-SSRS REPORTS
            var cssrsReports = await _context.CSSRSAssessments
                .Where(c => c.StudentId == studentId.Value)
                .OrderByDescending(c => c.AssessmentDate)
                .ToListAsync();

            // Send data to View
            ViewBag.Student = student;
            ViewBag.PHQReports = phqReports;
            ViewBag.CSSRSReports = cssrsReports;
            ViewBag.Appointments = appointments;
            ViewBag.UpcomingAppointments = upcomingAppointments;

            return View();
        }


        // =====================================================
        // GUARDIAN APPOINTMENTS - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Appointments()
        {
            var studentId = HttpContext.Session.GetInt32("GuardianStudentId");
            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

            await CounselingSchedulerService.UpdateMissedAppointmentsAsync(_context);

            var appointments = await _context.Counselings
                .Include(c => c.Psychologist)
                .Where(c => c.StudentId == studentId.Value)
                .OrderByDescending(c => c.CounselingDate)
                .ThenByDescending(c => c.AppointmentTime)
                .ToListAsync();

            var now = DateTime.Now;
            var upcomingAppointments = appointments
                .Where(c => (c.Status == "Confirmed" || c.Status == "Pending") &&
                            (c.CounselingDate.Date > DateTime.Today ||
                            (c.CounselingDate.Date == DateTime.Today && c.AppointmentEndTime >= now.TimeOfDay)))
                .OrderBy(c => c.CounselingDate)
                .ThenBy(c => c.AppointmentTime)
                .ToList();

            ViewBag.Student = student;
            ViewBag.Appointments = appointments;
            ViewBag.UpcomingAppointments = upcomingAppointments;

            return View();
        }


        // =====================================================
        // GUARDIAN CANCEL APPOINTMENT - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id, string? reason, string? returnUrl)
        {
            var studentId = HttpContext.Session.GetInt32("GuardianStudentId");
            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            await CounselingSchedulerService.UpdateMissedAppointmentsAsync(_context);

            var counseling = await _context.Counselings
                .Include(c => c.Student)
                .Include(c => c.Psychologist)
                .FirstOrDefaultAsync(c => c.CounselingId == id && c.StudentId == studentId.Value);

            if (counseling == null)
            {
                TempData["ErrorMessage"] = "Counseling appointment record was not found.";
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Dashboard")! : returnUrl);
            }

            if (counseling.Status == "Cancelled")
            {
                TempData["ErrorMessage"] = "This appointment has already been cancelled.";
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Dashboard")! : returnUrl);
            }

            if (counseling.Status == "Completed")
            {
                TempData["ErrorMessage"] = "Completed counseling sessions cannot be cancelled.";
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Dashboard")! : returnUrl);
            }

            var now = DateTime.Now;
            bool isBeforeAppointment = counseling.CounselingDate.Date > DateTime.Today ||
                (counseling.CounselingDate.Date == DateTime.Today && counseling.AppointmentTime > now.TimeOfDay);

            if (!isBeforeAppointment)
            {
                if (counseling.Status != "Completed")
                {
                    counseling.Status = "Missed";
                    await _context.SaveChangesAsync();
                }
                TempData["ErrorMessage"] = "Appointments can only be cancelled prior to the scheduled date and time.";
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Dashboard")! : returnUrl);
            }

            // Update status to Cancelled
            counseling.Status = "Cancelled";
            var guardianName = string.IsNullOrWhiteSpace(counseling.Student?.GuardianName)
                ? "Guardian"
                : counseling.Student.GuardianName.Trim();

            string cancelNote = string.IsNullOrWhiteSpace(reason)
                ? $"Cancelled by guardian ({guardianName}) on {DateTime.Now:MMM dd, yyyy h:mm tt}."
                : $"Cancelled by guardian ({guardianName}) on {DateTime.Now:MMM dd, yyyy h:mm tt}. Reason: {reason.Trim()}";

            counseling.Observation = string.IsNullOrWhiteSpace(counseling.Observation)
                ? cancelNote
                : $"{counseling.Observation} | {cancelNote}";

            await _context.SaveChangesAsync();

            // Send notification emails
            try
            {
                var studentName = counseling.Student?.FullName ?? "Student";
                var psychologistName = counseling.Psychologist?.FullName ?? "University Psychologist";

                // 1. Notify Student
                if (counseling.Student != null && !string.IsNullOrWhiteSpace(counseling.Student.Email))
                {
                    await _emailService.SendAppointmentCancellationEmailAsync(
                        recipientEmail: counseling.Student.Email,
                        recipientName: studentName,
                        otherPartyName: psychologistName,
                        appointmentDate: counseling.CounselingDate,
                        startTime: counseling.AppointmentTime,
                        endTime: counseling.AppointmentEndTime,
                        appointmentRoom: counseling.AppointmentRoom,
                        cancelledBy: $"your guardian ({guardianName})",
                        cancellationReason: reason
                    );
                }

                // 2. Notify Psychologist
                if (counseling.Psychologist != null && !string.IsNullOrWhiteSpace(counseling.Psychologist.Email))
                {
                    await _emailService.SendAppointmentCancellationEmailAsync(
                        recipientEmail: counseling.Psychologist.Email,
                        recipientName: psychologistName,
                        otherPartyName: $"{studentName} (ID: {counseling.Student?.StudentIdNumber})",
                        appointmentDate: counseling.CounselingDate,
                        startTime: counseling.AppointmentTime,
                        endTime: counseling.AppointmentEndTime,
                        appointmentRoom: counseling.AppointmentRoom,
                        cancelledBy: $"student's guardian ({guardianName})",
                        cancellationReason: reason
                    );
                }

                // 3. Notify Guardian
                if (counseling.Student != null && !string.IsNullOrWhiteSpace(counseling.Student.GuardianEmail))
                {
                    await _emailService.SendAppointmentCancellationEmailAsync(
                        recipientEmail: counseling.Student.GuardianEmail,
                        recipientName: guardianName,
                        otherPartyName: $"{psychologistName} (for student {studentName})",
                        appointmentDate: counseling.CounselingDate,
                        startTime: counseling.AppointmentTime,
                        endTime: counseling.AppointmentEndTime,
                        appointmentRoom: counseling.AppointmentRoom,
                        cancelledBy: "you (Guardian)",
                        cancellationReason: reason
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GuardianController] Cancellation email dispatch failed: {ex.Message}");
            }

            TempData["SuccessMessage"] = "The scheduled appointment has been successfully cancelled.";
            return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Dashboard")! : returnUrl);
        }


        // =====================================================
        // GUARDIAN LOGOUT
        // =====================================================

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }


        // =====================================================
        // HELPER: MASK EMAIL
        // =====================================================

        private static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return "registered email";
            }

            var parts = email.Split('@');
            var name = parts[0];
            var domain = parts[1];

            if (name.Length <= 2)
            {
                return $"{name[0]}*@{domain}";
            }

            var visiblePrefix = name.Substring(0, 2);
            var visibleSuffix = name.Substring(name.Length - 1);
            var masked = new string('*', Math.Max(3, name.Length - 3));

            return $"{visiblePrefix}{masked}{visibleSuffix}@{domain}";
        }
    }
}