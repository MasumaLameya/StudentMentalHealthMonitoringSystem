using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using StudentMentalHealthMonitoringSystem.Data;
using StudentMentalHealthMonitoringSystem.Models;
using System.Linq;

namespace StudentMentalHealthMonitoringSystem.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public StudentController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ================= Login =================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            var student = _context.Students
                .FirstOrDefault(s => s.Email == email);

            if (student == null)
            {
                ViewBag.Error = "Invalid Email or Password";
                return View();
            }

            if (!BCrypt.Net.BCrypt.Verify(password, student.Password))
            {
                ViewBag.Error = "Invalid Email or Password";
                return View();
            }

            HttpContext.Session.SetInt32(
                "StudentId",
                student.StudentId
            );

            HttpContext.Session.SetString(
                "StudentName",
                student.FullName
            );

            return RedirectToAction("Dashboard");
        }

        // ================= Register =================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Student student)
        {
            // Model Validation
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                ViewBag.Errors = string.Join(" | ", errors);

                return View(student);
            }

            // Duplicate Email Check
            if (_context.Students.Any(s => s.Email == student.Email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Email already exists."
                );

                return View(student);
            }

            // Duplicate Student ID Check
            if (_context.Students.Any(
                s => s.StudentIdNumber == student.StudentIdNumber))
            {
                ModelState.AddModelError(
                    "StudentIdNumber",
                    "Student ID already exists."
                );

                return View(student);
            }

            try
            {
                // Password Hash
                student.Password =
                    BCrypt.Net.BCrypt.HashPassword(student.Password);

                // ================= Upload Image =================

                if (student.ImageFile != null &&
                    student.ImageFile.Length > 0)
                {
                    // Allow only image files
                    var allowedExtensions =
                        new[] { ".jpg", ".jpeg", ".png" };

                    var extension = Path
                        .GetExtension(student.ImageFile.FileName)
                        .ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError(
                            "ImageFile",
                            "Only JPG, JPEG and PNG images are allowed."
                        );

                        return View(student);
                    }

                    var uploadFolder = Path.Combine(
                        _environment.WebRootPath,
                        "images",
                        "students"
                    );

                    // Create folder if not exists
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    var fileName =
                        $"{Guid.NewGuid()}{extension}";

                    var fullPath = Path.Combine(
                        uploadFolder,
                        fileName
                    );

                    await using var stream = new FileStream(
                        fullPath,
                        FileMode.Create
                    );

                    await student.ImageFile.CopyToAsync(stream);

                    // Save image path into database
                    student.ProfileImage =
                        $"/images/students/{fileName}";
                }

                // Save Student
                _context.Students.Add(student);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Registration Successful.";

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                return Content(ex.ToString());
            }
        }

        // ================= Student Dashboard =================

        // ================= Student Dashboard =================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // Check Session
            var studentId =
                HttpContext.Session.GetInt32("StudentId");

            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            // Get Student Information
            var student = await _context.Students
                .FirstOrDefaultAsync(
                    s => s.StudentId == studentId.Value
                );

            if (student == null)
            {
                return RedirectToAction("Login");
            }

            // Default Status
            ViewBag.PHQCompleted = false;
            ViewBag.CSSRSCompleted = false;
            ViewBag.FeelingsCompleted = false;
            ViewBag.AvailableTimeCompleted = false;
            ViewBag.ScreeningCompleted = false;
            ViewBag.RiskLevel = "Not Assessed";

            // Semester না থাকলে default status দেখাবে
            if (string.IsNullOrWhiteSpace(student.Semester))
            {
                return View(student);
            }

            // PHQ-9 Status
            bool phqCompleted =
                await _context.PHQAssessments.AnyAsync(p =>
                    p.StudentId == student.StudentId &&
                    p.Semester == student.Semester
                );

            // C-SSRS Assessment
            var cssrsAssessment =
                await _context.CSSRSAssessments
                    .FirstOrDefaultAsync(c =>
                        c.StudentId == student.StudentId &&
                        c.Semester == student.Semester
                    );

            bool cssrsCompleted =
                cssrsAssessment != null;

            // Feelings এবং Available Time
            var semesterRecord =
                await _context.StudentSemesterRecords
                    .FirstOrDefaultAsync(r =>
                        r.StudentId == student.StudentId &&
                        r.Semester == student.Semester
                    );

            bool feelingsCompleted =
                semesterRecord != null &&
                !string.IsNullOrWhiteSpace(
                    semesterRecord.FeelingText
                );

            bool availableTimeCompleted =
                semesterRecord != null &&
                !string.IsNullOrWhiteSpace(
                    semesterRecord.AvailableDay
                ) &&
                semesterRecord.StartTime.HasValue &&
                semesterRecord.EndTime.HasValue;

            // Send Status to Dashboard View
            ViewBag.PHQCompleted = phqCompleted;
            ViewBag.CSSRSCompleted = cssrsCompleted;
            ViewBag.FeelingsCompleted = feelingsCompleted;
            ViewBag.AvailableTimeCompleted =
                availableTimeCompleted;

            // সব section complete হলে Screening Completed
            ViewBag.ScreeningCompleted =
                phqCompleted &&
                cssrsCompleted &&
                feelingsCompleted &&
                availableTimeCompleted;

            // C-SSRS Risk Level
            ViewBag.RiskLevel =
                cssrsAssessment?.RiskLevel
                ?? "Not Assessed";

            return View(student);
        }

        // ================= Student Profile =================

        public IActionResult Profile()
        {
            // Check Student Session
            var studentId = HttpContext.Session.GetInt32("StudentId");

            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            // Get Logged-in Student Information
            var student = _context.Students
                .FirstOrDefault(s => s.StudentId == studentId.Value);

            if (student == null)
            {
                return RedirectToAction("Login");
            }

            return View(student);
        }

        // ================= Semester Screening =================

        // ================= Semester Screening =================

        [HttpGet]
        public async Task<IActionResult> SemesterScreening()
        {
            // Check Student Session
            var studentId =
                HttpContext.Session.GetInt32("StudentId");

            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            // Get Logged-in Student
            var student = await _context.Students
                .FirstOrDefaultAsync(
                    s => s.StudentId == studentId.Value
                );

            if (student == null)
            {
                return RedirectToAction("Login");
            }

            // Check Current Semester
            if (string.IsNullOrWhiteSpace(student.Semester))
            {
                TempData["Error"] =
                    "Current semester information was not found.";

                return RedirectToAction("Dashboard");
            }

            // PHQ-9 Status
            ViewBag.PHQCompleted =
                await _context.PHQAssessments.AnyAsync(p =>
                    p.StudentId == student.StudentId &&
                    p.Semester == student.Semester
                );

            // C-SSRS Status
            ViewBag.CSSRSCompleted =
                await _context.CSSRSAssessments.AnyAsync(c =>
                    c.StudentId == student.StudentId &&
                    c.Semester == student.Semester
                );

            // Get Semester Record
            var semesterRecord =
                await _context.StudentSemesterRecords
                    .FirstOrDefaultAsync(r =>
                        r.StudentId == student.StudentId &&
                        r.Semester == student.Semester
                    );

            // Feelings Status
            ViewBag.FeelingsCompleted =
                semesterRecord != null &&
                !string.IsNullOrWhiteSpace(
                    semesterRecord.FeelingText
                );

            // Available Time Status
            ViewBag.AvailableTimeCompleted =
                semesterRecord != null &&
                !string.IsNullOrWhiteSpace(
                    semesterRecord.AvailableDay
                ) &&
                semesterRecord.StartTime.HasValue &&
                semesterRecord.EndTime.HasValue;

            ViewBag.CurrentSemester = student.Semester;

            return View();
        }

        // ================= PHQ-9 Assessment GET =================

        [HttpGet]
        public async Task<IActionResult> PHQ()
        {
            // Check Student Session
            var studentId =
                HttpContext.Session.GetInt32("StudentId");

            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            // Get Logged-in Student
            var student = await _context.Students
                .FirstOrDefaultAsync(
                    s => s.StudentId == studentId.Value
                );

            if (student == null)
            {
                return RedirectToAction("Login");
            }

            // Check Current Semester
            if (string.IsNullOrWhiteSpace(student.Semester))
            {
                TempData["Error"] =
                    "Current semester information was not found.";

                return RedirectToAction("Dashboard");
            }

            // Check Previous PHQ Submission
            var previousAssessment =
                await _context.PHQAssessments
                    .FirstOrDefaultAsync(p =>
                        p.StudentId == studentId.Value &&
                        p.Semester == student.Semester
                    );

            // Already submitted হলে previous result দেখাবে
            if (previousAssessment != null)
            {
                return RedirectToAction(
                    "PHQResult",
                    new
                    {
                        id = previousAssessment.AssessmentId
                    }
                );
            }

            // Empty PHQ Form Model
            var model = new PHQAssessment
            {
                Semester = student.Semester
            };

            return View(model);
        }

        // ================= PHQ-9 Assessment POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PHQ(
            PHQAssessment model)
        {
            // Check Student Session
            var studentId =
                HttpContext.Session.GetInt32("StudentId");

            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            // Get Logged-in Student
            var student = await _context.Students
                .FirstOrDefaultAsync(
                    s => s.StudentId == studentId.Value
                );

            if (student == null)
            {
                return RedirectToAction("Login");
            }

            // Check Current Semester
            if (string.IsNullOrWhiteSpace(student.Semester))
            {
                TempData["Error"] =
                    "Current semester information was not found.";

                return RedirectToAction("Dashboard");
            }

            // StudentId এবং Semester form থেকে নেওয়া হবে না
            // Logged-in Student থেকে নেওয়া হবে
            model.StudentId = student.StudentId;
            model.Semester = student.Semester;

            // Server থেকে set করা propertyগুলোর পুরোনো
            // validation state remove করা হচ্ছে
            ModelState.Remove(nameof(PHQAssessment.StudentId));
            ModelState.Remove(nameof(PHQAssessment.Student));
            ModelState.Remove(nameof(PHQAssessment.Semester));
            ModelState.Remove(nameof(PHQAssessment.TotalScore));
            ModelState.Remove(nameof(PHQAssessment.SeverityLevel));
            ModelState.Remove(nameof(PHQAssessment.RequiresImmediateReview));
            ModelState.Remove(nameof(PHQAssessment.AssessmentDate));

            // Check Every Question
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

            // Check Model Validation
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check Duplicate Submission
            var previousAssessment =
                await _context.PHQAssessments
                    .FirstOrDefaultAsync(p =>
                        p.StudentId == student.StudentId &&
                        p.Semester == student.Semester
                    );

            // আগে submit করা থাকলে previous result দেখাবে
            if (previousAssessment != null)
            {
                return RedirectToAction(
                    "PHQResult",
                    new
                    {
                        id = previousAssessment.AssessmentId
                    }
                );
            }

            // ================= Calculate Total Score =================

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

            // Calculate Severity
            model.SeverityLevel =
                GetPHQSeverity(model.TotalScore);

            // Question 9 score 0-এর বেশি হলে review required
            model.RequiresImmediateReview =
                model.Question9Score.Value > 0;

            model.AssessmentDate = DateTime.Now;

            try
            {
                // Save PHQ Assessment
                _context.PHQAssessments.Add(model);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "PHQ-9 assessment submitted successfully.";

                return RedirectToAction(
                    "PHQResult",
                    new
                    {
                        id = model.AssessmentId
                    }
                );
            }
            catch (DbUpdateException ex)
            {
                // Database error দেখা যাবে
                string errorMessage =
                    ex.InnerException?.Message ?? ex.Message;

                return Content(errorMessage);
            }
            catch (Exception ex)
            {
                // অন্য error দেখা যাবে
                string errorMessage =
                    ex.InnerException?.Message ?? ex.Message;

                return Content(errorMessage);
            }
        }

        // ================= PHQ-9 Result =================

        [HttpGet]
        public async Task<IActionResult> PHQResult(int id)
        {
            // Check Student Session
            var studentId =
                HttpContext.Session.GetInt32("StudentId");

            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            // Student নিজের result শুধু দেখতে পারবে
            var assessment =
                await _context.PHQAssessments
                    .FirstOrDefaultAsync(p =>
                        p.AssessmentId == id &&
                        p.StudentId == studentId.Value
                    );

            if (assessment == null)
            {
                return NotFound();
            }

            return View(assessment);
        }

        // ================= Calculate PHQ Severity =================

        private string GetPHQSeverity(int totalScore)
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

        // ================= C-SSRS Assessment GET =================

        [HttpGet]
        public async Task<IActionResult> CSRRS()
        {
            // Check Student Session
            var studentId =
                HttpContext.Session.GetInt32("StudentId");

            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            // Get Logged-in Student
            var student = await _context.Students
                .FirstOrDefaultAsync(
                    s => s.StudentId == studentId.Value
                );

            if (student == null)
            {
                return RedirectToAction("Login");
            }

            // Check Current Semester
            if (string.IsNullOrWhiteSpace(student.Semester))
            {
                TempData["Error"] =
                    "Current semester information was not found.";

                return RedirectToAction("Dashboard");
            }

            // Check Previous C-SSRS Submission
            var previousAssessment =
                await _context.CSSRSAssessments
                    .FirstOrDefaultAsync(c =>
                        c.StudentId == student.StudentId &&
                        c.Semester == student.Semester
                    );

            // আগে submit করা থাকলে আগের result দেখাবে
            if (previousAssessment != null)
            {
                return RedirectToAction(
                    "CSRRSResult",
                    new
                    {
                        id = previousAssessment.AssessmentId
                    }
                );
            }

            // Empty C-SSRS Form Model
            var model = new CSSRSAssessment
            {
                Semester = student.Semester
            };

            return View(model);
        }

        // ================= C-SSRS Assessment POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CSRRS(
            CSSRSAssessment model)
        {
            // Check Student Session
            var studentId =
                HttpContext.Session.GetInt32("StudentId");

            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            // Get Logged-in Student
            var student = await _context.Students
                .FirstOrDefaultAsync(
                    s => s.StudentId == studentId.Value
                );

            if (student == null)
            {
                return RedirectToAction("Login");
            }

            // Check Current Semester
            if (string.IsNullOrWhiteSpace(student.Semester))
            {
                TempData["Error"] =
                    "Current semester information was not found.";

                return RedirectToAction("Dashboard");
            }

            // StudentId এবং Semester logged-in student থেকে
            model.StudentId = student.StudentId;
            model.Semester = student.Semester;

            // Server থেকে set করা propertyগুলোর
            // validation state remove করা হচ্ছে
            ModelState.Remove(nameof(CSSRSAssessment.StudentId));
            ModelState.Remove(nameof(CSSRSAssessment.Student));
            ModelState.Remove(nameof(CSSRSAssessment.Semester));
            ModelState.Remove(nameof(CSSRSAssessment.RiskLevel));
            ModelState.Remove(nameof(CSSRSAssessment.RequiresImmediateAction));
            ModelState.Remove(nameof(CSSRSAssessment.AssessmentDate));

            // Question 1, 2 এবং 6 সবসময় answer করতে হবে
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

            // Question 2 Yes হলে Question 3, 4 এবং 5 answer করতে হবে
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

            // Question 2 No হলে Question 3, 4 এবং 5 skip হবে
            if (model.Question2Answer == false)
            {
                model.Question3Answer = false;
                model.Question4Answer = false;
                model.Question5Answer = false;

                ModelState.Remove(
                    nameof(CSSRSAssessment.Question3Answer)
                );

                ModelState.Remove(
                    nameof(CSSRSAssessment.Question4Answer)
                );

                ModelState.Remove(
                    nameof(CSSRSAssessment.Question5Answer)
                );
            }

            // Question 6 Yes হলে recent behaviour answer করতে হবে
            if (model.Question6Answer == true &&
                model.RecentBehavior == null)
            {
                ModelState.AddModelError(
                    "",
                    "Please specify whether the behaviour occurred within the past three months."
                );

                return View(model);
            }

            // Question 6 No হলে RecentBehavior false হবে
            if (model.Question6Answer == false)
            {
                model.RecentBehavior = false;

                ModelState.Remove(
                    nameof(CSSRSAssessment.RecentBehavior)
                );
            }

            // Check Model Validation
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check Duplicate Submission
            var previousAssessment =
                await _context.CSSRSAssessments
                    .FirstOrDefaultAsync(c =>
                        c.StudentId == student.StudentId &&
                        c.Semester == student.Semester
                    );

            // আগে submit করা থাকলে previous result দেখাবে
            if (previousAssessment != null)
            {
                return RedirectToAction(
                    "CSRRSResult",
                    new
                    {
                        id = previousAssessment.AssessmentId
                    }
                );
            }

            // Calculate Risk Level
            model.RiskLevel =
                GetCSSRSRiskLevel(model);

            // High risk হলে immediate action প্রয়োজন
            model.RequiresImmediateAction =
                model.RiskLevel == "High";

            model.AssessmentDate = DateTime.Now;

            try
            {
                // Save C-SSRS Assessment
                _context.CSSRSAssessments.Add(model);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "C-SSRS assessment submitted successfully.";

                return RedirectToAction(
                    "CSRRSResult",
                    new
                    {
                        id = model.AssessmentId
                    }
                );
            }
            catch (DbUpdateException ex)
            {
                string errorMessage =
                    ex.InnerException?.Message ?? ex.Message;

                return Content(errorMessage);
            }
            catch (Exception ex)
            {
                string errorMessage =
                    ex.InnerException?.Message ?? ex.Message;

                return Content(errorMessage);
            }
        }

        // ================= C-SSRS Result =================

        [HttpGet]
        public async Task<IActionResult> CSRRSResult(int id)
        {
            // Check Student Session
            var studentId =
                HttpContext.Session.GetInt32("StudentId");

            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            // Student নিজের assessment শুধু দেখতে পারবে
            var assessment =
                await _context.CSSRSAssessments
                    .FirstOrDefaultAsync(c =>
                        c.AssessmentId == id &&
                        c.StudentId == studentId.Value
                    );

            if (assessment == null)
            {
                return NotFound();
            }

            return View(assessment);
        }

        // ================= Calculate C-SSRS Risk =================

        private string GetCSSRSRiskLevel(
            CSSRSAssessment model)
        {
            // High Risk
            if (model.Question4Answer == true ||
                model.Question5Answer == true ||
                (model.Question6Answer == true &&
                 model.RecentBehavior == true))
            {
                return "High";
            }

            // Moderate Risk
            if (model.Question3Answer == true ||
                model.Question6Answer == true)
            {
                return "Moderate";
            }

            // Low Risk
            if (model.Question1Answer == true ||
                model.Question2Answer == true)
            {
                return "Low";
            }

            return "No Risk Identified";
        }

        // ================= Feelings =================

        // ================= Feelings GET =================

        [HttpGet]
        public async Task<IActionResult> Feelings()
        {
            // Check Student Session
            var studentId =
                HttpContext.Session.GetInt32("StudentId");

            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            // Get Logged-in Student
            var student = await _context.Students
                .FirstOrDefaultAsync(
                    s => s.StudentId == studentId.Value
                );

            if (student == null)
            {
                return RedirectToAction("Login");
            }

            // Check Current Semester
            if (string.IsNullOrWhiteSpace(student.Semester))
            {
                TempData["Error"] =
                    "Current semester information was not found.";

                return RedirectToAction("Dashboard");
            }

            // Check Previous Semester Record
            var semesterRecord =
                await _context.StudentSemesterRecords
                    .FirstOrDefaultAsync(r =>
                        r.StudentId == student.StudentId &&
                        r.Semester == student.Semester
                    );

            // Previous record না থাকলে empty model
            if (semesterRecord == null)
            {
                semesterRecord = new StudentSemesterRecord
                {
                    Semester = student.Semester
                };
            }

            return View(semesterRecord);
        }


        // ================= Feelings POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Feelings(
            StudentSemesterRecord model)
        {
            // Check Student Session
            var studentId =
                HttpContext.Session.GetInt32("StudentId");

            if (studentId == null)
            {
                return RedirectToAction("Login");
            }

            // Get Logged-in Student
            var student = await _context.Students
                .FirstOrDefaultAsync(
                    s => s.StudentId == studentId.Value
                );

            if (student == null)
            {
                return RedirectToAction("Login");
            }

            // Check Current Semester
            if (string.IsNullOrWhiteSpace(student.Semester))
            {
                TempData["Error"] =
                    "Current semester information was not found.";

                return RedirectToAction("Dashboard");
            }

            // Student এবং Semester server থেকে set হবে
            model.StudentId = student.StudentId;
            model.Semester = student.Semester;

            // Server-side property validation remove
            ModelState.Remove(
                nameof(StudentSemesterRecord.StudentId)
            );

            ModelState.Remove(
                nameof(StudentSemesterRecord.Student)
            );

            ModelState.Remove(
                nameof(StudentSemesterRecord.Semester)
            );

            ModelState.Remove(
                nameof(StudentSemesterRecord.SubmittedAt)
            );

            ModelState.Remove(
                nameof(StudentSemesterRecord.UpdatedAt)
            );

            bool hasFeeling =
                !string.IsNullOrWhiteSpace(model.FeelingText);

            bool hasAnyAvailableTime =
                !string.IsNullOrWhiteSpace(model.AvailableDay) ||
                model.StartTime.HasValue ||
                model.EndTime.HasValue;

            // কিছুই না দিলে submit হবে না
            if (!hasFeeling && !hasAnyAvailableTime)
            {
                ModelState.AddModelError(
                    "",
                    "Please write your feelings or provide your available time."
                );
            }

            // Available Time দিলে সব field দিতে হবে
            if (hasAnyAvailableTime &&
                (string.IsNullOrWhiteSpace(model.AvailableDay) ||
                 !model.StartTime.HasValue ||
                 !model.EndTime.HasValue))
            {
                ModelState.AddModelError(
                    "",
                    "Please select day, start time and end time."
                );
            }

            // End Time অবশ্যই Start Time-এর পরে হতে হবে
            if (model.StartTime.HasValue &&
                model.EndTime.HasValue &&
                model.EndTime.Value <= model.StartTime.Value)
            {
                ModelState.AddModelError(
                    nameof(StudentSemesterRecord.EndTime),
                    "End time must be later than start time."
                );
            }

            // Allowed Counseling Days
            var allowedDays = new[]
            {
        "Sunday",
        "Monday",
        "Tuesday",
        "Wednesday",
        "Thursday"
    };

            if (!string.IsNullOrWhiteSpace(model.AvailableDay) &&
                !allowedDays.Contains(model.AvailableDay))
            {
                ModelState.AddModelError(
                    nameof(StudentSemesterRecord.AvailableDay),
                    "Please select a valid day."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check Previous Semester Record
            var existingRecord =
                await _context.StudentSemesterRecords
                    .FirstOrDefaultAsync(r =>
                        r.StudentId == student.StudentId &&
                        r.Semester == student.Semester
                    );

            try
            {
                if (existingRecord == null)
                {
                    // Create New Record
                    model.FeelingText =
                        model.FeelingText?.Trim();

                    model.SubmittedAt = DateTime.Now;

                    _context.StudentSemesterRecords.Add(model);
                }
                else
                {
                    // Update Existing Record
                    existingRecord.FeelingText =
                        model.FeelingText?.Trim();

                    existingRecord.AvailableDay =
                        model.AvailableDay;

                    existingRecord.StartTime =
                        model.StartTime;

                    existingRecord.EndTime =
                        model.EndTime;

                    existingRecord.UpdatedAt =
                        DateTime.Now;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Your feelings and available time have been saved successfully.";

                return RedirectToAction(
                    "SemesterScreening"
                );
            }
            catch (DbUpdateException ex)
            {
                string errorMessage =
                    ex.InnerException?.Message ?? ex.Message;

                return Content(errorMessage);
            }
            catch (Exception ex)
            {
                string errorMessage =
                    ex.InnerException?.Message ?? ex.Message;

                return Content(errorMessage);
            }
        }

        // ================= AI Chat =================

        public IActionResult AIChat()
        {
            return View();
        }

        // ================= Appointment =================

        public IActionResult Appointment()
        {
            return View();
        }

        // ================= Progress =================

        public IActionResult Progress()
        {
            return View();
        }

        // ================= History =================

        public IActionResult History()
        {
            return View();
        }

        // ================= Reports =================

        public IActionResult Reports()
        {
            return View();
        }
    }
}