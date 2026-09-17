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
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly GeminiChatService _geminiChatService;
        private readonly CounselingSchedulerService _counselingSchedulerService;
        private readonly EmailService _emailService;

        public StudentController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            GeminiChatService geminiChatService,
            CounselingSchedulerService counselingSchedulerService,
            EmailService emailService)
        {
            _context = context;
            _environment = environment;
            _geminiChatService = geminiChatService;
            _counselingSchedulerService = counselingSchedulerService;
            _emailService = emailService;
        }

        // =====================================================
        // LOGIN
        // =====================================================

        // ================= Login GET =================

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("StudentId") != null)
            {
                return RedirectToAction("Dashboard");
            }

            return View();
        }

        // ================= Login POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(
            string email,
            string password)
        {
            var student =
                _context.Students
                    .FirstOrDefault(
                        s => s.Email == email
                    );

            if (student == null)
            {
                ViewBag.Error =
                    "Invalid Email or Password";

                return View();
            }

            bool isPasswordValid = false;
            try
            {
                if (!string.IsNullOrEmpty(student.Password))
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(password, student.Password);
                }
            }
            catch
            {
                isPasswordValid = (student.Password == password);
            }

            if (!isPasswordValid && student.Password == password)
            {
                isPasswordValid = true;
            }

            if (!isPasswordValid)
            {
                ViewBag.Error = "Invalid Email or Password";
                return View();
            }

            // ================= Check Account Suspension =================
            if (student.IsSuspended)
            {
                ViewBag.SuspendedError = true;
                return View();
            }

            if (string.IsNullOrWhiteSpace(student.Semester) || student.Semester != student.ActiveSemester)
            {
                student.Semester = student.ActiveSemester;
                _context.SaveChanges();
            }

            HttpContext.Session.SetInt32(
                "StudentId",
                student.StudentId
            );

            HttpContext.Session.SetString(
                "StudentName",
                student.FullName
            );

            HttpContext.Session.SetString(
                "StudentIdNumber",
                student.StudentIdNumber ?? ""
            );

            HttpContext.Session.SetString(
                "StudentDepartment",
                student.Department ?? "CSE"
            );

            HttpContext.Session.SetString(
                "StudentSemester",
                student.ActiveSemester
            );

            HttpContext.Session.SetString(
                "StudentProfileImage",
                student.ProfileImage ?? ""
            );

            return RedirectToAction(
                "Dashboard"
            );
        }

        // =====================================================
        // FORGOT PASSWORD
        // =====================================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (HttpContext.Session.GetInt32("StudentId") != null)
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

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email.ToLower() == email);

            if (student == null)
            {
                ViewBag.Error = "No student account was found with this email address.";
                return View();
            }

            if (student.IsSuspended)
            {
                ViewBag.Error = "Your student account is currently suspended. Verification code cannot be sent. Please contact university administration.";
                return View();
            }

            var otp = Random.Shared.Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);

            HttpContext.Session.SetString("Student_Reset_Email", student.Email);
            HttpContext.Session.SetString("Student_Reset_Otp", otp);
            HttpContext.Session.SetString("Student_Reset_Expiry", expiry.ToString("o"));

            try
            {
                await _emailService.SendPasswordResetOtpAsync(student.Email, student.FullName, otp, "Student");
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
            var resetEmail = HttpContext.Session.GetString("Student_Reset_Email");
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
            var resetEmail = HttpContext.Session.GetString("Student_Reset_Email");
            var sessionOtp = HttpContext.Session.GetString("Student_Reset_Otp");
            var expiryStr = HttpContext.Session.GetString("Student_Reset_Expiry");

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

            // Strong password check: at least 1 uppercase, 1 lowercase, 1 digit, 1 special char
            if (!System.Text.RegularExpressions.Regex.IsMatch(newPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$"))
            {
                ViewBag.Error = "Password must contain at least 1 uppercase letter, 1 lowercase letter, 1 number, and 1 special character.";
                return View();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email.ToLower() == resetEmail.ToLower());

            if (student == null)
            {
                ViewBag.Error = "Student account not found.";
                return View();
            }

            student.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            // Clear reset session
            HttpContext.Session.Remove("Student_Reset_Email");
            HttpContext.Session.Remove("Student_Reset_Otp");
            HttpContext.Session.Remove("Student_Reset_Expiry");

            TempData["SuccessMessage"] = "Your password has been successfully reset! Please log in with your new password.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendResetOtp()
        {
            var resetEmail = HttpContext.Session.GetString("Student_Reset_Email");
            if (string.IsNullOrWhiteSpace(resetEmail))
            {
                return Json(new { success = false, message = "Session expired. Please start over." });
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email.ToLower() == resetEmail.ToLower());

            if (student == null)
            {
                return Json(new { success = false, message = "Student account not found." });
            }

            if (student.IsSuspended)
            {
                return Json(new { success = false, message = "Your student account is suspended. Verification code cannot be sent." });
            }

            var otp = Random.Shared.Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);

            HttpContext.Session.SetString("Student_Reset_Otp", otp);
            HttpContext.Session.SetString("Student_Reset_Expiry", expiry.ToString("o"));

            try
            {
                await _emailService.SendPasswordResetOtpAsync(student.Email, student.FullName, otp, "Student");
                return Json(new { success = true, message = "A new verification code has been sent to your email." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to send email: {ex.Message}" });
            }
        }

        // =====================================================
        // REGISTER
        // =====================================================

        private void PopulateRegistrationDepartments()
        {
            var depts = _context.Departments
                .Select(d => d.DepartmentName)
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (!depts.Any())
            {
                depts = new List<string> { "CSE", "EEE", "Mechanical", "Civil", "BBA", "BATHM" };
            }

            ViewBag.Departments = depts;
        }

        // ================= Register GET =================

        [HttpGet]
        public IActionResult Register()
        {
            PopulateRegistrationDepartments();
            return View();
        }

        // ================= Register POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            Student student)
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
                    string.Join(
                        " | ",
                        errors
                    );

                PopulateRegistrationDepartments();
                return View(student);
            }

            // ================= Duplicate Email =================

            if (_context.Students.Any(
                s => s.Email == student.Email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Email already exists."
                );

                PopulateRegistrationDepartments();
                return View(student);
            }

            // ================= Validate Student ID format (No negative / minus IDs) =================
            if (string.IsNullOrWhiteSpace(student.StudentIdNumber) ||
                student.StudentIdNumber.Trim().StartsWith("-") ||
                (long.TryParse(student.StudentIdNumber.Trim(), out long numVal) && numVal <= 0) ||
                !System.Text.RegularExpressions.Regex.IsMatch(student.StudentIdNumber.Trim(), @"^[a-zA-Z0-9][a-zA-Z0-9_\-\./]*$"))
            {
                ModelState.AddModelError(
                    "StudentIdNumber",
                    "Student ID cannot start with a minus sign or be negative. Please enter a valid Student ID."
                );

                PopulateRegistrationDepartments();
                return View(student);
            }

            // ================= Duplicate Student ID =================

            if (_context.Students.Any(
                s =>
                    s.StudentIdNumber ==
                    student.StudentIdNumber))
            {
                ModelState.AddModelError(
                    "StudentIdNumber",
                    "Student ID already exists."
                );

                PopulateRegistrationDepartments();
                return View(student);
            }

            // ================= Validate Date of Birth (No future date) =================
            if (student.DateOfBirth.HasValue && student.DateOfBirth.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError("DateOfBirth", "Date of birth cannot be in the future. Please select a valid birth date.");
                PopulateRegistrationDepartments();
                return View(student);
            }

            // ================= Validate Academic Year & Semester (No future time) =================
            if (student.AdmissionYear.HasValue && student.AdmissionYear.Value > DateTime.Now.Year)
            {
                ModelState.AddModelError("AdmissionYear", "Academic / Batch Year cannot be in the future. Please select a valid academic year.");
                PopulateRegistrationDepartments();
                return View(student);
            }

            if (student.AdmissionYear.HasValue && student.AdmissionYear.Value == DateTime.Now.Year && !string.IsNullOrWhiteSpace(student.Semester))
            {
                int currentMonth = DateTime.Now.Month;
                int currentMaxTermOrder = currentMonth <= 4 ? 1 : (currentMonth <= 8 ? 2 : 3);
                int selectedTermOrder = student.Semester.Trim().ToLower() switch
                {
                    var s when s.StartsWith("spring") => 1,
                    var s when s.StartsWith("summer") => 2,
                    var s when s.StartsWith("fall") => 3,
                    _ => 0
                };

                if (selectedTermOrder > currentMaxTermOrder)
                {
                    ModelState.AddModelError("Semester", "The selected admission term is in the future. Please select a current or past term.");
                    PopulateRegistrationDepartments();
                    return View(student);
                }
            }

            // ================= Password Complexity Validation =================

            if (string.IsNullOrWhiteSpace(student.Password) || student.Password.Length < 8 ||
                !System.Text.RegularExpressions.Regex.IsMatch(student.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$"))
            {
                ModelState.AddModelError("Password", "Password must contain at least 8 characters, including 1 uppercase letter, 1 lowercase letter, 1 number, and 1 special character.");
                PopulateRegistrationDepartments();
                return View(student);
            }

            try
            {
                // ================= Password Hash =================

                student.Password =
                    BCrypt.Net.BCrypt.HashPassword(
                        student.Password
                    );

                // ================= Upload Image =================

                if (student.ImageFile != null &&
                    student.ImageFile.Length > 0)
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
                            student.ImageFile.FileName
                        ).ToLower();

                    if (!allowedExtensions.Contains(
                        extension))
                    {
                        ModelState.AddModelError(
                            "ImageFile",
                            "Only JPG, JPEG and PNG images are allowed."
                        );

                        PopulateRegistrationDepartments();
                        return View(student);
                    }

                    var uploadFolder =
                        Path.Combine(
                            _environment.WebRootPath,
                            "images",
                            "students"
                        );

                    if (!Directory.Exists(
                        uploadFolder))
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

                    await student.ImageFile
                        .CopyToAsync(stream);

                    student.ProfileImage =
                        $"/images/students/{fileName}";
                }

                // Format Semester with Year (e.g., Spring 2026)
                if (!string.IsNullOrWhiteSpace(student.Semester) && student.AdmissionYear.HasValue)
                {
                    if (!student.Semester.Contains(student.AdmissionYear.Value.ToString()))
                    {
                        student.Semester = $"{student.Semester} {student.AdmissionYear.Value}";
                    }
                }
                else if (!string.IsNullOrWhiteSpace(student.Semester) && !student.Semester.Any(char.IsDigit))
                {
                    student.Semester = $"{student.Semester} {DateTime.Now.Year}";
                }

                // ================= Save Student =================

                _context.Students.Add(
                    student
                );

                await _context.SaveChangesAsync();

                // Initialize continuous semester observation record
                if (!string.IsNullOrWhiteSpace(student.Semester))
                {
                    try
                    {
                        var initialRecord = new StudentSemesterRecord
                        {
                            StudentId = student.StudentId,
                            Semester = student.Semester,
                            FeelingRiskLevel = "Normal",
                            FeelingSummary = "Continuous Observation Initialized"
                        };

                        _context.StudentSemesterRecords.Add(initialRecord);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception) { }
                }

                TempData["Success"] =
                    "Registration Successful. Continuous Semester Observation is now active for your account.";

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

        // =====================================================
        // STUDENT DASHBOARD
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // ================= Check Session =================

            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );

            if (studentId == null)
            {
                return RedirectToAction(
                    "Login"
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
                    "Login"
                );
            }

            // Ensure student semester is updated to active format
            if (string.IsNullOrWhiteSpace(student.Semester) || student.Semester != student.ActiveSemester)
            {
                student.Semester = student.ActiveSemester;
                await _context.SaveChangesAsync();
            }

            // Refresh session variables for sidebar
            HttpContext.Session.SetString("StudentName", student.FullName);
            HttpContext.Session.SetString("StudentIdNumber", student.StudentIdNumber ?? "");
            HttpContext.Session.SetString("StudentDepartment", student.Department ?? "CSE");
            HttpContext.Session.SetString("StudentSemester", student.ActiveSemester);
            HttpContext.Session.SetString("StudentProfileImage", student.ProfileImage ?? "");


            // =================================================
            // COUNSELING INFORMATION
            // =================================================

            await CounselingSchedulerService.UpdateMissedAppointmentsAsync(_context);

            var counselings =
                await _context.Counselings
                    .Include(c =>
                        c.Psychologist)
                    .Where(c =>
                        c.StudentId ==
                            student.StudentId &&

                        c.Status !=
                            "Cancelled")
                    .OrderBy(c =>
                        c.CounselingDate)
                    .ThenBy(c =>
                        c.AppointmentTime)
                    .ToListAsync();


            // ================= Counseling Count =================

            ViewBag.CounselingCount =
                counselings.Count;


            // ================= Next Counseling =================

            var nextCounseling =
                counselings
                    .Where(c =>
                        c.Status !=
                            "Completed" &&

                        c.Status !=
                            "Cancelled" &&

                        c.Status !=
                            "Missed" &&

                        c.CounselingDate.Date
                            .Add(
                                c.AppointmentTime
                            ) >=
                            DateTime.Now
                    )
                    .OrderBy(c =>
                        c.CounselingDate)
                    .ThenBy(c =>
                        c.AppointmentTime)
                    .FirstOrDefault();


            ViewBag.NextCounseling =
                nextCounseling;


            // ================= Default Status =================

            ViewBag.PHQCompleted =
                false;

            ViewBag.CSSRSCompleted =
                false;

            ViewBag.ScreeningCompleted =
                false;

            ViewBag.RiskLevel =
                "Not Assessed";


            // ================= AI Chat Status =================

            ViewBag.ChatRiskStatus =
                student.LatestChatRiskStatus
                ?? "Not Assessed";


            // ================= Semester Check =================

            if (string.IsNullOrWhiteSpace(
                student.Semester))
            {
                return View(
                    student
                );
            }


            // =================================================
            // PHQ STATUS
            // =================================================

            var phqAssessment =
                await _context.PHQAssessments
                    .FirstOrDefaultAsync(
                        p =>
                            p.StudentId ==
                                student.StudentId &&

                            p.Semester ==
                                student.Semester
                    );


            bool phqCompleted =
                phqAssessment != null;


            // =================================================
            // C-SSRS STATUS
            // =================================================

            var cssrsAssessment =
                await _context.CSSRSAssessments
                    .FirstOrDefaultAsync(
                        c =>
                            c.StudentId ==
                                student.StudentId &&

                            c.Semester ==
                                student.Semester
                    );


            bool cssrsCompleted =
                cssrsAssessment != null;





            // ================= Send Status =================

            ViewBag.PHQCompleted =
                phqCompleted;

            ViewBag.CSSRSCompleted =
                cssrsCompleted;


            // ================= Screening Complete =================

            ViewBag.ScreeningCompleted =
                phqCompleted &&
                cssrsCompleted;


            // =================================================
            // PROJECT LEVEL RISK
            // =================================================
            //
            // Highest available severity from:
            //
            // PHQ-9
            // C-SSRS
            // AI Chat
            //
            // No weighted combined score is calculated.
            // =================================================

            var projectRiskLevels =
                new List<string>();


            // ================= PHQ Risk =================

            if (phqAssessment != null)
            {
                projectRiskLevels.Add(
                    GetPHQProjectSeverity(
                        phqAssessment.SeverityLevel
                    )
                );
            }


            // ================= C-SSRS Risk =================

            if (cssrsAssessment != null)
            {
                projectRiskLevels.Add(
                    GetCSSRSProjectSeverity(
                        cssrsAssessment.RiskLevel
                    )
                );
            }


            // ================= AI Chat Risk =================

            if (!string.IsNullOrWhiteSpace(
                student.LatestChatRiskStatus))
            {
                projectRiskLevels.Add(
                    student.LatestChatRiskStatus
                );
            }


            // ================= Highest Risk =================

            if (projectRiskLevels.Contains(
                "Extremely Severe"))
            {
                ViewBag.RiskLevel =
                    "Extremely Severe";
            }
            else if (projectRiskLevels.Contains(
                "Severe"))
            {
                ViewBag.RiskLevel =
                    "Severe";
            }
            else if (projectRiskLevels.Contains(
                "Moderate"))
            {
                ViewBag.RiskLevel =
                    "Moderate";
            }
            else if (projectRiskLevels.Any())
            {
                ViewBag.RiskLevel =
                    "Normal";
            }
            else
            {
                ViewBag.RiskLevel =
                    "Not Assessed";
            }

            // ================= Semester Screening Compliance =================
            var currentSemester = string.IsNullOrWhiteSpace(student.Semester) ? "Semester 1" : student.Semester;

            var phqRecord = await _context.PHQAssessments
                .Where(p => p.StudentId == student.StudentId && p.Semester.ToLower() == currentSemester.ToLower())
                .OrderByDescending(p => p.AssessmentDate)
                .FirstOrDefaultAsync();

            var cssrsRecord = await _context.CSSRSAssessments
                .Where(c => c.StudentId == student.StudentId && c.Semester.ToLower() == currentSemester.ToLower())
                .OrderByDescending(c => c.AssessmentDate)
                .FirstOrDefaultAsync();

            var screeningEval = Services.ScreeningComplianceService.Evaluate(
                hasPHQ: phqRecord != null,
                phqSeverity: phqRecord?.SeverityLevel,
                phqScore: phqRecord?.TotalScore,
                hasCSSRS: cssrsRecord != null,
                cssrsRiskLevel: cssrsRecord?.RiskLevel
            );

            ViewBag.HasPHQ = phqRecord != null;
            ViewBag.HasCSSRS = cssrsRecord != null;
            ViewBag.ScreeningCompleted = screeningEval.IsScreeningComplete;
            ViewBag.IsScreeningComplete = screeningEval.IsScreeningComplete;
            ViewBag.ScreeningWarningTitle = screeningEval.WarningTitle;
            ViewBag.ScreeningWarningReason = screeningEval.WarningReason;
            ViewBag.ScreeningStatusBadgeText = screeningEval.StatusBadgeText;

            return View(
                student
            );
        }

        // =====================================================
        // SEMESTER SCREENING
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> SemesterScreening()
        {
            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );

            if (studentId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }

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
                    "Login"
                );
            }

            if (string.IsNullOrWhiteSpace(
                student.Semester))
            {
                TempData["Error"] =
                    "Current semester information was not found.";

                return RedirectToAction(
                    "Dashboard"
                );
            }

            // ================= Evaluate Screening Compliance =================
            var phqRecord = await _context.PHQAssessments
                .Where(p => p.StudentId == student.StudentId && p.Semester.ToLower() == student.Semester.ToLower())
                .OrderByDescending(p => p.AssessmentDate)
                .FirstOrDefaultAsync();

            var cssrsRecord = await _context.CSSRSAssessments
                .Where(c => c.StudentId == student.StudentId && c.Semester.ToLower() == student.Semester.ToLower())
                .OrderByDescending(c => c.AssessmentDate)
                .FirstOrDefaultAsync();

            var screeningEval = Services.ScreeningComplianceService.Evaluate(
                hasPHQ: phqRecord != null,
                phqSeverity: phqRecord?.SeverityLevel,
                phqScore: phqRecord?.TotalScore,
                hasCSSRS: cssrsRecord != null,
                cssrsRiskLevel: cssrsRecord?.RiskLevel
            );

            ViewBag.PHQCompleted = phqRecord != null;
            ViewBag.PHQSeverity = phqRecord?.SeverityLevel;
            ViewBag.PHQScore = phqRecord?.TotalScore;
            ViewBag.IsPHQSevere = screeningEval.IsPHQSevere;

            ViewBag.CSSRSCompleted = cssrsRecord != null;
            ViewBag.CSSRSRiskLevel = cssrsRecord?.RiskLevel;
            ViewBag.IsCSSRSSevere = screeningEval.IsCSSRSSevere;

            ViewBag.IsScreeningComplete = screeningEval.IsScreeningComplete;
            ViewBag.ScreeningWarningTitle = screeningEval.WarningTitle;
            ViewBag.ScreeningWarningReason = screeningEval.WarningReason;
            ViewBag.StatusBadgeText = screeningEval.StatusBadgeText;
            ViewBag.CurrentSemester = student.Semester;

            return View();
        }

        // =====================================================
        // PHQ
        // =====================================================

        // ================= PHQ GET =================

        [HttpGet]
        public async Task<IActionResult> PHQ()
        {
            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );

            if (studentId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }

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
                    "Login"
                );
            }

            if (string.IsNullOrWhiteSpace(
                student.Semester))
            {
                TempData["Error"] =
                    "Current semester information was not found.";

                return RedirectToAction(
                    "Dashboard"
                );
            }

            // ================= Previous Assessment =================

            var previousAssessment =
                await _context.PHQAssessments
                    .FirstOrDefaultAsync(
                        p =>
                            p.StudentId ==
                            studentId.Value &&
                            p.Semester ==
                            student.Semester
                    );

            if (previousAssessment != null)
            {
                return RedirectToAction(
                    "PHQResult",
                    new
                    {
                        id =
                            previousAssessment
                                .AssessmentId
                    }
                );
            }

            var model =
                new PHQAssessment
                {
                    Semester =
                        student.Semester
                };

            return View(model);
        }

        // ================= PHQ POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PHQ(
            PHQAssessment model)
        {
            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );

            if (studentId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }

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
                    "Login"
                );
            }

            if (string.IsNullOrWhiteSpace(
                student.Semester))
            {
                TempData["Error"] =
                    "Current semester information was not found.";

                return RedirectToAction(
                    "Dashboard"
                );
            }

            // ================= Server Values =================

            model.StudentId =
                student.StudentId;

            model.Semester =
                student.Semester;

            // ================= Remove Validation =================

            ModelState.Remove(
                nameof(
                    PHQAssessment.StudentId
                )
            );

            ModelState.Remove(
                nameof(
                    PHQAssessment.Student
                )
            );

            ModelState.Remove(
                nameof(
                    PHQAssessment.Semester
                )
            );

            ModelState.Remove(
                nameof(
                    PHQAssessment.TotalScore
                )
            );

            ModelState.Remove(
                nameof(
                    PHQAssessment.SeverityLevel
                )
            );

            ModelState.Remove(
                nameof(
                    PHQAssessment
                        .RequiresImmediateReview
                )
            );

            ModelState.Remove(
                nameof(
                    PHQAssessment
                        .AssessmentDate
                )
            );

            // ================= Questions Check =================

            if (model.Question1Score == null ||
                model.Question2Score == null ||
                model.Question3Score == null ||
                model.Question4Score == null ||
                model.Question5Score == null ||
                model.Question6Score == null ||
                model.Question7Score == null ||
                model.Question8Score == null ||
                model.Question9Score == null)
            {
                ModelState.AddModelError(
                    "",
                    "Please answer all PHQ-9 questions."
                );

                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ================= Duplicate Check =================

            var previousAssessment =
                await _context.PHQAssessments
                    .FirstOrDefaultAsync(
                        p =>
                            p.StudentId ==
                            student.StudentId &&
                            p.Semester ==
                            student.Semester
                    );

            if (previousAssessment != null)
            {
                return RedirectToAction(
                    "PHQResult",
                    new
                    {
                        id =
                            previousAssessment
                                .AssessmentId
                    }
                );
            }

            // ================= Total Score =================

            model.TotalScore =
                model.Question1Score.Value +
                model.Question2Score.Value +
                model.Question3Score.Value +
                model.Question4Score.Value +
                model.Question5Score.Value +
                model.Question6Score.Value +
                model.Question7Score.Value +
                model.Question8Score.Value +
                model.Question9Score.Value;

            // ================= Severity =================

            model.SeverityLevel =
                GetPHQSeverity(
                    model.TotalScore
                );

            // ================= Immediate Review =================

            model.RequiresImmediateReview =
                model.Question9Score.Value > 0;

            model.AssessmentDate =
                DateTime.Now;

            try
            {
                _context.PHQAssessments.Add(
                    model
                );

                await _context.SaveChangesAsync();


                // =================================================
                // PROJECT SEVERITY
                // =================================================

                var projectSeverity =
                    GetPHQProjectSeverity(
                        model.SeverityLevel
                    );


                // =================================================
                // AUTO PSYCHOLOGIST ASSIGNMENT
                // Rule: Student must complete BOTH PHQ-9 and C-SSRS screenings.
                // Auto appointment triggers only if BOTH exist and either/both is Severe or Extremely Severe.
                // =================================================

                bool hasCompletedCSSRS = await _context.CSSRSAssessments
                    .AnyAsync(c => c.StudentId == student.StudentId);

                if (hasCompletedCSSRS)
                {
                    var latestCSSRS = await _context.CSSRSAssessments
                        .Where(c => c.StudentId == student.StudentId)
                        .OrderByDescending(c => c.AssessmentDate)
                        .FirstOrDefaultAsync();

                    string cssrsProjectSeverity = latestCSSRS != null
                        ? GetCSSRSProjectSeverity(latestCSSRS.RiskLevel)
                        : "Normal";

                    bool isSevereOrExtremelySevere = (projectSeverity == "Severe" ||
                                                      projectSeverity == "Extremely Severe" ||
                                                      cssrsProjectSeverity == "Severe" ||
                                                      cssrsProjectSeverity == "Extremely Severe");

                    if (isSevereOrExtremelySevere)
                    {
                        string effectiveSeverity = (projectSeverity == "Extremely Severe" || cssrsProjectSeverity == "Extremely Severe")
                            ? "Extremely Severe"
                            : "Severe";

                        await _counselingSchedulerService
                            .AutoAssignPsychologistAsync(
                                student.StudentId,
                                effectiveSeverity,
                                "Semester Screening (PHQ-9 & C-SSRS)"
                            );
                    }
                }


                TempData["Success"] =
                    "PHQ-9 assessment submitted successfully.";

                return RedirectToAction(
                    "PHQResult",
                    new
                    {
                        id =
                            model.AssessmentId
                    }
                );
            }
            catch (DbUpdateException ex)
            {
                string errorMessage =
                    ex.InnerException?.Message
                    ?? ex.Message;

                return Content(
                    errorMessage
                );
            }
            catch (Exception ex)
            {
                string errorMessage =
                    ex.InnerException?.Message
                    ?? ex.Message;

                return Content(
                    errorMessage
                );
            }
        }

        // ================= PHQ Result =================

        [HttpGet]
        public async Task<IActionResult> PHQResult(
            int id)
        {
            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );

            if (studentId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }

            var assessment =
                await _context.PHQAssessments
                    .FirstOrDefaultAsync(
                        p =>
                            p.AssessmentId == id &&
                            p.StudentId ==
                            studentId.Value
                    );

            if (assessment == null)
            {
                return NotFound();
            }

            return View(
                assessment
            );
        }

        // ================= PHQ Severity =================

        private string GetPHQSeverity(
            int totalScore)
        {
            if (totalScore <= 4)
            {
                return "Minimal";
            }
            else if (totalScore <= 9)
            {
                return "Mild";
            }
            else if (totalScore <= 14)
            {
                return "Moderate";
            }
            else if (totalScore <= 19)
            {
                return "Moderately Severe";
            }
            else
            {
                return "Severe";
            }
        }


        // =====================================================
        // PHQ PROJECT SEVERITY
        // =====================================================

        private string GetPHQProjectSeverity(
            string? severityLevel)
        {
            if (string.IsNullOrWhiteSpace(
                severityLevel))
            {
                return "Normal";
            }


            if (severityLevel == "Moderate")
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

        // =====================================================
        // C-SSRS
        // =====================================================

        // ================= C-SSRS GET =================

        [HttpGet]
        public async Task<IActionResult> CSRRS()
        {
            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );

            if (studentId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }

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
                    "Login"
                );
            }

            if (string.IsNullOrWhiteSpace(
                student.Semester))
            {
                TempData["Error"] =
                    "Current semester information was not found.";

                return RedirectToAction(
                    "Dashboard"
                );
            }

            // ================= Previous Assessment =================

            var previousAssessment =
                await _context.CSSRSAssessments
                    .FirstOrDefaultAsync(
                        c =>
                            c.StudentId ==
                            student.StudentId &&
                            c.Semester ==
                            student.Semester
                    );

            if (previousAssessment != null)
            {
                return RedirectToAction(
                    "CSRRSResult",
                    new
                    {
                        id =
                            previousAssessment
                                .AssessmentId
                    }
                );
            }

            var model =
                new CSSRSAssessment
                {
                    Semester =
                        student.Semester
                };

            return View(model);
        }

        // ================= C-SSRS POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CSRRS(
            CSSRSAssessment model)
        {
            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );

            if (studentId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }

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
                    "Login"
                );
            }

            if (string.IsNullOrWhiteSpace(
                student.Semester))
            {
                TempData["Error"] =
                    "Current semester information was not found.";

                return RedirectToAction(
                    "Dashboard"
                );
            }

            // ================= Server Values =================

            model.StudentId =
                student.StudentId;

            model.Semester =
                student.Semester;

            // ================= Remove Validation =================

            ModelState.Remove(
                nameof(
                    CSSRSAssessment.StudentId
                )
            );

            ModelState.Remove(
                nameof(
                    CSSRSAssessment.Student
                )
            );

            ModelState.Remove(
                nameof(
                    CSSRSAssessment.Semester
                )
            );

            ModelState.Remove(
                nameof(
                    CSSRSAssessment.RiskLevel
                )
            );

            ModelState.Remove(
                nameof(
                    CSSRSAssessment
                        .RequiresImmediateAction
                )
            );

            ModelState.Remove(
                nameof(
                    CSSRSAssessment
                        .AssessmentDate
                )
            );

            // ================= Required Questions =================

            if (model.Question1Answer == null ||
                model.Question2Answer == null ||
                model.Question6Answer == null)
            {
                ModelState.AddModelError(
                    "",
                    "Please answer the required C-SSRS questions."
                );

                return View(model);
            }

            // ================= Question 2 =================

            if (model.Question2Answer == true &&
                (model.Question3Answer == null ||
                 model.Question4Answer == null ||
                 model.Question5Answer == null))
            {
                ModelState.AddModelError(
                    "",
                    "Please answer Questions 3, 4 and 5."
                );

                return View(model);
            }

            if (model.Question2Answer == false)
            {
                model.Question3Answer =
                    false;

                model.Question4Answer =
                    false;

                model.Question5Answer =
                    false;

                ModelState.Remove(
                    nameof(
                        CSSRSAssessment
                            .Question3Answer
                    )
                );

                ModelState.Remove(
                    nameof(
                        CSSRSAssessment
                            .Question4Answer
                    )
                );

                ModelState.Remove(
                    nameof(
                        CSSRSAssessment
                            .Question5Answer
                    )
                );
            }

            // ================= Question 6 =================

            if (model.Question6Answer == true &&
                model.RecentBehavior == null)
            {
                ModelState.AddModelError(
                    nameof(CSSRSAssessment.RecentBehavior),
                    "Please specify whether the behaviour occurred within the past three months."
                );

                ModelState.AddModelError(
                    "",
                    "Please specify whether the behaviour occurred within the past three months."
                );

                return View(model);
            }

            if (model.Question6Answer == false)
            {
                model.RecentBehavior =
                    false;

                ModelState.Remove(
                    nameof(
                        CSSRSAssessment
                            .RecentBehavior
                    )
                );
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ================= Duplicate Check =================

            var previousAssessment =
                await _context.CSSRSAssessments
                    .FirstOrDefaultAsync(
                        c =>
                            c.StudentId ==
                            student.StudentId &&
                            c.Semester ==
                            student.Semester
                    );

            if (previousAssessment != null)
            {
                return RedirectToAction(
                    "CSRRSResult",
                    new
                    {
                        id =
                            previousAssessment
                                .AssessmentId
                    }
                );
            }

            // ================= Calculate Risk =================

            model.RiskLevel =
                GetCSSRSRiskLevel(
                    model
                );

            model.RequiresImmediateAction =
                model.RiskLevel ==
                "High";

            model.AssessmentDate =
                DateTime.Now;

            try
            {
                _context.CSSRSAssessments.Add(
                    model
                );

                await _context.SaveChangesAsync();


                // =================================================
                // PROJECT SEVERITY
                // =================================================

                var projectSeverity =
                    GetCSSRSProjectSeverity(
                        model.RiskLevel
                    );


                // =================================================
                // AUTO PSYCHOLOGIST ASSIGNMENT
                // Rule: Student must complete BOTH PHQ-9 and C-SSRS screenings.
                // Auto appointment triggers only if BOTH exist and either/both is Severe or Extremely Severe.
                // =================================================

                bool hasCompletedPHQ = await _context.PHQAssessments
                    .AnyAsync(p => p.StudentId == student.StudentId);

                if (hasCompletedPHQ)
                {
                    var latestPHQ = await _context.PHQAssessments
                        .Where(p => p.StudentId == student.StudentId)
                        .OrderByDescending(p => p.AssessmentDate)
                        .FirstOrDefaultAsync();

                    string phqProjectSeverity = latestPHQ != null
                        ? GetPHQProjectSeverity(latestPHQ.SeverityLevel)
                        : "Normal";

                    bool isSevereOrExtremelySevere = (projectSeverity == "Severe" ||
                                                      projectSeverity == "Extremely Severe" ||
                                                      phqProjectSeverity == "Severe" ||
                                                      phqProjectSeverity == "Extremely Severe");

                    if (isSevereOrExtremelySevere)
                    {
                        string effectiveSeverity = (projectSeverity == "Extremely Severe" || phqProjectSeverity == "Extremely Severe")
                            ? "Extremely Severe"
                            : "Severe";

                        await _counselingSchedulerService
                            .AutoAssignPsychologistAsync(
                                student.StudentId,
                                effectiveSeverity,
                                "Semester Screening (PHQ-9 & C-SSRS)"
                            );
                    }
                }


                TempData["Success"] =
                    "C-SSRS assessment submitted successfully.";

                return RedirectToAction(
                    "CSRRSResult",
                    new
                    {
                        id =
                            model.AssessmentId
                    }
                );
            }
            catch (DbUpdateException ex)
            {
                string errorMessage =
                    ex.InnerException?.Message
                    ?? ex.Message;

                return Content(
                    errorMessage
                );
            }
            catch (Exception ex)
            {
                string errorMessage =
                    ex.InnerException?.Message
                    ?? ex.Message;

                return Content(
                    errorMessage
                );
            }
        }

        // ================= C-SSRS Result =================

        [HttpGet]
        public async Task<IActionResult> CSRRSResult(
            int id)
        {
            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );

            if (studentId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }

            var assessment =
                await _context.CSSRSAssessments
                    .FirstOrDefaultAsync(
                        c =>
                            c.AssessmentId == id &&
                            c.StudentId ==
                            studentId.Value
                    );

            if (assessment == null)
            {
                return NotFound();
            }

            return View(
                assessment
            );
        }

        // ================= C-SSRS Risk =================

        private string GetCSSRSRiskLevel(
            CSSRSAssessment model)
        {
            // ================= High Risk =================

            if (model.Question4Answer == true ||
                model.Question5Answer == true ||
                (model.Question6Answer == true &&
                 model.RecentBehavior == true))
            {
                return "High";
            }

            // ================= Moderate Risk =================

            if (model.Question3Answer == true ||
                model.Question6Answer == true)
            {
                return "Moderate";
            }

            // ================= Low Risk =================

            if (model.Question1Answer == true ||
                model.Question2Answer == true)
            {
                return "Low";
            }

            return "No Risk Identified";
        }


        // =====================================================
        // C-SSRS PROJECT SEVERITY
        // =====================================================

        private string GetCSSRSProjectSeverity(
            string? riskLevel)
        {
            if (string.IsNullOrWhiteSpace(
                riskLevel))
            {
                return "Normal";
            }


            if (riskLevel == "Moderate")
            {
                return "Moderate";
            }


            if (riskLevel == "High")
            {
                return "Severe";
            }


            return "Normal";
        }


        // =====================================================
        // AI CHAT
        // =====================================================

        // ================= AI Chat GET =================

        [HttpGet]
        public async Task<IActionResult> AIChat()
        {
            // ================= Check Student Session =================

            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );

            if (studentId == null)
            {
                return RedirectToAction(
                    "Login"
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
                    "Login"
                );
            }

            // ================= Get Active Chat Session =================

            var chatSession =
                await _context.ChatSessions
                    .Where(
                        s =>
                            s.StudentId ==
                            studentId.Value &&
                            s.IsActive
                    )
                    .OrderByDescending(
                        s => s.StartedAt
                    )
                    .FirstOrDefaultAsync();

            // ================= Create Session =================

            if (chatSession == null)
            {
                chatSession =
                    new ChatSession
                    {
                        StudentId =
                            studentId.Value,

                        StartedAt =
                            DateTime.Now,

                        IsActive =
                            true,

                        Summary =
                            string.Empty
                    };

                _context.ChatSessions.Add(
                    chatSession
                );

                await _context.SaveChangesAsync();
            }

            // ================= Get Messages =================

            var messages =
                await _context.ChatMessages
                    .Where(
                        m =>
                            m.ChatSessionId ==
                            chatSession.ChatSessionId
                    )
                    .OrderBy(
                        m => m.CreatedAt
                    )
                    .ToListAsync();

            // ================= Latest Assessment =================

            var latestRisk =
                await _context
                    .ChatRiskAssessments
                    .Where(
                        r =>
                            r.StudentId ==
                            studentId.Value
                    )
                    .OrderByDescending(
                        r => r.CreatedAt
                    )
                    .FirstOrDefaultAsync();

            // ================= Send Data To View =================

            ViewBag.StudentName =
                student.FullName;

            ViewBag.ChatSessionId =
                chatSession.ChatSessionId;

            ViewBag.ChatMessages =
                messages;

            ViewBag.ChatRiskStatus =
                latestRisk?.RiskStatus
                ?? "Not Assessed";

            return View();
        }

        // =====================================================
        // SEND AI MESSAGE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendAIMessage(
            string message)
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
                            "Your session has expired. Please login again."
                    }
                );
            }

            // ================= Validate Message =================

            if (string.IsNullOrWhiteSpace(
                message))
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Please write a message."
                    }
                );
            }

            message =
                message.Trim();

            if (message.Length > 2000)
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Message is too long."
                    }
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
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "Student account was not found."
                    }
                );
            }

            // ================= Get Active Session =================

            var chatSession =
                await _context.ChatSessions
                    .Where(
                        s =>
                            s.StudentId ==
                            studentId.Value &&
                            s.IsActive
                    )
                    .OrderByDescending(
                        s => s.StartedAt
                    )
                    .FirstOrDefaultAsync();

            // ================= Create Session =================

            if (chatSession == null)
            {
                chatSession =
                    new ChatSession
                    {
                        StudentId =
                            studentId.Value,

                        StartedAt =
                            DateTime.Now,

                        IsActive =
                            true,

                        Summary =
                            string.Empty
                    };

                _context.ChatSessions.Add(
                    chatSession
                );

                await _context.SaveChangesAsync();
            }

            // =====================================================
            // LOAD CURRENT CHAT MEMORY
            // =====================================================

            var recentMessages =
                await _context.ChatMessages
                    .Where(
                        m =>
                            m.ChatSessionId ==
                            chatSession.ChatSessionId
                    )
                    .OrderByDescending(
                        m => m.CreatedAt
                    )
                    .Take(20)
                    .OrderBy(
                        m => m.CreatedAt
                    )
                    .ToListAsync();

            // =====================================================
            // LOAD PREVIOUS ASSESSMENT CONTEXT
            // =====================================================

            var previousRisk =
                await _context
                    .ChatRiskAssessments
                    .Where(
                        r =>
                            r.StudentId ==
                            studentId.Value
                    )
                    .OrderByDescending(
                        r => r.CreatedAt
                    )
                    .FirstOrDefaultAsync();

            // ================= Save Student Message =================

            var studentMessage =
                new ChatMessage
                {
                    ChatSessionId =
                        chatSession.ChatSessionId,

                    Sender =
                        "Student",

                    MessageText =
                        message,

                    CreatedAt =
                        DateTime.Now
                };

            _context.ChatMessages.Add(
                studentMessage
            );

            await _context.SaveChangesAsync();

            try
            {
                // =====================================================
                // SEND MESSAGE + MEMORY TO GEMINI
                // =====================================================

                var aiResult =
                    await _geminiChatService
                        .SendMessageAsync(
                            student.FullName,
                            recentMessages,
                            message,
                            chatSession.Summary,
                            previousRisk?.RiskStatus,
                            previousRisk?.Summary
                        );

                // ================= Validate AI Reply =================

                if (string.IsNullOrWhiteSpace(
                    aiResult.Reply))
                {
                    aiResult.Reply =
                        "I am here to listen. Please tell me a little more about how you are feeling.";
                }

                // ================= Normalize Risk =================

                string riskStatus =
                    NormalizeChatRiskStatus(
                        aiResult.RiskStatus
                    );

                // ================= Save AI Reply =================

                var aiMessage =
                    new ChatMessage
                    {
                        ChatSessionId =
                            chatSession.ChatSessionId,

                        Sender =
                            "AI",

                        MessageText =
                            aiResult.Reply.Trim(),

                        CreatedAt =
                            DateTime.Now
                    };

                _context.ChatMessages.Add(
                    aiMessage
                );

                // ================= Save Risk Assessment =================

                var riskAssessment =
                    new ChatRiskAssessment
                    {
                        ChatSessionId =
                            chatSession.ChatSessionId,

                        StudentId =
                            student.StudentId,

                        RiskStatus =
                            riskStatus,

                        Summary =
                            aiResult.AssessmentSummary
                            ?? string.Empty,

                        CreatedAt =
                            DateTime.Now
                    };

                _context.ChatRiskAssessments.Add(
                    riskAssessment
                );

                // ================= Update Conversation Memory =================

                if (!string.IsNullOrWhiteSpace(
                    aiResult.ConversationSummary))
                {
                    chatSession.Summary =
                        aiResult
                            .ConversationSummary
                            .Trim();
                }

                // ================= Update Student Latest Status =================

                student.LatestChatRiskStatus =
                    riskStatus;

                student.LatestChatRiskUpdatedAt =
                    DateTime.Now;

                // ================= Save Everything =================

                await _context.SaveChangesAsync();


                // =====================================================
                // AUTO PSYCHOLOGIST ASSIGNMENT
                // =====================================================

                if (riskStatus == "Severe" ||
                    riskStatus == "Extremely Severe")
                {
                    try
                    {
                        await _counselingSchedulerService
                            .AutoAssignPsychologistAsync(
                                student.StudentId,
                                riskStatus,
                                "AI Chat"
                            );
                    }
                    catch
                    {
                        // AI chat response should continue
                        // even if appointment scheduling fails.
                    }
                }


                // ================= Return Result =================

                return Json(
                    new
                    {
                        success = true,

                        reply =
                            aiResult.Reply,

                        riskStatus =
                            riskStatus,

                        createdAt =
                            DateTime.Now.ToString(
                                "hh:mm tt"
                            )
                    }
                );
            }
            catch (Exception)
            {
                return Json(
                    new
                    {
                        success = false,

                        message =
                            "The support assistant could not respond right now. Please try again."
                    }
                );
            }
        }

        // =====================================================
        // START NEW AI CHAT
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartNewAIChat()
        {
            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );

            if (studentId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }

            // ================= Close Active Sessions =================

            var activeSessions =
                await _context.ChatSessions
                    .Where(
                        s =>
                            s.StudentId ==
                            studentId.Value &&
                            s.IsActive
                    )
                    .ToListAsync();

            foreach (var session
                in activeSessions)
            {
                session.IsActive =
                    false;

                session.EndedAt =
                    DateTime.Now;
            }

            // ================= Create New Session =================

            var newSession =
                new ChatSession
                {
                    StudentId =
                        studentId.Value,

                    StartedAt =
                        DateTime.Now,

                    IsActive =
                        true,

                    Summary =
                        string.Empty
                };

            _context.ChatSessions.Add(
                newSession
            );

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "AIChat"
            );
        }

        // =====================================================
        // NORMALIZE CHATBOT ASSESSMENT
        // =====================================================

        private string NormalizeChatRiskStatus(
            string? riskStatus)
        {
            if (string.IsNullOrWhiteSpace(
                riskStatus))
            {
                return "Normal";
            }

            var status =
                riskStatus
                    .Trim()
                    .ToLowerInvariant();

            // ================= Normal =================

            if (status == "normal" ||
                status == "stable")
            {
                return "Normal";
            }

            // ================= Moderate =================

            if (status == "moderate" ||
                status == "stress" ||
                status == "stressed" ||
                status == "possible stress")
            {
                return "Moderate";
            }

            // ================= Severe =================

            if (status == "severe" ||
                status == "depressed" ||
                status == "possible depression" ||
                status == "depressive signs")
            {
                return "Severe";
            }

            // ================= Extremely Severe =================

            if (status == "extremely severe" ||
                status == "possible high risk" ||
                status == "high risk" ||
                status == "elevated risk")
            {
                return "Extremely Severe";
            }

            return "Normal";
        }

        // =====================================================
        // APPOINTMENT
        // =====================================================

        // ================= Session History Helper for Counseling Session Page =================
        private async Task<StudentSessionHistoryViewModel> GetStudentSessionHistoryViewModelAsync(int studentId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            var counselings = await _context.Counselings
                .Include(c => c.Psychologist)
                .Where(c => c.StudentId == studentId)
                .OrderByDescending(c => c.CounselingDate)
                .ThenByDescending(c => c.AppointmentTime)
                .ToListAsync();

            var observations = await _context.CounselingObservations
                .Where(o => o.StudentId == studentId)
                .ToListAsync();

            var observationReports = await _context.ObservationReports
                .Where(r => r.StudentId == studentId)
                .ToListAsync();

            var sessionItems = new List<StudentSessionHistoryItemViewModel>();

            foreach (var c in counselings)
            {
                var obs = observations.FirstOrDefault(o => o.CounselingId == c.CounselingId);
                var rep = observationReports.FirstOrDefault(r => r.RootCounselingId == c.CounselingId ||
                    (obs != null && r.RootCounselingId == obs.RootCounselingId));

                sessionItems.Add(new StudentSessionHistoryItemViewModel
                {
                    CounselingId = c.CounselingId,
                    CounselingDate = c.CounselingDate,
                    AppointmentTime = c.AppointmentTime,
                    AppointmentEndTime = c.AppointmentEndTime,
                    Status = c.Status,
                    AppointmentRoom = c.AppointmentRoom,
                    AppointmentSource = c.AppointmentSource,
                    TriggerSource = c.TriggerSource,
                    TriggerSeverity = c.TriggerSeverity,
                    ObservationNote = c.Observation,
                    CanCancel = false,
                    PsychologistId = c.PsychologistId,
                    PsychologistName = c.Psychologist?.FullName ?? "University Psychologist",
                    PsychologistSpecialization = c.Psychologist?.Specialization,
                    PsychologistEmail = c.Psychologist?.Email,
                    PsychologistProfileImage = c.Psychologist?.ProfileImage,
                    Observation = obs,
                    ObservationReport = rep
                });
            }

            return new StudentSessionHistoryViewModel
            {
                Student = student ?? new Student(),
                Sessions = sessionItems
            };
        }

        // ================= Appointment GET =================

        [HttpGet]
        public async Task<IActionResult> Appointment()
        {
            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );

            if (studentId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }

            var currentStudent = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId.Value);
            if (currentStudent == null || currentStudent.IsSuspended)
            {
                TempData["Error"] = "Your student account is currently suspended. You cannot schedule counseling appointments.";
                return RedirectToAction("Dashboard");
            }

            // Automatically transition any expired unassessed appointments to Missed
            await CounselingSchedulerService.UpdateMissedAppointmentsAsync(_context);

            var now = DateTime.Now;

            // Check if student has completed both screening assessments (PHQ-9 and C-SSRS)
            bool hasCompletedPHQ = await _context.PHQAssessments.AnyAsync(p => p.StudentId == studentId.Value);
            bool hasCompletedCSSRS = await _context.CSSRSAssessments.AnyAsync(c => c.StudentId == studentId.Value);
            bool hasCompletedScreening = hasCompletedPHQ && hasCompletedCSSRS;

            ViewBag.HasCompletedPHQ = hasCompletedPHQ;
            ViewBag.HasCompletedCSSRS = hasCompletedCSSRS;
            ViewBag.HasCompletedScreening = hasCompletedScreening;

            // Load complete session history & observation reports
            ViewBag.SessionHistory = await GetStudentSessionHistoryViewModelAsync(studentId.Value);

            // Check if student already has an active, pending, or scheduled appointment with an active psychologist
            var activeAppointment = await _context.Counselings
                .Include(c => c.Psychologist)
                .Where(c => c.StudentId == studentId.Value &&
                            c.Status != "Completed" &&
                            c.Status != "Cancelled" &&
                            c.Status != "Missed" &&
                            c.Psychologist != null &&
                            !c.Psychologist.IsSuspended &&
                            (c.CounselingDate.Date > DateTime.Today ||
                            (c.CounselingDate.Date == DateTime.Today && c.AppointmentEndTime >= now.TimeOfDay)))
                .OrderByDescending(c => c.CounselingDate)
                .ThenByDescending(c => c.AppointmentTime)
                .FirstOrDefaultAsync();

            ViewBag.ActiveAppointment = activeAppointment;
            ViewBag.CanCancel = false;

            var model =
                new AppointmentViewModel
                {
                    PreferredDate =
                        DateTime.Today
                };

            return View(
                model
            );
        }

        /// ================= Appointment POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Appointment(
            AppointmentViewModel model)
        {
            // ================= Check Student Session =================

            var studentId =
                HttpContext.Session.GetInt32(
                    "StudentId"
                );

            if (studentId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }

            var currentStudent = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId.Value);
            if (currentStudent == null || currentStudent.IsSuspended)
            {
                TempData["Error"] = "Your student account is currently suspended. You cannot schedule counseling appointments.";
                return RedirectToAction("Dashboard");
            }

            // Automatically transition any expired unassessed appointments to Missed
            await CounselingSchedulerService.UpdateMissedAppointmentsAsync(_context);

            // Load complete session history & observation reports
            ViewBag.SessionHistory = await GetStudentSessionHistoryViewModelAsync(studentId.Value);

            // Check if student has completed both screening assessments (PHQ-9 and C-SSRS)
            bool hasCompletedPHQ = await _context.PHQAssessments.AnyAsync(p => p.StudentId == studentId.Value);
            bool hasCompletedCSSRS = await _context.CSSRSAssessments.AnyAsync(c => c.StudentId == studentId.Value);
            bool hasCompletedScreening = hasCompletedPHQ && hasCompletedCSSRS;

            ViewBag.HasCompletedPHQ = hasCompletedPHQ;
            ViewBag.HasCompletedCSSRS = hasCompletedCSSRS;
            ViewBag.HasCompletedScreening = hasCompletedScreening;

            var now = DateTime.Now;

            // Check if student already has an active, pending, or scheduled appointment with an active psychologist
            var activeAppointment = await _context.Counselings
                .Include(c => c.Psychologist)
                .Where(c => c.StudentId == studentId.Value &&
                            c.Status != "Completed" &&
                            c.Status != "Cancelled" &&
                            c.Status != "Missed" &&
                            c.Psychologist != null &&
                            !c.Psychologist.IsSuspended &&
                            (c.CounselingDate.Date > DateTime.Today ||
                            (c.CounselingDate.Date == DateTime.Today && c.AppointmentEndTime >= now.TimeOfDay)))
                .OrderByDescending(c => c.CounselingDate)
                .ThenByDescending(c => c.AppointmentTime)
                .FirstOrDefaultAsync();

            if (activeAppointment != null)
            {
                bool canCancel = activeAppointment.CounselingDate.Date > DateTime.Today ||
                    (activeAppointment.CounselingDate.Date == DateTime.Today && activeAppointment.AppointmentTime > now.TimeOfDay);

                ViewBag.ActiveAppointment = activeAppointment;
                ViewBag.CanCancel = canCancel;
                TempData["Error"] = "Appointment already scheduled! You cannot request a new appointment while an active or pending appointment exists.";
                ModelState.AddModelError(
                    "",
                    "Appointment already scheduled! You already have an active counseling appointment."
                );

                return View(model);
            }


            // ================= Allowed Working Days =================

            var allowedDays = new[]
            {
                DayOfWeek.Saturday,
                DayOfWeek.Sunday,
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday
            };


            // ================= Fixed Counseling Slots =================

            var allowedStartTimes = new[]
            {
                new TimeSpan(8, 30, 0),
                new TimeSpan(9, 35, 0),
                new TimeSpan(10, 40, 0),
                new TimeSpan(11, 45, 0),
                new TimeSpan(13, 10, 0),
                new TimeSpan(14, 15, 0),
                new TimeSpan(15, 20, 0),
                new TimeSpan(16, 25, 0)
            };


            // ================= Automatic End Time =================

            var endTime =
                model.StartTime.Add(
                    TimeSpan.FromHours(1)
                );


            // ================= Date Validation =================

            if (model.PreferredDate.Date <
                DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(
                        model.PreferredDate
                    ),
                    "Please select today or a future date."
                );
            }


            // ================= Working Day Validation =================

            if (!allowedDays.Contains(
                model.PreferredDate.DayOfWeek))
            {
                ModelState.AddModelError(
                    nameof(
                        model.PreferredDate
                    ),
                    "Counseling is available only from Saturday to Wednesday."
                );
            }


            // ================= Fixed Time Validation =================

            if (!allowedStartTimes.Contains(
                model.StartTime))
            {
                ModelState.AddModelError(
                    nameof(
                        model.StartTime
                    ),
                    "Please select a valid counseling time."
                );
            }


            // ================= Past Time Validation For Today =================

            if (model.PreferredDate.Date == DateTime.Today && model.StartTime < DateTime.Now.TimeOfDay)
            {
                ModelState.AddModelError(
                    nameof(
                        model.StartTime
                    ),
                    "Cannot select a past time slot for today. Please select a future time slot."
                );
            }


            if (!ModelState.IsValid)
            {
                return View(
                    model
                );
            }


            // ================= Student Double Booking =================

            var studentAlreadyBooked =
                await _context.Counselings
                    .Include(c => c.Psychologist)
                    .AnyAsync(
                        c =>
                            c.StudentId ==
                                studentId.Value &&

                            c.CounselingDate.Date ==
                                model.PreferredDate.Date &&

                            c.Status !=
                                "Cancelled" &&

                            c.Status !=
                                "Missed" &&

                            c.Psychologist != null &&
                            !c.Psychologist.IsSuspended &&

                            model.StartTime <
                                c.AppointmentEndTime &&

                            endTime >
                                c.AppointmentTime
                    );


            if (studentAlreadyBooked)
            {
                ModelState.AddModelError(
                    "",
                    "You already have another counseling appointment during this time."
                );

                return View(
                    model
                );
            }


            // ================= Get Psychologists (Only Active / Non-Suspended) =================

            var psychologists =
                await _context.Psychologists
                    .Where(p => !p.IsSuspended)
                    .ToListAsync();


            if (!psychologists.Any())
            {
                ModelState.AddModelError(
                    "",
                    "No active psychologist is currently available."
                );

                return View(
                    model
                );
            }


            // ================= Find Free Psychologists =================

            var availablePsychologists =
                new List<Psychologist>();


            foreach (var psychologist
                in psychologists)
            {
                // Psychologist is considered free by default.
                // Existing appointment = unavailable.

                var psychologistAlreadyBooked =
                    await _context.Counselings
                        .AnyAsync(
                            c =>
                                c.PsychologistId ==
                                    psychologist
                                        .PsychologistId &&

                                c.CounselingDate.Date ==
                                    model.PreferredDate.Date &&

                                c.Status !=
                                    "Cancelled" &&

                                model.StartTime <
                                    c.AppointmentEndTime &&

                                endTime >
                                    c.AppointmentTime
                        );


                if (psychologistAlreadyBooked)
                {
                    continue;
                }


                availablePsychologists.Add(
                    psychologist
                );
            }


            // =====================================================
            // SELECTED TIME FULL
            // FIND OTHER FREE TIMES
            // =====================================================

            if (!availablePsychologists.Any())
            {
                var suggestedTimes =
                    new List<TimeSpan>();


                foreach (var suggestedStartTime
                    in allowedStartTimes)
                {
                    // Selected time again suggest করবে না

                    if (suggestedStartTime ==
                        model.StartTime)
                    {
                        continue;
                    }


                    // ================= Suggested End Time =================

                    var suggestedEndTime =
                        suggestedStartTime.Add(
                            TimeSpan.FromHours(1)
                        );


                    // ================= Student Conflict =================

                    var studentBookedAtSuggestedTime =
                        await _context.Counselings
                            .AnyAsync(
                                c =>
                                    c.StudentId ==
                                        studentId.Value &&

                                    c.CounselingDate.Date ==
                                        model.PreferredDate.Date &&

                                    c.Status !=
                                        "Cancelled" &&

                                    suggestedStartTime <
                                        c.AppointmentEndTime &&

                                    suggestedEndTime >
                                        c.AppointmentTime
                            );


                    if (studentBookedAtSuggestedTime)
                    {
                        continue;
                    }


                    // ================= Check Any Psychologist =================

                    bool psychologistFound =
                        false;


                    foreach (var psychologist
                        in psychologists)
                    {
                        var psychologistBookedAtSuggestedTime =
                            await _context.Counselings
                                .AnyAsync(
                                    c =>
                                        c.PsychologistId ==
                                            psychologist
                                                .PsychologistId &&

                                        c.CounselingDate.Date ==
                                            model.PreferredDate.Date &&

                                        c.Status !=
                                            "Cancelled" &&

                                        suggestedStartTime <
                                            c.AppointmentEndTime &&

                                        suggestedEndTime >
                                            c.AppointmentTime
                                );


                        if (!psychologistBookedAtSuggestedTime)
                        {
                            psychologistFound =
                                true;

                            break;
                        }
                    }


                    if (psychologistFound)
                    {
                        suggestedTimes.Add(
                            suggestedStartTime
                        );
                    }
                }


                // ================= Send Suggestions =================

                model.SuggestedTimes =
                    suggestedTimes;


                if (suggestedTimes.Any())
                {
                    model.Message =
                        "All psychologists are busy at your selected time. Please choose one of the suggested free times below.";
                }
                else
                {
                    model.Message =
                        "All psychologists are booked for this date. Please choose another date.";
                }


                return View(
                    model
                );
            }


            // =====================================================
            // PSYCHOLOGIST PRIORITY
            // =====================================================
            // 1. Lowest Appointment Count
            // 2. Same Count = Alphabetical Name
            // =====================================================

            Psychologist? selectedPsychologist =
                null;


            int lowestAppointmentCount =
                int.MaxValue;


            foreach (var psychologist
                in availablePsychologists)
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


                // ================= Lower Count =================

                if (appointmentCount <
                    lowestAppointmentCount)
                {
                    lowestAppointmentCount =
                        appointmentCount;


                    selectedPsychologist =
                        psychologist;
                }


                // ================= Same Count =================

                else if (appointmentCount ==
                         lowestAppointmentCount)
                {
                    if (selectedPsychologist == null ||
                        string.Compare(
                            psychologist.FullName,
                            selectedPsychologist.FullName,
                            StringComparison
                                .OrdinalIgnoreCase
                        ) < 0)
                    {
                        selectedPsychologist =
                            psychologist;
                    }
                }
            }


            // ================= Final Check =================

            if (selectedPsychologist == null)
            {
                ModelState.AddModelError(
                    "",
                    "No psychologist could be assigned."
                );

                return View(
                    model
                );
            }


            // ================= Create Appointment =================

            var counseling =
                new Counseling
                {
                    StudentId =
                        studentId.Value,

                    PsychologistId =
                        selectedPsychologist
                            .PsychologistId,

                    CounselingDate =
                        model.PreferredDate.Date,

                    AppointmentTime =
                        model.StartTime,

                    AppointmentEndTime =
                        endTime,

                    Observation =
                        string.Empty,

                    Assessment =
                        string.Empty,

                    Recommendation =
                        string.Empty,

                    RiskLevel =
                        string.Empty,

                    Status =
                        "Confirmed",

                    AppointmentSource =
                        "StudentRequest",

                    AppointmentRoom =
                        "Mental Health & Counseling Center, Room 402",

                    CreatedAt =
                        DateTime.Now
                };


            // ================= Save =================

            _context.Counselings.Add(
                counseling
            );


            await _context.SaveChangesAsync();


            // ================= Create / Update Combined Screening Report =================

            try
            {
                await _counselingSchedulerService
                    .CreateOrUpdateCombinedScreeningReportAsync(
                        counseling,
                        "StudentRequest",
                        "Self-Requested"
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StudentController] Failed to generate screening report: {ex.Message}");
            }


            // ================= Send Confirmation Email =================

            try
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

                if (student != null && !student.IsSuspended && !string.IsNullOrWhiteSpace(student.Email))
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
                        appointmentSource: "StudentRequest",
                        severityOrReason: "Self-Requested Counseling Session"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StudentController] Failed to send appointment email: {ex.Message}");
            }


            // ================= Success =================

            TempData["Success"] =
                $"Your counseling appointment has been confirmed with {selectedPsychologist.FullName}. Time: {DateTime.Today.Add(model.StartTime):h:mm tt} - {DateTime.Today.Add(endTime):h:mm tt}.";


            return RedirectToAction(
                "Appointment"
            );
        }

        // =========================================================
        // CANCEL APPOINTMENT (STUDENT)
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelAppointment(int id, string? reason)
        {
            TempData["Error"] = "Students are not permitted to cancel counseling appointments directly. Appointments can only be cancelled by your registered guardian via the Guardian Portal, or by visiting the Counseling Center.";
            return RedirectToAction("Appointment");
        }

        // =====================================================
        // PROGRESS (INDIVIDUAL STUDENT PROGRESS REPORT)
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Progress()
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            List<ObservationReport> reports = new List<ObservationReport>();
            try
            {
                reports = await _context.ObservationReports
                    .Include(r => r.Student)
                    .Include(r => r.Psychologist)
                    .Where(r => r.StudentId == studentId.Value)
                    .OrderByDescending(r => r.UpdatedAt)
                    .ToListAsync();
            }
            catch (Exception)
            {
                reports = new List<ObservationReport>();
            }

            var progressVms = new List<StudentProgressReportDetailViewModel>();
            var processedRootIds = new HashSet<int>();

            foreach (var report in reports)
            {
                processedRootIds.Add(report.RootCounselingId);

                List<CounselingObservation> obsList = new List<CounselingObservation>();
                try
                {
                    obsList = await _context.CounselingObservations
                        .Include(o => o.Counseling)
                        .Where(o => o.RootCounselingId == report.RootCounselingId)
                        .OrderBy(o => o.Counseling!.CounselingDate)
                        .ThenBy(o => o.Counseling!.AppointmentTime)
                        .ToListAsync();
                }
                catch (Exception)
                {
                    obsList = new List<CounselingObservation>();
                }

                var vm = ProgressScoringService.BuildDetailViewModel(report, obsList);
                progressVms.Add(vm);
            }

            // Fallback for counselings without ObservationReport yet
            var counselings = await _context.Counselings
                .Include(c => c.Psychologist)
                .Where(c => c.StudentId == studentId.Value)
                .OrderBy(c => c.CounselingDate)
                .ThenBy(c => c.AppointmentTime)
                .ToListAsync();

            var unmappedCounselings = counselings
                .Where(c => !processedRootIds.Contains(c.CounselingId))
                .ToList();

            if (unmappedCounselings.Any() && student != null)
            {
                var dummyReport = new ObservationReport
                {
                    ObservationReportId = 0,
                    RootCounselingId = unmappedCounselings.First().CounselingId,
                    StudentId = student.StudentId,
                    Student = student,
                    PsychologistId = unmappedCounselings.First().PsychologistId,
                    Psychologist = unmappedCounselings.First().Psychologist,
                    IsFinal = false,
                    CreatedAt = unmappedCounselings.First().CounselingDate,
                    UpdatedAt = unmappedCounselings.Last().CounselingDate
                };

                var dummyObsList = await _context.CounselingObservations
                    .Include(o => o.Counseling)
                    .Where(o => o.StudentId == student.StudentId)
                    .OrderBy(o => o.Counseling!.CounselingDate)
                    .ThenBy(o => o.Counseling!.AppointmentTime)
                    .ToListAsync();

                var fallbackVm = ProgressScoringService.BuildDetailViewModel(dummyReport, dummyObsList);
                progressVms.Add(fallbackVm);
            }

            return View(progressVms);
        }

        // =====================================================
        // OBSERVATION DETAILS (INDIVIDUAL REPORT VIEW FOR STUDENT)
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ObservationDetails(int id)
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            var counseling = await _context.Counselings
                .Include(c => c.Student)
                .Include(c => c.Psychologist)
                .FirstOrDefaultAsync(c => c.CounselingId == id && c.StudentId == studentId.Value);

            if (counseling == null)
            {
                TempData["Error"] = "Session record not found.";
                return RedirectToAction("Appointment");
            }

            var observation = await _context.CounselingObservations
                .FirstOrDefaultAsync(o => o.CounselingId == counseling.CounselingId);

            var observationReport = await _context.ObservationReports
                .FirstOrDefaultAsync(r => r.RootCounselingId == counseling.CounselingId ||
                    (observation != null && r.RootCounselingId == observation.RootCounselingId));

            ViewBag.Counseling = counseling;
            ViewBag.ObservationReport = observationReport;

            return View(observation);
        }

        // =====================================================
        // REPORTS
        // =====================================================

        public IActionResult Reports()
        {
            return View();
        }

        // =====================================================
        // SEMESTER SCREENING CLEARANCE REPORT
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ScreeningClearance(string? semester)
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students.FindAsync(studentId.Value);
            if (student == null)
            {
                return RedirectToAction("Login");
            }

            var availableSemestersFromPHQ = await _context.PHQAssessments
                .Where(p => p.StudentId == student.StudentId && !string.IsNullOrEmpty(p.Semester))
                .Select(p => p.Semester!)
                .Distinct()
                .ToListAsync();

            var availableSemestersFromCSSRS = await _context.CSSRSAssessments
                .Where(c => c.StudentId == student.StudentId && !string.IsNullOrEmpty(c.Semester))
                .Select(c => c.Semester!)
                .Distinct()
                .ToListAsync();

            var availableSemesters = availableSemestersFromPHQ
                .Union(availableSemestersFromCSSRS)
                .Distinct()
                .OrderByDescending(s => s)
                .ToList();

            if (!availableSemesters.Contains(student.ActiveSemester))
            {
                availableSemesters.Insert(0, student.ActiveSemester);
            }

            var currentSem = string.IsNullOrWhiteSpace(semester) ? student.ActiveSemester : semester.Trim();

            var phq = await _context.PHQAssessments
                .Where(p => p.StudentId == student.StudentId && p.Semester == currentSem)
                .OrderByDescending(p => p.AssessmentDate)
                .FirstOrDefaultAsync();

            var cssrs = await _context.CSSRSAssessments
                .Where(c => c.StudentId == student.StudentId && c.Semester == currentSem)
                .OrderByDescending(c => c.AssessmentDate)
                .FirstOrDefaultAsync();

            var screeningEval = Services.ScreeningComplianceService.Evaluate(
                hasPHQ: phq != null,
                phqSeverity: phq?.SeverityLevel,
                phqScore: phq?.TotalScore,
                hasCSSRS: cssrs != null,
                cssrsRiskLevel: cssrs?.RiskLevel
            );

            bool isCleared = screeningEval.IsScreeningComplete;
            string remarks = isCleared
                ? (phq != null && cssrs != null
                    ? "All mandatory screening evaluations for this semester have been completed. Your screening record is fully compliant."
                    : (screeningEval.IsPHQSevere
                        ? "PHQ-9 screening evaluated with clinical severity indicator. Case is forwarded for clinical counseling care."
                        : "C-SSRS screening evaluated with clinical safety indicator. Case is forwarded for clinical counseling care."))
                : screeningEval.WarningReason;

            var model = new StudentScreeningClearanceViewModel
            {
                StudentId = student.StudentId,
                FullName = student.FullName,
                StudentIdNumber = !string.IsNullOrWhiteSpace(student.StudentIdNumber) ? student.StudentIdNumber : "-",
                Department = student.Department ?? "General",
                SelectedSemester = currentSem,
                AvailableSemesters = availableSemesters,
                CheckedDate = DateTime.Now,
                HasCompletedPHQ = phq != null,
                PHQCompletionDate = phq?.AssessmentDate,
                PHQSeverityLevel = phq?.SeverityLevel ?? "Not Completed",
                HasCompletedCSSRS = cssrs != null,
                CSSRSCompletionDate = cssrs?.AssessmentDate,
                CSSRSRiskLevel = cssrs?.RiskLevel ?? "Not Completed",
                AdministrativeRemarks = remarks
            };

            return View(model);
        }

        // =====================================================
        // STUDENT PROFILE - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
            {
                return RedirectToAction("Login");
            }

            var vm = new StudentProfileViewModel
            {
                StudentId = student.StudentId,
                StudentIdNumber = student.StudentIdNumber,
                FullName = student.FullName,
                Email = student.Email,
                Phone = student.Phone,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender,
                Department = student.Department,
                AdmissionYear = student.AdmissionYear,
                Semester = student.Semester,
                Height = student.Height,
                Weight = student.Weight,
                FinancialCondition = student.FinancialCondition,
                GuardianName = student.GuardianName,
                Relationship = student.Relationship,
                GuardianPhone = student.GuardianPhone,
                GuardianEmail = student.GuardianEmail,
                ProfileImage = student.ProfileImage
            };

            return View(vm);
        }

        // =====================================================
        // STUDENT PROFILE - POST (EDIT INFO & PASSWORD)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(StudentProfileViewModel model)
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
            {
                return RedirectToAction("Login");
            }

            // Validate Unique Email if changed
            if (!string.IsNullOrWhiteSpace(model.Email) && !string.Equals(model.Email.Trim(), student.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailExists = await _context.Students
                    .AnyAsync(s => s.StudentId != student.StudentId && s.Email.ToLower() == model.Email.Trim().ToLower());

                if (emailExists)
                {
                    ModelState.AddModelError("Email", "This email address is already in use by another student account.");
                }
            }

            // Validate Date of Birth (No future date)
            if (model.DateOfBirth.HasValue && model.DateOfBirth.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError("DateOfBirth", "Date of birth cannot be in the future. Please select a valid birth date.");
            }

            // Password update handling
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is required to set a new password.");
                }
                else
                {
                    bool isCurrentValid = false;
                    try
                    {
                        if (!string.IsNullOrEmpty(student.Password))
                        {
                            isCurrentValid = BCrypt.Net.BCrypt.Verify(model.CurrentPassword, student.Password);
                        }
                    }
                    catch
                    {
                        isCurrentValid = (student.Password == model.CurrentPassword);
                    }

                    if (!isCurrentValid && student.Password == model.CurrentPassword)
                    {
                        isCurrentValid = true;
                    }

                    if (!isCurrentValid)
                    {
                        ModelState.AddModelError("CurrentPassword", "The current password you entered is incorrect.");
                    }
                }

                if (model.NewPassword.Length < 8)
                {
                    ModelState.AddModelError("NewPassword", "New password must be at least 8 characters long.");
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(model.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$"))
                {
                    ModelState.AddModelError("NewPassword", "Password must contain at least 1 uppercase letter, 1 lowercase letter, 1 number, and 1 special character.");
                }

                if (model.NewPassword != model.ConfirmNewPassword)
                {
                    ModelState.AddModelError("ConfirmNewPassword", "New passwords do not match.");
                }
            }

            // Image Upload handling
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
                    var uploadFolder = Path.Combine(_environment.WebRootPath, "images", "students");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var fullPath = Path.Combine(uploadFolder, fileName);

                    await using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }

                    student.ProfileImage = $"/images/students/{fileName}";
                    HttpContext.Session.SetString("StudentProfileImage", student.ProfileImage);
                }
            }

            if (!ModelState.IsValid)
            {
                model.ProfileImage = student.ProfileImage;
                model.StudentIdNumber = student.StudentIdNumber;
                return View(model);
            }

            // Update editable student properties (Department, AdmissionYear and Semester cannot be modified by the student)
            student.FullName = model.FullName.Trim();
            student.Email = model.Email.Trim();
            student.Phone = model.Phone.Trim();
            student.DateOfBirth = model.DateOfBirth;
            student.Gender = model.Gender;
            // Department, AdmissionYear and Semester are locked for students and strictly preserved from existing records
            student.Semester = student.ActiveSemester;
            student.Height = model.Height;
            student.Weight = model.Weight;
            student.FinancialCondition = model.FinancialCondition;

            // Guardian details
            student.GuardianName = model.GuardianName?.Trim();
            student.Relationship = model.Relationship?.Trim();
            student.GuardianPhone = model.GuardianPhone?.Trim();
            student.GuardianEmail = model.GuardianEmail?.Trim();

            // Apply new password if changed
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                student.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            }

            // Update Session details
            HttpContext.Session.SetString("StudentName", student.FullName);
            HttpContext.Session.SetString("StudentDepartment", student.Department ?? "CSE");
            HttpContext.Session.SetString("StudentSemester", student.ActiveSemester);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Your profile information and settings have been updated successfully!";
            return RedirectToAction("Profile");
        }

        // =====================================================
        // LOGOUT
        // =====================================================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Index",
                "Home"
            );
        }
    }
}