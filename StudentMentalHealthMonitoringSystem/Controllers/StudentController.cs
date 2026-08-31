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

            if (!BCrypt.Net.BCrypt.Verify(
                password,
                student.Password))
            {
                ViewBag.Error =
                    "Invalid Email or Password";

                return View();
            }

            // ================= Update / Normalize Semester =================
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

                return View(student);
            }

            // ================= Password Complexity Validation =================

            if (string.IsNullOrWhiteSpace(student.Password) || student.Password.Length < 8 ||
                !System.Text.RegularExpressions.Regex.IsMatch(student.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$"))
            {
                ModelState.AddModelError("Password", "Password must contain at least 8 characters, including 1 uppercase letter, 1 lowercase letter, 1 number, and 1 special character.");
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

            ViewBag.FeelingsCompleted =
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


            // =================================================
            // FEELINGS STATUS
            // =================================================

            var semesterRecord =
                await _context
                    .StudentSemesterRecords
                    .FirstOrDefaultAsync(
                        r =>
                            r.StudentId ==
                                student.StudentId &&

                            r.Semester ==
                                student.Semester
                    );


            bool feelingsCompleted =
                semesterRecord != null &&
                !string.IsNullOrWhiteSpace(
                    semesterRecord.FeelingText
                );


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

            bool hasPHQ = await _context.PHQAssessments
                .AnyAsync(p => p.StudentId == student.StudentId && p.Semester.ToLower() == currentSemester.ToLower());

            bool hasCSSRS = await _context.CSSRSAssessments
                .AnyAsync(c => c.StudentId == student.StudentId && c.Semester.ToLower() == currentSemester.ToLower());

            ViewBag.HasPHQ = hasPHQ;
            ViewBag.HasCSSRS = hasCSSRS;
            ViewBag.IsScreeningComplete = hasPHQ && hasCSSRS;

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

            // ================= PHQ Status =================

            ViewBag.PHQCompleted =
                await _context.PHQAssessments
                    .AnyAsync(
                        p =>
                            p.StudentId ==
                            student.StudentId &&
                            p.Semester ==
                            student.Semester
                    );

            // ================= C-SSRS Status =================

            ViewBag.CSSRSCompleted =
                await _context.CSSRSAssessments
                    .AnyAsync(
                        c =>
                            c.StudentId ==
                            student.StudentId &&
                            c.Semester ==
                            student.Semester
                    );

            ViewBag.CurrentSemester =
                student.Semester;

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
                // =================================================

                if (projectSeverity == "Severe" ||
                    projectSeverity == "Extremely Severe")
                {
                    await _counselingSchedulerService
                        .AutoAssignPsychologistAsync(
                            student.StudentId,
                            projectSeverity,
                            "PHQ-9"
                        );
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
                // =================================================

                if (projectSeverity == "Severe" ||
                    projectSeverity == "Extremely Severe")
                {
                    await _counselingSchedulerService
                        .AutoAssignPsychologistAsync(
                            student.StudentId,
                            projectSeverity,
                            "C-SSRS"
                        );
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
        // FEELINGS (REMOVED / REDIRECT TO DASHBOARD)
        // =====================================================

        [HttpGet]
        public IActionResult Feelings()
        {
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Feelings(StudentSemesterRecord model)
        {
            return RedirectToAction("Dashboard");
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

            // Check if student already has an active, pending, or scheduled appointment
            var activeAppointment = await _context.Counselings
                .Include(c => c.Psychologist)
                .Where(c => c.StudentId == studentId.Value && c.Status != "Completed" && c.Status != "Cancelled")
                .OrderByDescending(c => c.CounselingDate)
                .ThenByDescending(c => c.AppointmentTime)
                .FirstOrDefaultAsync();

            ViewBag.ActiveAppointment = activeAppointment;

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

            // Check if student already has an active, pending, or scheduled appointment
            var activeAppointment = await _context.Counselings
                .Include(c => c.Psychologist)
                .Where(c => c.StudentId == studentId.Value && c.Status != "Completed" && c.Status != "Cancelled")
                .OrderByDescending(c => c.CounselingDate)
                .ThenByDescending(c => c.AppointmentTime)
                .FirstOrDefaultAsync();

            if (activeAppointment != null)
            {
                ViewBag.ActiveAppointment = activeAppointment;
                TempData["Error"] = "Appointment already scheduled! You cannot request a new appointment while an active or pending appointment exists.";
                ModelState.AddModelError(
                    "",
                    "Appointment already scheduled! You already have an active or pending counseling appointment."
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


            if (!ModelState.IsValid)
            {
                return View(
                    model
                );
            }


            // ================= Student Double Booking =================

            var studentAlreadyBooked =
                await _context.Counselings
                    .AnyAsync(
                        c =>
                            c.StudentId ==
                                studentId.Value &&

                            c.CounselingDate.Date ==
                                model.PreferredDate.Date &&

                            c.Status !=
                                "Cancelled" &&

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


            // ================= Get Psychologists =================

            var psychologists =
                await _context.Psychologists
                    .ToListAsync();


            if (!psychologists.Any())
            {
                ModelState.AddModelError(
                    "",
                    "No psychologist account is currently available."
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


            // ================= Send Confirmation Email =================

            try
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

                if (student != null && !string.IsNullOrWhiteSpace(student.Email))
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
        // HISTORY
        // =====================================================

        public IActionResult History()
        {
            return View();
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

            bool isCleared = phq != null && cssrs != null;
            string remarks = isCleared
                ? "All mandatory screening evaluations for this semester have been completed. Your academic course registration and semester promotion hold is removed."
                : (phq == null && cssrs == null
                    ? "Pending completion of both PHQ-9 and C-SSRS assessments. Please complete both screening modules to clear registration holds."
                    : (phq == null
                        ? "Pending completion of PHQ-9 depression screening assessment."
                        : "Pending completion of C-SSRS suicide risk screening assessment."));

            var model = new StudentScreeningClearanceViewModel
            {
                StudentId = student.StudentId,
                FullName = student.FullName,
                StudentIdNumber = student.StudentIdNumber ?? $"STU-{student.StudentId}",
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

        [HttpGet]
        public IActionResult ClearanceCertificate(string? semester)
        {
            return RedirectToAction("ScreeningClearance", new { semester });
        }

        // =====================================================
        // AI TELEHEALTH & COPING HISTORY
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> AIHistory(string? type)
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            var selType = string.IsNullOrWhiteSpace(type) ? "All" : type.Trim();

            // Load Chat Sessions
            var chatSessions = await _context.ChatSessions
                .Where(s => s.StudentId == studentId.Value)
                .OrderByDescending(s => s.StartedAt)
                .ToListAsync();

            // Load Voice Bot Sessions
            var voiceSessions = await _context.VoiceBotSessions
                .Where(v => v.StudentId == studentId.Value)
                .OrderByDescending(v => v.StartedAt)
                .ToListAsync();

            var sessionList = new List<StudentAISessionItemViewModel>();

            // Process Chat Sessions
            foreach (var cs in chatSessions)
            {
                var msgCount = await _context.ChatMessages.CountAsync(m => m.ChatSessionId == cs.ChatSessionId);
                var risk = await _context.ChatRiskAssessments
                    .Where(r => r.ChatSessionId == cs.ChatSessionId)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();

                var dur = cs.EndedAt.HasValue
                    ? $"{(int)(cs.EndedAt.Value - cs.StartedAt).TotalMinutes} mins"
                    : "Ongoing / Completed";

                sessionList.Add(new StudentAISessionItemViewModel
                {
                    SessionId = cs.ChatSessionId,
                    SessionType = "AI Chat",
                    StartedAt = cs.StartedAt,
                    EndedAt = cs.EndedAt,
                    DurationText = dur,
                    TotalExchanges = msgCount,
                    RiskStatus = risk?.RiskStatus ?? "Normal",
                    Summary = !string.IsNullOrEmpty(cs.Summary) ? cs.Summary : (risk?.Summary ?? "Routine wellness companion interaction."),
                    CopingAdvice = "Deep breathing, grounding 5-4-3-2-1 technique, and daily journaling."
                });
            }

            // Process Voice Sessions
            foreach (var vs in voiceSessions)
            {
                var tCount = await _context.VoiceBotTranscripts.CountAsync(t => t.VoiceBotSessionId == vs.VoiceBotSessionId);
                var vReport = await _context.VoiceBotReports
                    .Where(r => r.VoiceBotSessionId == vs.VoiceBotSessionId)
                    .OrderByDescending(r => r.LastUpdatedAt)
                    .FirstOrDefaultAsync();

                var dur = vs.EndedAt.HasValue
                    ? $"{(int)(vs.EndedAt.Value - vs.StartedAt).TotalMinutes} mins"
                    : "Completed call";

                var status = !string.IsNullOrEmpty(vReport?.FinalStatus) ? vReport.FinalStatus : (vs.CurrentStatus ?? "Normal");
                var summary = !string.IsNullOrEmpty(vReport?.FinalSummary) ? vReport.FinalSummary : (vs.CurrentSummary ?? "Acoustic wellness check-in.");

                sessionList.Add(new StudentAISessionItemViewModel
                {
                    SessionId = vs.VoiceBotSessionId,
                    SessionType = "Voice Bot",
                    StartedAt = vs.StartedAt,
                    EndedAt = vs.EndedAt,
                    DurationText = dur,
                    TotalExchanges = tCount,
                    RiskStatus = status,
                    Summary = summary,
                    CopingAdvice = "Progressive muscle relaxation, positive self-affirmation, and regular sleep schedule."
                });
            }

            var allSorted = sessionList.OrderByDescending(s => s.StartedAt).ToList();

            if (selType == "Chat")
            {
                allSorted = allSorted.Where(s => s.SessionType == "AI Chat").ToList();
            }
            else if (selType == "Voice")
            {
                allSorted = allSorted.Where(s => s.SessionType == "Voice Bot").ToList();
            }

            var model = new StudentAIHistoryViewModel
            {
                TotalSessions = sessionList.Count,
                TotalChatSessions = chatSessions.Count,
                TotalVoiceSessions = voiceSessions.Count,
                SelectedType = selType,
                DominantEmotionalState = sessionList.Any(s => s.RiskStatus == "Severe" || s.RiskStatus == "Extremely Severe") ? "Needs Care" : "Stable",
                Sessions = allSorted
            };

            return View(model);
        }

        // =====================================================
        // AI TELEHEALTH CONVERSATION DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> AIDetails(string type, int id)
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            if (string.Equals(type, "Voice", StringComparison.OrdinalIgnoreCase))
            {
                var session = await _context.VoiceBotSessions
                    .FirstOrDefaultAsync(v => v.VoiceBotSessionId == id && v.StudentId == studentId.Value);

                if (session == null)
                {
                    return NotFound();
                }

                var report = await _context.VoiceBotReports
                    .Where(r => r.VoiceBotSessionId == id)
                    .OrderByDescending(r => r.LastUpdatedAt)
                    .FirstOrDefaultAsync();

                var transcripts = await _context.VoiceBotTranscripts
                    .Where(t => t.VoiceBotSessionId == id)
                    .OrderBy(t => t.CreatedAt)
                    .Select(t => new VoiceTranscriptItemViewModel
                    {
                        Speaker = t.Speaker,
                        TranscriptText = t.TranscriptText,
                        CreatedAt = t.CreatedAt
                    })
                    .ToListAsync();

                var vm = new StudentAIDetailsViewModel
                {
                    SessionId = session.VoiceBotSessionId,
                    SessionType = "Voice Bot",
                    StartedAt = session.StartedAt,
                    EndedAt = session.EndedAt,
                    RiskStatus = report?.FinalStatus ?? session.CurrentStatus ?? "Normal",
                    Summary = report?.FinalSummary ?? session.CurrentSummary ?? "Acoustic live consultation summary.",
                    CopingAdvice = "Keep a relaxed posture, practice 4-7-8 breathing exercises, and stay connected with campus support.",
                    VoiceTranscripts = transcripts
                };

                return View(vm);
            }
            else
            {
                var session = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.ChatSessionId == id && s.StudentId == studentId.Value);

                if (session == null)
                {
                    return NotFound();
                }

                var risk = await _context.ChatRiskAssessments
                    .Where(r => r.ChatSessionId == id)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();

                var messages = await _context.ChatMessages
                    .Where(m => m.ChatSessionId == id)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new ChatMessageItemViewModel
                    {
                        Sender = m.Sender,
                        MessageText = m.MessageText,
                        CreatedAt = m.CreatedAt
                    })
                    .ToListAsync();

                var vm = new StudentAIDetailsViewModel
                {
                    SessionId = session.ChatSessionId,
                    SessionType = "AI Chat",
                    StartedAt = session.StartedAt,
                    EndedAt = session.EndedAt,
                    RiskStatus = risk?.RiskStatus ?? "Normal",
                    Summary = session.Summary ?? risk?.Summary ?? "Confidential AI Companion session.",
                    CopingAdvice = "Focus on one manageable task at a time, take short study breaks, and maintain hydration.",
                    ChatMessages = messages
                };

                return View(vm);
            }
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

            // Update all student properties
            student.FullName = model.FullName.Trim();
            student.Email = model.Email.Trim();
            student.Phone = model.Phone.Trim();
            student.DateOfBirth = model.DateOfBirth;
            student.Gender = model.Gender;
            student.Department = model.Department;
            student.AdmissionYear = model.AdmissionYear;
            // Running semester is system-managed and cannot be manually modified by the student
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