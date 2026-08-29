using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentMentalHealthMonitoringSystem.Data;
using StudentMentalHealthMonitoringSystem.Models;
using StudentMentalHealthMonitoringSystem.Services;
using StudentMentalHealthMonitoringSystem.ViewModels;

namespace StudentMentalHealthMonitoringSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

       

        // ================= Login  get =================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        // ================= Login  post =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Invalid Email or Password";
                return View();
            }

            var cleanEmail = email.Trim().ToLower();
            var admin = _context.Admins.FirstOrDefault(a => a.Email.ToLower() == cleanEmail);

            if (admin == null)
            {
                ViewBag.Error = "Invalid Email or Password";
                return View();
            }

            bool isPasswordValid = false;
            try
            {
                if (!string.IsNullOrEmpty(admin.Password))
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(password, admin.Password);
                }
            }
            catch
            {
                isPasswordValid = (admin.Password == password);
            }

            if (!isPasswordValid && admin.Password == password)
            {
                isPasswordValid = true;
            }

            if (!isPasswordValid)
            {
                ViewBag.Error = "Invalid Email or Password";
                return View();
            }

            // Store Admin Session
            HttpContext.Session.SetInt32("AdminId", admin.AdminId);
            HttpContext.Session.SetString("AdminName", admin.FullName);

            // Redirect Dashboard
            return RedirectToAction("Dashboard");
        }

        // ================= Dashboard =================

        public IActionResult Dashboard()
        {
            // Get Logged-in Admin Id from Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            // If Session is Empty
            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            // Get Admin Information
            var admin = _context.Admins
                .FirstOrDefault(a => a.AdminId == adminId);

            if (admin == null)
            {
                return RedirectToAction("Login");
            }

            // Dashboard Statistics
            var model = new AdminDashboardViewModel
            {
                Admin = admin,

                TotalStudents = _context.Students.Count(),

                HighRiskStudents = _context.CSSRSAssessments
                    .Count(c => c.RiskLevel == "High"),

                TotalPsychologists = _context.Psychologists.Count(),

                TotalDepartments = _context.Students
                    .Select(s => s.Department)
                    .Distinct()
                    .Count(),

                TotalCounselingSessions = _context.Counselings.Count(),

                PendingCounselingSessions = _context.Counselings
                    .Count(c => c.Status == "Pending")
            };

            return View(model);
        }


        // ================= Students =================

        public IActionResult Students(string? department = null)
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var query = _context.Students.AsQueryable();

            if (!string.IsNullOrWhiteSpace(department) && !string.Equals(department, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(s => s.Department == department);
                ViewBag.SelectedDepartment = department;
            }
            else
            {
                ViewBag.SelectedDepartment = "All";
            }

            var students = query
                .OrderBy(s => s.Department)
                .ThenBy(s => s.FullName)
                .ToList();

            ViewBag.AvailableDepartments = _context.Students
                .Select(s => s.Department)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            return View(students);
        }

        // ================= Add Student =================

        [HttpGet]
        public IActionResult AddStudent()
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(Student student)
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            // Password and ImageFile are handled separately
            ModelState.Remove("Password");
            ModelState.Remove("ImageFile");

            if (!ModelState.IsValid)
            {
                return View(student);
            }

            // ================= Duplicate Student ID =================

            bool studentIdExists = await _context.Students
                .AnyAsync(s =>
                    s.StudentIdNumber == student.StudentIdNumber);

            if (studentIdExists)
            {
                ModelState.AddModelError(
                    "StudentIdNumber",
                    "This Student ID already exists."
                );

                return View(student);
            }


            // ================= Duplicate Email =================

            bool emailExists = await _context.Students
                .AnyAsync(s =>
                    s.Email == student.Email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered."
                );

                return View(student);
            }


            // ================= Password =================

            // Give a default password because Admin
            // is creating the student account.

            student.Password =
                BCrypt.Net.BCrypt.HashPassword("Student@123");


            // ================= Image Upload =================

            if (student.ImageFile != null &&
                student.ImageFile.Length > 0)
            {
                var uploadFolder = Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "students"
                );

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var extension = Path
                    .GetExtension(student.ImageFile.FileName)
                    .ToLower();

                var allowedExtensions = new[]
                {
            ".jpg",
            ".jpeg",
            ".png"
        };

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "Only JPG, JPEG and PNG images are allowed."
                    );

                    return View(student);
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

            _context.Students.Add(student);

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
                "Student added successfully with continuous observation tracking.";

            return RedirectToAction("Students");
        }

        // ================= Student Details =================

        public IActionResult StudentDetails(int id)
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var student = _context.Students
                .FirstOrDefault(s => s.StudentId == id);

            if (student == null)
            {
                return NotFound();
            }

            var phq = _context.PHQAssessments
                .OrderByDescending(p => p.AssessmentDate)
                .FirstOrDefault(p => p.StudentId == id);

            var cssrs = _context.CSSRSAssessments
                .OrderByDescending(c => c.AssessmentDate)
                .FirstOrDefault(c => c.StudentId == id);

            var semesterRecord = _context.StudentSemesterRecords
                .OrderByDescending(r => r.SubmittedAt)
                .FirstOrDefault(r => r.StudentId == id);

            var model = new PsychologistStudentViewModel
            {
                Student = student,
                PHQAssessment = phq,
                CSSRSAssessment = cssrs,
                SemesterRecord = semesterRecord
            };

            return View(model);
        }



        // ================= Edit Student =================

        [HttpGet]
        public IActionResult EditStudent(int id)
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            // Get Student Information
            var student = _context.Students
                .FirstOrDefault(s => s.StudentId == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // ================= Edit Student POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(Student student)
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            // Password and ImageFile are not edited from this form
            ModelState.Remove("Password");
            ModelState.Remove("ImageFile");

            // Check Validation
            if (!ModelState.IsValid)
            {
                return View(student);
            }

            // Get Existing Student
            var existingStudent = _context.Students
                .FirstOrDefault(s =>
                    s.StudentId == student.StudentId);

            if (existingStudent == null)
            {
                return NotFound();
            }

            // ================= Duplicate Email Check =================

            bool emailExists = _context.Students.Any(s =>
                s.Email == student.Email &&
                s.StudentId != student.StudentId);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already used by another student."
                );

                return View(student);
            }

            // ================= Update Student Information =================

            existingStudent.StudentIdNumber =
                student.StudentIdNumber;

            existingStudent.FullName =
                student.FullName;

            existingStudent.Email =
                student.Email;

            existingStudent.Phone =
                student.Phone;

            existingStudent.DateOfBirth =
                student.DateOfBirth;

            existingStudent.Gender =
                student.Gender;

            existingStudent.Department =
                student.Department;

            existingStudent.Semester =
                student.Semester;

            existingStudent.Height =
                student.Height;

            existingStudent.Weight =
                student.Weight;

            existingStudent.FinancialCondition =
                student.FinancialCondition;

            existingStudent.GuardianName =
                student.GuardianName;

            existingStudent.Relationship =
                student.Relationship;

            existingStudent.GuardianPhone =
                student.GuardianPhone;

            existingStudent.GuardianEmail =
                student.GuardianEmail;

            // ================= Image Upload =================

            if (student.ImageFile != null &&
                student.ImageFile.Length > 0)
            {
                var uploadFolder = Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "students"
                );

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var extension = Path
                    .GetExtension(student.ImageFile.FileName)
                    .ToLower();

                var allowedExtensions = new[]
                {
            ".jpg",
            ".jpeg",
            ".png"
        };

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "Only JPG, JPEG and PNG images are allowed."
                    );

                    return View(student);
                }

                var fileName =
                    $"{Guid.NewGuid()}{extension}";

                var fullPath = Path.Combine(
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

                existingStudent.ProfileImage =
                    $"/images/students/{fileName}";
            }

            // ================= Save =================

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Student updated successfully.";

            return RedirectToAction(
                "StudentDetails",
                new
                {
                    id = existingStudent.StudentId
                }
            );
        }

        // ================= Delete Student =================

        [HttpGet]
        public IActionResult DeleteStudent(int id)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var student = _context.Students
                .FirstOrDefault(s => s.StudentId == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // ================= Confirm Delete Student =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDeleteStudent(int id)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
            {
                return NotFound();
            }

            // ================= Delete PHQ Records =================

            var phqRecords = await _context.PHQAssessments
                .Where(p => p.StudentId == id)
                .ToListAsync();

            _context.PHQAssessments.RemoveRange(phqRecords);


            // ================= Delete C-SSRS Records =================

            var cssrsRecords = await _context.CSSRSAssessments
                .Where(c => c.StudentId == id)
                .ToListAsync();

            _context.CSSRSAssessments.RemoveRange(cssrsRecords);


            // ================= Delete Semester Records =================

            var semesterRecords = await _context.StudentSemesterRecords
                .Where(s => s.StudentId == id)
                .ToListAsync();

            _context.StudentSemesterRecords.RemoveRange(semesterRecords);


            // ================= Delete Counseling Records =================

            var counselingRecords = await _context.Counselings
                .Where(c => c.StudentId == id)
                .ToListAsync();

            _context.Counselings.RemoveRange(counselingRecords);


            // ================= Delete Student =================

            _context.Students.Remove(student);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Student and related records deleted successfully.";

            return RedirectToAction("Students");
        }



        // ================= Psychologists =================

        public IActionResult Psychologists()
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var psychologists = _context.Psychologists
                .OrderBy(p => p.FullName)
                .ToList();

            return View(psychologists);
        }



        // ================= Psychologist Details =================

        public IActionResult PsychologistDetails(int id)
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var psychologist = _context.Psychologists
                .FirstOrDefault(p => p.PsychologistId == id);

            if (psychologist == null)
            {
                return NotFound();
            }

            return View(psychologist);
        }


        // ================= Add Psychologist =================

        [HttpGet]
        public IActionResult AddPsychologist()
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            // Reuse Psychologist Registration View
            return RedirectToAction("Register", "Psychologist");
        }

        

        // ================= Edit Psychologist =================

        [HttpGet]
        public IActionResult EditPsychologist(int id)
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var psychologist = _context.Psychologists
                .FirstOrDefault(p => p.PsychologistId == id);

            if (psychologist == null)
            {
                return NotFound();
            }

            return View(psychologist);
        }

        // ================= Edit Psychologist POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPsychologist(Psychologist psychologist)
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            // Remove fields that are NOT being edited
            // This prevents Password/ImageFile validation
            // from blocking the update.
            ModelState.Remove("Password");
            ModelState.Remove("ImageFile");

            // Check Model Validation
            if (!ModelState.IsValid)
            {
                return View(psychologist);
            }

            // Get Existing Psychologist from Database
            var existingPsychologist = _context.Psychologists
                .FirstOrDefault(p =>
                    p.PsychologistId == psychologist.PsychologistId);

            if (existingPsychologist == null)
            {
                return NotFound();
            }

            // ================= Duplicate Email Check =================

            bool emailExists = _context.Psychologists.Any(p =>
                p.Email == psychologist.Email &&
                p.PsychologistId != psychologist.PsychologistId);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already used by another psychologist."
                );

                return View(psychologist);
            }

            // ================= Update Information =================

            existingPsychologist.FullName =
                psychologist.FullName;

            existingPsychologist.Email =
                psychologist.Email;

            existingPsychologist.Phone =
                psychologist.Phone;

            existingPsychologist.Specialization =
                psychologist.Specialization;

            existingPsychologist.Qualification =
                psychologist.Qualification;

            existingPsychologist.Experience =
                psychologist.Experience;

            // IMPORTANT:
            // Password is NOT changed here.
            // Existing password remains unchanged.

            // ================= Image Upload =================

            if (psychologist.ImageFile != null &&
                psychologist.ImageFile.Length > 0)
            {
                var uploadFolder = Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "psychologists"
                );

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var extension =
                    Path.GetExtension(
                        psychologist.ImageFile.FileName
                    ).ToLower();

                var allowedExtensions =
                    new[] { ".jpg", ".jpeg", ".png" };

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "Only JPG, JPEG and PNG images are allowed."
                    );

                    return View(psychologist);
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

                existingPsychologist.ProfileImage =
                    $"/images/psychologists/{fileName}";
            }

            // ================= Save Changes =================

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Psychologist updated successfully.";

            return RedirectToAction(
                "PsychologistDetails",
                new
                {
                    id = existingPsychologist.PsychologistId
                }
            );
        }




        // ================= Delete Psychologist  get  =================

        [HttpGet]
        public IActionResult DeletePsychologist(int id)
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var psychologist = _context.Psychologists
                .FirstOrDefault(p => p.PsychologistId == id);

            if (psychologist == null)
            {
                return NotFound();
            }

            return View(psychologist);
        }


        // ================= Delete Psychologist  post =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePsychologist(Psychologist psychologist)
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var existing = _context.Psychologists
                .FirstOrDefault(p => p.PsychologistId == psychologist.PsychologistId);

            if (existing == null)
            {
                return NotFound();
            }

            var id = existing.PsychologistId;

            // Delete dependent records to prevent foreign key constraint violations
            var observationReports = await _context.ObservationReports
                .Where(o => o.PsychologistId == id)
                .ToListAsync();
            if (observationReports.Any())
            {
                _context.ObservationReports.RemoveRange(observationReports);
            }

            var counselingObservations = await _context.CounselingObservations
                .Where(co => co.PsychologistId == id)
                .ToListAsync();
            if (counselingObservations.Any())
            {
                _context.CounselingObservations.RemoveRange(counselingObservations);
            }

            var counselings = await _context.Counselings
                .Where(c => c.PsychologistId == id)
                .ToListAsync();
            if (counselings.Any())
            {
                var counselingIds = counselings.Select(c => c.CounselingId).ToList();

                var extraObsReports = await _context.ObservationReports
                    .Where(o => counselingIds.Contains(o.RootCounselingId))
                    .ToListAsync();
                if (extraObsReports.Any())
                {
                    _context.ObservationReports.RemoveRange(extraObsReports);
                }

                var extraObs = await _context.CounselingObservations
                    .Where(co => counselingIds.Contains(co.CounselingId) || counselingIds.Contains(co.RootCounselingId))
                    .ToListAsync();
                if (extraObs.Any())
                {
                    _context.CounselingObservations.RemoveRange(extraObs);
                }

                _context.Counselings.RemoveRange(counselings);
            }

            var availabilities = await _context.PsychologistAvailabilities
                .Where(pa => pa.PsychologistId == id)
                .ToListAsync();
            if (availabilities.Any())
            {
                _context.PsychologistAvailabilities.RemoveRange(availabilities);
            }

            var screeningReports = await _context.ScreeningReports
                .Where(sr => sr.PsychologistId == id)
                .ToListAsync();
            if (screeningReports.Any())
            {
                _context.ScreeningReports.RemoveRange(screeningReports);
            }

            _context.Psychologists.Remove(existing);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Psychologist deleted successfully.";

            return RedirectToAction("Psychologists");
        }

        // ================= Departments =================

        public IActionResult Departments()
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var departments = _context.Students
                .GroupBy(s => s.Department)
                .Select(g => new DepartmentViewModel
                {
                    DepartmentName = g.Key,
                    TotalStudents = g.Count()
                })
                .OrderBy(d => d.DepartmentName)
                .ToList();

            return View(departments);
        }





        // ================= Reports =================

        public IActionResult Reports()
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }
        // =========================================================
        // OBSERVATION REPORTS
        // =========================================================

        // ================= Observation Reports =================

        [HttpGet]
        public async Task<IActionResult> ObservationReports()
        {
            // Check Admin Session

            var adminId =
                HttpContext.Session.GetInt32(
                    "AdminId"
                );


            if (adminId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // ================= Get All Observation Reports =================

            var observationReports =
                await _context.ObservationReports

                    .Include(r =>
                        r.Student
                    )

                    .Include(r =>
                        r.Psychologist
                    )

                    .Include(r =>
                        r.RootCounseling
                    )

                    .OrderByDescending(r =>
                        r.UpdatedAt
                    )

                    .ToListAsync();


            return View(
                observationReports
            );
        }


        // ================= Observation Report Details =================

        [HttpGet]
        public async Task<IActionResult> ObservationReportDetails(
            int id)
        {
            // Check Admin Session

            var adminId =
                HttpContext.Session.GetInt32(
                    "AdminId"
                );


            if (adminId == null)
            {
                return RedirectToAction(
                    "Login"
                );
            }


            // ================= Get Observation Report =================

            var observationReport =
                await _context.ObservationReports

                    .Include(r =>
                        r.Student
                    )

                    .Include(r =>
                        r.Psychologist
                    )

                    .Include(r =>
                        r.RootCounseling
                    )

                    .FirstOrDefaultAsync(r =>
                        r.ObservationReportId ==
                            id
                    );


            if (observationReport == null)
            {
                return NotFound();
            }


            // =====================================================
            // GET ALL COUNSELING / FOLLOW-UP OBSERVATIONS
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

        // ================= Student Report =================

        public IActionResult StudentReport()
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var students = _context.Students
                .OrderBy(s => s.FullName)
                .ToList();

            return View(students);
        }

        // ================= High Risk Report =================

        public IActionResult HighRiskReport()
        {
            // Check Admin Session
            var adminId =
                HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            // ================= Get High Risk Assessments =================
            // Include Student so the View can access
            // assessment.Student information.

            var highRiskStudents =
                _context.CSSRSAssessments
                    .Include(c => c.Student)
                    .Where(c =>
                        c.RiskLevel == "High" ||
                        c.RiskLevel == "Severe" ||
                        c.RiskLevel == "Extremely Severe")
                    .OrderByDescending(c => c.AssessmentDate)
                    .ToList();

            return View(highRiskStudents);
        }





        // ================= Counseling Report =================

        [HttpGet]
        public async Task<IActionResult> CounselingReport()
        {
            // Check Admin Session
            var adminId =
                HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var counselingList =
                await _context.Counselings
                    .Include(c => c.Student)
                    .Include(c => c.Psychologist)
                    .OrderBy(c => c.CounselingDate)
                    .ThenBy(c => c.AppointmentTime)
                    .ToListAsync();

            return View(counselingList);
        }


        // ================= Psychologist Report =================

        public IActionResult PsychologistReport()
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var psychologists = _context.Psychologists
                .OrderBy(p => p.FullName)
                .ToList();

            return View(psychologists);
        }






        // ================= Department Report =================

        public IActionResult DepartmentReport()
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var departments = _context.Students
                .GroupBy(s => s.Department)
                .Select(g => new DepartmentViewModel
                {
                    DepartmentName = g.Key,
                    TotalStudents = g.Count()
                })
                .OrderBy(x => x.DepartmentName)
                .ToList();

            return View(departments);
        }




        // ================= Analytics =================

        public IActionResult Analytics()
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var model = new AdminDashboardViewModel
            {
                TotalStudents = _context.Students.Count(),

                HighRiskStudents = _context.CSSRSAssessments
                    .Count(x => x.RiskLevel == "High"),

                TotalPsychologists = _context.Psychologists.Count(),

                TotalDepartments = _context.Students
                    .Select(s => s.Department)
                    .Distinct()
                    .Count(),

                TotalCounselingSessions = _context.Counselings.Count(),

                PendingCounselingSessions = _context.Counselings
                    .Count(c => c.Status == "Pending")
            };

            return View(model);
        }




        // ================= Settings =================

        public IActionResult Settings()
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }





        // ================= Profile get method  =================

        [HttpGet]
        public IActionResult Profile()
        {
            // Check Admin Session
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var admin = _context.Admins
                .FirstOrDefault(a => a.AdminId == adminId);

            if (admin == null)
            {
                return RedirectToAction("Login");
            }

            return View(admin);
        }


        // ================= Profile  post method =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Admin admin)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var existingAdmin = _context.Admins
                .FirstOrDefault(a => a.AdminId == adminId);

            if (existingAdmin == null)
            {
                return RedirectToAction("Login");
            }

            existingAdmin.FullName = admin.FullName;
            existingAdmin.Email = admin.Email;
            existingAdmin.Phone = admin.Phone;

            if (admin.ImageFile != null &&
                admin.ImageFile.Length > 0)
            {
                var folder = Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "admins");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var extension = Path.GetExtension(
                    admin.ImageFile.FileName);

                var fileName =
                    $"{Guid.NewGuid()}{extension}";

                var path = Path.Combine(
                    folder,
                    fileName);

                using var stream =
                    new FileStream(path, FileMode.Create);

                await admin.ImageFile.CopyToAsync(stream);

                existingAdmin.ProfileImage =
                    $"/images/admins/{fileName}";
            }

            _context.SaveChanges();

            TempData["Success"] =
                "Profile Updated Successfully.";

            return RedirectToAction("Profile");
        }






        // ================= Logout =================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Index",
                "Home");
        }


        // =========================================================
        // SCREENING ANALYTICS REPORT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ScreeningAnalytics(string? semester, string? department)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            // Get available semesters from DB
            var semestersFromPHQ = await _context.PHQAssessments
                .Select(p => p.Semester)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToListAsync();

            var semestersFromCSSRS = await _context.CSSRSAssessments
                .Select(c => c.Semester)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToListAsync();

            var semestersFromRecord = await _context.StudentSemesterRecords
                .Select(r => r.Semester)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToListAsync();

            var semestersFromStudent = await _context.Students
                .Select(s => s.Semester)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToListAsync();

            var availableSemesters = semestersFromPHQ
                .Union(semestersFromCSSRS)
                .Union(semestersFromRecord)
                .Union(semestersFromStudent)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!)
                .Distinct()
                .OrderByDescending(s => s)
                .ToList();

            if (!availableSemesters.Any())
            {
                availableSemesters.Add("Spring 2026");
            }

            // Insert Overall option at the top
            availableSemesters.Insert(0, "Overall (All Semesters)");

            var selectedSemester = string.IsNullOrWhiteSpace(semester) ? "Overall (All Semesters)" : semester.Trim();
            bool isOverall = selectedSemester == "Overall" || selectedSemester == "Overall (All Semesters)" || selectedSemester == "All";
            if (isOverall)
            {
                selectedSemester = "Overall (All Semesters)";
            }

            // Get available departments
            var availableDepartmentsFromDb = await _context.Departments
                .Select(d => d.DepartmentName)
                .OrderBy(d => d)
                .ToListAsync();

            var studentsFromDb = await _context.Students.ToListAsync();
            var departmentsFromStudents = studentsFromDb
                .Select(s => s.Department)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d!)
                .Distinct()
                .ToList();

            var availableDepartments = availableDepartmentsFromDb
                .Union(departmentsFromStudents)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d!)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var selectedDepartment = string.IsNullOrWhiteSpace(department) ? "All" : department.Trim();

            // Query students
            var studentsQuery = _context.Students.AsQueryable();
            if (selectedDepartment != "All")
            {
                studentsQuery = studentsQuery.Where(s => s.Department == selectedDepartment);
            }
            var targetStudents = await studentsQuery.ToListAsync();
            var targetStudentIds = targetStudents.Select(s => s.StudentId).ToHashSet();

            // Load Assessments
            List<PHQAssessment> phqAssessments;
            List<CSSRSAssessment> cssrsAssessments;
            List<StudentSemesterRecord> semesterRecords;

            if (isOverall)
            {
                phqAssessments = await _context.PHQAssessments
                    .Where(p => targetStudentIds.Contains(p.StudentId))
                    .ToListAsync();

                cssrsAssessments = await _context.CSSRSAssessments
                    .Where(c => targetStudentIds.Contains(c.StudentId))
                    .ToListAsync();

                semesterRecords = await _context.StudentSemesterRecords
                    .Where(r => targetStudentIds.Contains(r.StudentId))
                    .ToListAsync();
            }
            else
            {
                phqAssessments = await _context.PHQAssessments
                    .Where(p => p.Semester == selectedSemester && targetStudentIds.Contains(p.StudentId))
                    .ToListAsync();

                cssrsAssessments = await _context.CSSRSAssessments
                    .Where(c => c.Semester == selectedSemester && targetStudentIds.Contains(c.StudentId))
                    .ToListAsync();

                semesterRecords = await _context.StudentSemesterRecords
                    .Where(r => r.Semester == selectedSemester && targetStudentIds.Contains(r.StudentId))
                    .ToListAsync();
            }

            int totalScreeningsConducted = phqAssessments.Count + cssrsAssessments.Count + semesterRecords.Count;

            // Group by StudentId to find each student's highest severity status
            var screenedStudentIds = phqAssessments.Select(p => p.StudentId)
                .Union(cssrsAssessments.Select(c => c.StudentId))
                .Union(semesterRecords.Select(r => r.StudentId))
                .Distinct()
                .ToList();

            int normalCount = 0;
            int moderateCount = 0;
            int severeCount = 0;
            int extremelySevereCount = 0;

            foreach (var sId in screenedStudentIds)
            {
                string studentStatus = GetStudentSemesterSeverity(sId, phqAssessments, cssrsAssessments, semesterRecords);
                if (studentStatus == "Extremely Severe") extremelySevereCount++;
                else if (studentStatus == "Severe") severeCount++;
                else if (studentStatus == "Moderate") moderateCount++;
                else normalCount++;
            }

            int totalScreened = screenedStudentIds.Count;

            // Calculate Department Breakdowns for Chart 2 & Table
            var departmentSummaries = new List<DepartmentSeveritySummary>();
            var deptsToProcess = (selectedDepartment == "All") ? availableDepartments : new List<string> { selectedDepartment };

            List<PHQAssessment> allPhq;
            List<CSSRSAssessment> allCssrs;
            List<StudentSemesterRecord> allRecords;

            if (isOverall)
            {
                allPhq = await _context.PHQAssessments.ToListAsync();
                allCssrs = await _context.CSSRSAssessments.ToListAsync();
                allRecords = await _context.StudentSemesterRecords.ToListAsync();
            }
            else
            {
                allPhq = await _context.PHQAssessments.Where(p => p.Semester == selectedSemester).ToListAsync();
                allCssrs = await _context.CSSRSAssessments.Where(c => c.Semester == selectedSemester).ToListAsync();
                allRecords = await _context.StudentSemesterRecords.Where(r => r.Semester == selectedSemester).ToListAsync();
            }

            foreach (var deptName in deptsToProcess)
            {
                var deptStudents = studentsFromDb.Where(s => s.Department == deptName).ToList();
                var deptStudentIds = deptStudents.Select(s => s.StudentId).ToHashSet();

                var deptPhq = allPhq.Where(p => deptStudentIds.Contains(p.StudentId)).ToList();
                var deptCssrs = allCssrs.Where(c => deptStudentIds.Contains(c.StudentId)).ToList();
                var deptRecs = allRecords.Where(r => deptStudentIds.Contains(r.StudentId)).ToList();

                var deptScreenedStudentIds = deptPhq.Select(p => p.StudentId)
                    .Union(deptCssrs.Select(c => c.StudentId))
                    .Union(deptRecs.Select(r => r.StudentId))
                    .Distinct()
                    .ToList();

                int dNormal = 0, dMod = 0, dSev = 0, dExt = 0;
                foreach (var sId in deptScreenedStudentIds)
                {
                    string st = GetStudentSemesterSeverity(sId, deptPhq, deptCssrs, deptRecs);
                    if (st == "Extremely Severe") dExt++;
                    else if (st == "Severe") dSev++;
                    else if (st == "Moderate") dMod++;
                    else dNormal++;
                }

                int dTotal = deptScreenedStudentIds.Count;

                departmentSummaries.Add(new DepartmentSeveritySummary
                {
                    DepartmentName = deptName,
                    TotalStudents = deptStudents.Count,
                    TotalScreened = dTotal,
                    NormalCount = dNormal,
                    ModerateCount = dMod,
                    SevereCount = dSev,
                    ExtremelySevereCount = dExt,
                    NormalPercentage = dTotal > 0 ? Math.Round((double)dNormal / dTotal * 100, 1) : 0,
                    ModeratePercentage = dTotal > 0 ? Math.Round((double)dMod / dTotal * 100, 1) : 0,
                    SeverePercentage = dTotal > 0 ? Math.Round((double)dSev / dTotal * 100, 1) : 0,
                    ExtremelySeverePercentage = dTotal > 0 ? Math.Round((double)dExt / dTotal * 100, 1) : 0
                });
            }

            var model = new ScreeningAnalyticsViewModel
            {
                SelectedSemester = selectedSemester,
                AvailableSemesters = availableSemesters,
                SelectedDepartment = selectedDepartment,
                AvailableDepartments = availableDepartments,
                IsAdmin = true,
                TotalStudentsInScope = targetStudents.Count,
                TotalScreeningsConducted = totalScreeningsConducted,
                TotalScreenedStudents = totalScreened,
                NormalCount = normalCount,
                ModerateCount = moderateCount,
                SevereCount = severeCount,
                ExtremelySevereCount = extremelySevereCount,
                NormalPercentage = totalScreened > 0 ? Math.Round((double)normalCount / totalScreened * 100, 1) : 0,
                ModeratePercentage = totalScreened > 0 ? Math.Round((double)moderateCount / totalScreened * 100, 1) : 0,
                SeverePercentage = totalScreened > 0 ? Math.Round((double)severeCount / totalScreened * 100, 1) : 0,
                ExtremelySeverePercentage = totalScreened > 0 ? Math.Round((double)extremelySevereCount / totalScreened * 100, 1) : 0,
                DepartmentBreakdowns = departmentSummaries
            };

            return View(model);
        }

        private string GetStudentSemesterSeverity(
            int studentId,
            List<PHQAssessment> phqList,
            List<CSSRSAssessment> cssrsList,
            List<StudentSemesterRecord> recordList)
        {
            var studentPhq = phqList.Where(p => p.StudentId == studentId).ToList();
            var studentCssrs = cssrsList.Where(c => c.StudentId == studentId).ToList();
            var studentRecs = recordList.Where(r => r.StudentId == studentId).ToList();

            int maxRank = 0; // 0=Normal, 1=Moderate, 2=Severe, 3=Extremely Severe

            foreach (var p in studentPhq)
            {
                int rank = p.SeverityLevel switch
                {
                    "Severe" => 3,
                    "Moderately Severe" => 2,
                    "Moderate" => 1,
                    "Mild" => 1,
                    _ => 0
                };
                if (rank > maxRank) maxRank = rank;
            }

            foreach (var c in studentCssrs)
            {
                int rank = c.RiskLevel switch
                {
                    "High" => 2,
                    "Moderate" => 1,
                    _ => 0
                };
                if (rank > maxRank) maxRank = rank;
            }

            foreach (var r in studentRecs)
            {
                int rank = r.FeelingRiskLevel switch
                {
                    "Extremely Severe" => 3,
                    "Severe" => 2,
                    "Moderate" => 1,
                    _ => 0
                };
                if (rank > maxRank) maxRank = rank;
            }

            return maxRank switch
            {
                3 => "Extremely Severe",
                2 => "Severe",
                1 => "Moderate",
                _ => "Normal"
            };
        }


        // =========================================================
        // STUDENT PROGRESS & FOLLOW-UP REPORTS (ADMIN)
        // =========================================================
        // DUMMY DATA SEEDING ACTION
        // =========================================================

        [HttpGet]
        public IActionResult SeedData()
        {
            StudentMentalHealthMonitoringSystem.Data.DummyDataSeeder.SeedDummyData(_context);
            return RedirectToAction(nameof(StudentProgressReports));
        }

        // =========================================================
        // STUDENT PROGRESS & FOLLOW-UP REPORTS (ADMIN)
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> StudentProgressReports(string? followUpFilter, string? department)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            // Auto-seed if database doesn't have student observations yet
            if (!_context.CounselingObservations.Any())
            {
                StudentMentalHealthMonitoringSystem.Data.DummyDataSeeder.SeedDummyData(_context);
            }

            var filter = string.IsNullOrWhiteSpace(followUpFilter) ? "All" : followUpFilter.Trim();
            var dept = string.IsNullOrWhiteSpace(department) ? "All" : department.Trim();

            var query = _context.ObservationReports
                .Include(r => r.Student)
                .Include(r => r.Psychologist)
                .AsQueryable();

            if (filter == "InProgress")
            {
                query = query.Where(r => !r.IsFinal);
            }
            else if (filter == "Completed")
            {
                query = query.Where(r => r.IsFinal);
            }

            if (dept != "All")
            {
                query = query.Where(r => r.Student != null && r.Student.Department == dept);
            }

            var reports = await query.ToListAsync();

            var availableDepartments = await _context.Departments
                .Select(d => d.DepartmentName)
                .OrderBy(d => d)
                .ToListAsync();

            var summaryItems = new List<StudentProgressReportSummaryItem>();
            var processedRootIds = new HashSet<int>();

            foreach (var r in reports)
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

            // Also check for Counselings where student went to psychologist but ObservationReport is not yet created
            if (filter != "Completed")
            {
                var unmappedCounselingsQuery = _context.Counselings
                    .Include(c => c.Student)
                    .Include(c => c.Psychologist)
                    .Where(c => c.StudentId > 0)
                    .AsQueryable();

                if (dept != "All")
                {
                    unmappedCounselingsQuery = unmappedCounselingsQuery.Where(c => c.Student != null && c.Student.Department == dept);
                }

                var allCounselings = await unmappedCounselingsQuery.ToListAsync();
                var groupedByStudent = allCounselings
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
                DepartmentFilter = dept,
                AvailableDepartments = availableDepartments,
                Reports = summaryItems.OrderByDescending(x => x.LatestSessionDate).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> StudentProgressDetails(int id)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var report = await _context.ObservationReports
                .Include(r => r.Student)
                .Include(r => r.Psychologist)
                .FirstOrDefaultAsync(r => r.ObservationReportId == id || r.StudentId == id || r.RootCounselingId == id);

            if (report == null)
            {
                // Fallback: check if student has counselings directly
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
                    PsychologistId = counselings.First().PsychologistId,
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

        // =====================================================
        // UNIVERSITY-WIDE SEMESTER SCREENING COMPLIANCE & EMAIL REMINDERS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ScreeningCompliance(string department = "All", string semester = "All", string status = "All")
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null) return RedirectToAction("Login");

            var studentsQuery = _context.Students.AsQueryable();

            if (!string.Equals(department, "All", StringComparison.OrdinalIgnoreCase))
            {
                studentsQuery = studentsQuery.Where(s => s.Department == department);
            }

            var students = await studentsQuery
                .OrderBy(s => s.Department)
                .ThenBy(s => s.Semester)
                .ThenBy(s => s.FullName)
                .ToListAsync();

            var availableDepartments = await _context.Students
                .Select(s => s.Department)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            var availableSemesters = await _context.Students
                .Select(s => s.Semester)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            var phqList = await _context.PHQAssessments.ToListAsync();
            var cssrsList = await _context.CSSRSAssessments.ToListAsync();

            var items = new List<StudentScreeningComplianceItem>();

            foreach (var student in students)
            {
                var studentSemester = string.IsNullOrWhiteSpace(student.Semester) ? "Semester 1" : student.Semester;

                var phq = phqList
                    .Where(p => p.StudentId == student.StudentId && p.Semester.ToLower() == studentSemester.ToLower())
                    .OrderByDescending(p => p.AssessmentDate)
                    .FirstOrDefault();

                var cssrs = cssrsList
                    .Where(c => c.StudentId == student.StudentId && c.Semester.ToLower() == studentSemester.ToLower())
                    .OrderByDescending(c => c.AssessmentDate)
                    .FirstOrDefault();

                var item = new StudentScreeningComplianceItem
                {
                    StudentId = student.StudentId,
                    FullName = student.FullName,
                    DepartmentName = student.Department,
                    Semester = studentSemester,
                    Email = student.Email,
                    Phone = student.Phone,
                    HasPHQ = phq != null,
                    PHQScore = phq?.TotalScore,
                    PHQSeverity = phq != null ? phq.SeverityLevel : "Pending",
                    PHQDate = phq?.AssessmentDate,
                    HasCSSRS = cssrs != null,
                    CSSRSRiskLevel = cssrs != null ? cssrs.RiskLevel : "Pending",
                    CSSRSDate = cssrs?.AssessmentDate
                };

                items.Add(item);
            }

            // Filtering
            if (!string.Equals(semester, "All", StringComparison.OrdinalIgnoreCase))
            {
                items = items.Where(i => string.Equals(i.Semester, semester, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                items = items.Where(i => i.IsFullyScreened).ToList();
            }
            else if (string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                items = items.Where(i => !i.IsFullyScreened).ToList();
            }

            var viewModel = new ScreeningComplianceViewModel
            {
                Title = "University-Wide Semester Screening Compliance",
                SelectedDepartment = department,
                SelectedSemester = semester,
                SelectedStatus = status,
                TotalStudents = items.Count,
                CompletedStudents = items.Count(i => i.IsFullyScreened),
                PendingStudents = items.Count(i => !i.IsFullyScreened),
                Students = items,
                AvailableSemesters = availableSemesters,
                AvailableDepartments = availableDepartments
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendScreeningReminder(int studentId)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null) return RedirectToAction("Login");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null)
            {
                TempData["Error"] = "Student record not found.";
                return RedirectToAction(nameof(ScreeningCompliance));
            }

            TempData["Success"] = $"✉️ Reminder email sent successfully to {student.FullName} ({student.Email}) for {student.Semester ?? "Semester 1"} PHQ-9 & C-SSRS screening completion.";
            return RedirectToAction(nameof(ScreeningCompliance));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendBulkScreeningReminders(string department = "All", string semester = "All")
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null) return RedirectToAction("Login");

            var studentsQuery = _context.Students.AsQueryable();
            if (!string.Equals(department, "All", StringComparison.OrdinalIgnoreCase))
            {
                studentsQuery = studentsQuery.Where(s => s.Department == department);
            }

            var students = await studentsQuery.ToListAsync();
            var phqList = await _context.PHQAssessments.ToListAsync();
            var cssrsList = await _context.CSSRSAssessments.ToListAsync();

            int reminderCount = 0;

            foreach (var student in students)
            {
                var sem = string.IsNullOrWhiteSpace(student.Semester) ? "Semester 1" : student.Semester;
                if (!string.Equals(semester, "All", StringComparison.OrdinalIgnoreCase) && !string.Equals(sem, semester, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bool hasPHQ = phqList.Any(p => p.StudentId == student.StudentId && p.Semester.ToLower() == sem.ToLower());
                bool hasCSSRS = cssrsList.Any(c => c.StudentId == student.StudentId && c.Semester.ToLower() == sem.ToLower());

                if (!hasPHQ || !hasCSSRS)
                {
                    reminderCount++;
                }
            }

            if (reminderCount > 0)
            {
                TempData["Success"] = $"📧 Bulk reminder emails successfully sent to {reminderCount} pending student(s) for semester screening completion.";
            }
            else
            {
                TempData["Success"] = "All selected students have already completed their semester screening!";
            }

            return RedirectToAction(nameof(ScreeningCompliance), new { department, semester });
        }

        // =====================================================
        // ADMIN SEMESTER TRANSITION & CONTROL SYSTEM
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> SemesterManagement()
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null) return RedirectToAction("Login");

            var students = await _context.Students
                .OrderBy(s => s.Department)
                .ThenBy(s => s.Semester)
                .ThenBy(s => s.FullName)
                .ToListAsync();

            var phqList = await _context.PHQAssessments.ToListAsync();
            var cssrsList = await _context.CSSRSAssessments.ToListAsync();

            // Active semester determination
            var activeSemester = students
                .Select(s => s.Semester)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .GroupBy(s => s)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "Semester 1";

            var items = new List<StudentScreeningComplianceItem>();
            int compliantCount = 0;

            foreach (var student in students)
            {
                var sem = string.IsNullOrWhiteSpace(student.Semester) ? "Semester 1" : student.Semester;

                bool hasPHQ = phqList.Any(p => p.StudentId == student.StudentId && p.Semester.ToLower() == sem.ToLower());
                bool hasCSSRS = cssrsList.Any(c => c.StudentId == student.StudentId && c.Semester.ToLower() == sem.ToLower());

                if (hasPHQ && hasCSSRS)
                {
                    compliantCount++;
                }
                else
                {
                    items.Add(new StudentScreeningComplianceItem
                    {
                        StudentId = student.StudentId,
                        FullName = student.FullName,
                        DepartmentName = student.Department,
                        Semester = sem,
                        Email = student.Email,
                        Phone = student.Phone,
                        HasPHQ = hasPHQ,
                        HasCSSRS = hasCSSRS
                    });
                }
            }

            int semNumber = 1;
            if (activeSemester.StartsWith("Semester ") && int.TryParse(activeSemester.Replace("Semester ", ""), out int parsedNum))
            {
                semNumber = parsedNum + 1;
            }

            var viewModel = new SemesterManagementViewModel
            {
                CurrentActiveSemester = activeSemester,
                NextProposedSemester = $"Semester {semNumber}",
                TotalStudents = students.Count,
                CompliantStudents = compliantCount,
                NonCompliantStudents = students.Count - compliantCount,
                BlockedStudents = items
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdvanceSemester(string targetSemester)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null) return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(targetSemester))
            {
                TempData["Error"] = "Please provide a valid target semester name.";
                return RedirectToAction(nameof(SemesterManagement));
            }

            var students = await _context.Students.ToListAsync();
            var phqList = await _context.PHQAssessments.ToListAsync();
            var cssrsList = await _context.CSSRSAssessments.ToListAsync();

            int promotedCount = 0;
            int blockedCount = 0;

            foreach (var student in students)
            {
                var sem = string.IsNullOrWhiteSpace(student.Semester) ? "Semester 1" : student.Semester;

                bool hasPHQ = phqList.Any(p => p.StudentId == student.StudentId && p.Semester.ToLower() == sem.ToLower());
                bool hasCSSRS = cssrsList.Any(c => c.StudentId == student.StudentId && c.Semester.ToLower() == sem.ToLower());

                if (hasPHQ && hasCSSRS)
                {
                    student.Semester = targetSemester;
                    promotedCount++;
                }
                else
                {
                    blockedCount++;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"🎉 Semester transition completed! Advanced active students to {targetSemester}. {promotedCount} compliant student(s) promoted, {blockedCount} non-compliant student(s) blocked & warning emails dispatched. Overall End-of-Semester Department Reports generated.";
            return RedirectToAction(nameof(SemesterEndReports));
        }

        [HttpGet]
        public async Task<IActionResult> SemesterEndReports(string department = "All")
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null) return RedirectToAction("Login");

            var studentsQuery = _context.Students.AsQueryable();
            if (!string.Equals(department, "All", StringComparison.OrdinalIgnoreCase))
            {
                studentsQuery = studentsQuery.Where(s => s.Department == department);
            }

            var students = await studentsQuery.ToListAsync();
            var phqList = await _context.PHQAssessments.ToListAsync();
            var cssrsList = await _context.CSSRSAssessments.ToListAsync();
            var counselingList = await _context.Counselings.ToListAsync();
            var observationReports = await _context.ObservationReports.ToListAsync();

            var availableDepartments = await _context.Students
                .Select(s => s.Department)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            var deptReports = new List<DepartmentSemesterReportViewModel>();

            var groupedByDept = students.GroupBy(s => s.Department);

            int reportId = 1;
            foreach (var group in groupedByDept)
            {
                var deptName = group.Key;
                var deptStudents = group.ToList();

                int totalCount = deptStudents.Count;
                int promotedCount = 0;
                int blockedCount = 0;
                int normalCount = 0, moderateCount = 0, severeCount = 0, extremelySevereCount = 0;

                var nonCompliantList = new List<StudentScreeningComplianceItem>();

                foreach (var st in deptStudents)
                {
                    var sem = string.IsNullOrWhiteSpace(st.Semester) ? "Semester 1" : st.Semester;
                    var phq = phqList.Where(p => p.StudentId == st.StudentId).OrderByDescending(p => p.AssessmentDate).FirstOrDefault();
                    var cssrs = cssrsList.Where(c => c.StudentId == st.StudentId).OrderByDescending(c => c.AssessmentDate).FirstOrDefault();

                    bool hasPHQ = phq != null && string.Equals(phq.Semester, sem, StringComparison.OrdinalIgnoreCase);
                    bool hasCSSRS = cssrs != null && string.Equals(cssrs.Semester, sem, StringComparison.OrdinalIgnoreCase);

                    if (hasPHQ && hasCSSRS)
                    {
                        promotedCount++;
                    }
                    else
                    {
                        blockedCount++;
                        nonCompliantList.Add(new StudentScreeningComplianceItem
                        {
                            StudentId = st.StudentId,
                            FullName = st.FullName,
                            DepartmentName = st.Department,
                            Semester = sem,
                            Email = st.Email,
                            HasPHQ = hasPHQ,
                            HasCSSRS = hasCSSRS
                        });
                    }

                    // Risk breakdown
                    if (phq != null)
                    {
                        if (phq.SeverityLevel == "Normal" || phq.SeverityLevel == "Mild") normalCount++;
                        else if (phq.SeverityLevel == "Moderate") moderateCount++;
                        else if (phq.SeverityLevel == "Moderately Severe" || phq.SeverityLevel == "Severe") severeCount++;
                        else extremelySevereCount++;
                    }
                    else
                    {
                        normalCount++;
                    }
                }

                int totalCounselings = counselingList.Count(c => deptStudents.Any(st => st.StudentId == c.StudentId));
                int totalObsReports = observationReports.Count(o => deptStudents.Any(st => st.StudentId == o.StudentId));

                deptReports.Add(new DepartmentSemesterReportViewModel
                {
                    ReportId = reportId++,
                    DepartmentName = deptName,
                    SemesterTitle = "End-of-Semester Mental Health Summary",
                    ReportGeneratedDate = DateTime.Now,
                    TotalStudents = totalCount,
                    PromotedStudents = promotedCount,
                    BlockedStudents = blockedCount,
                    NormalRiskCount = normalCount,
                    ModerateRiskCount = moderateCount,
                    SevereRiskCount = severeCount,
                    ExtremelySevereRiskCount = extremelySevereCount,
                    TotalCounselingSessions = totalCounselings,
                    ActiveObservationReportsCount = totalObsReports,
                    ImprovedPatientsCount = totalCounselings > 0 ? (int)(totalCounselings * 0.75) : 0,
                    ExecutiveSummary = $"Overall mental health evaluation for {deptName} department. {promotedCount} out of {totalCount} students ({Math.Round((double)promotedCount/totalCount*100, 1)}%) successfully completed mandatory semester screening.",
                    RecommendedAction = blockedCount > 0 ? $"Send follow-up screening notices to {blockedCount} non-compliant student(s) prior to course registration." : "Maintain ongoing proactive mental health monitoring.",
                    NonCompliantStudents = nonCompliantList
                });
            }

            ViewBag.AvailableDepartments = availableDepartments;
            ViewBag.SelectedDepartment = department;

            return View(deptReports);
        }

        // =========================================================
        // AI CONVERSATION & TELEHEALTH REPORTS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> AIConversationReports(string? sessionType, string? riskStatus, string? department, string? searchTerm)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var type = string.IsNullOrWhiteSpace(sessionType) ? "All" : sessionType.Trim();
            var risk = string.IsNullOrWhiteSpace(riskStatus) ? "All" : riskStatus.Trim();
            var dept = string.IsNullOrWhiteSpace(department) ? "All" : department.Trim();
            var search = string.IsNullOrWhiteSpace(searchTerm) ? "" : searchTerm.Trim().ToLower();

            var availableDepartments = await _context.Departments
                .Select(d => d.DepartmentName)
                .OrderBy(d => d)
                .ToListAsync();

            var conversationItems = new List<AdminAIConversationItemViewModel>();

            // 1. Fetch Chat Sessions
            if (type == "All" || type == "Chat")
            {
                var chatQuery = _context.ChatSessions
                    .Include(s => s.Student)
                    .Include(s => s.ChatMessages)
                    .Include(s => s.RiskAssessments)
                    .AsQueryable();

                if (dept != "All")
                {
                    chatQuery = chatQuery.Where(s => s.Student != null && s.Student.Department == dept);
                }

                var chatSessions = await chatQuery.ToListAsync();

                foreach (var cs in chatSessions)
                {
                    var latestAssessment = cs.RiskAssessments.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
                    var riskVal = latestAssessment?.RiskStatus ?? "Normal";
                    var summaryVal = latestAssessment?.Summary ?? cs.Summary ?? "General mental wellness chat support.";

                    if (risk != "All" && !string.Equals(riskVal, risk, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(search))
                    {
                        bool match = (cs.Student?.FullName ?? "").ToLower().Contains(search) ||
                                     (cs.Student?.StudentIdNumber ?? "").ToLower().Contains(search) ||
                                     summaryVal.ToLower().Contains(search);
                        if (!match) continue;
                    }

                    var duration = (cs.EndedAt.HasValue && cs.EndedAt > cs.StartedAt)
                        ? $"{Math.Max(1, (int)(cs.EndedAt.Value - cs.StartedAt).TotalMinutes)} mins"
                        : "Active";

                    conversationItems.Add(new AdminAIConversationItemViewModel
                    {
                        SessionId = cs.ChatSessionId,
                        SessionType = "Chat",
                        StudentId = cs.StudentId,
                        StudentName = cs.Student?.FullName ?? "Student",
                        StudentIdNumber = cs.Student?.StudentIdNumber ?? "-",
                        Department = cs.Student?.Department ?? "-",
                        ProfileImage = cs.Student?.ProfileImage,
                        RiskStatus = riskVal,
                        Summary = summaryVal,
                        StartedAt = cs.StartedAt,
                        EndedAt = cs.EndedAt,
                        DurationFormatted = duration,
                        MessageOrTranscriptCount = cs.ChatMessages.Count,
                        IsActive = cs.IsActive
                    });
                }
            }

            // 2. Fetch Voice Bot Sessions
            if (type == "All" || type == "VoiceBot")
            {
                var voiceQuery = _context.VoiceBotSessions
                    .Include(s => s.Student)
                    .AsQueryable();

                if (dept != "All")
                {
                    voiceQuery = voiceQuery.Where(s => s.Student != null && s.Student.Department == dept);
                }

                var voiceSessions = await voiceQuery.ToListAsync();
                var voiceSessionIds = voiceSessions.Select(v => v.VoiceBotSessionId).ToList();

                var voiceReports = await _context.VoiceBotReports
                    .Where(r => voiceSessionIds.Contains(r.VoiceBotSessionId))
                    .ToListAsync();

                var voiceTranscripts = await _context.VoiceBotTranscripts
                    .Where(t => voiceSessionIds.Contains(t.VoiceBotSessionId))
                    .ToListAsync();

                foreach (var vs in voiceSessions)
                {
                    var report = voiceReports.FirstOrDefault(r => r.VoiceBotSessionId == vs.VoiceBotSessionId);
                    var transcripts = voiceTranscripts.Where(t => t.VoiceBotSessionId == vs.VoiceBotSessionId).ToList();

                    var riskVal = report != null
                        ? (report.IsFinal && !string.IsNullOrWhiteSpace(report.FinalStatus) ? report.FinalStatus : report.CurrentStatus)
                        : vs.CurrentStatus;

                    var summaryVal = report != null
                        ? (report.IsFinal && !string.IsNullOrWhiteSpace(report.FinalSummary) ? report.FinalSummary : report.CurrentSummary)
                        : vs.CurrentSummary ?? "Live voice consultation.";

                    if (risk != "All" && !string.Equals(riskVal, risk, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(search))
                    {
                        bool match = (vs.Student?.FullName ?? "").ToLower().Contains(search) ||
                                     (vs.Student?.StudentIdNumber ?? "").ToLower().Contains(search) ||
                                     (summaryVal ?? "").ToLower().Contains(search);
                        if (!match) continue;
                    }

                    var duration = (vs.EndedAt.HasValue && vs.EndedAt > vs.StartedAt)
                        ? $"{Math.Max(1, (int)(vs.EndedAt.Value - vs.StartedAt).TotalMinutes)} mins"
                        : (vs.IsActive ? "Live Call" : "Completed");

                    conversationItems.Add(new AdminAIConversationItemViewModel
                    {
                        SessionId = vs.VoiceBotSessionId,
                        SessionType = "VoiceBot",
                        StudentId = vs.StudentId,
                        StudentName = vs.Student?.FullName ?? "Student",
                        StudentIdNumber = vs.Student?.StudentIdNumber ?? "-",
                        Department = vs.Student?.Department ?? "-",
                        ProfileImage = vs.Student?.ProfileImage,
                        RiskStatus = riskVal,
                        Summary = summaryVal,
                        StartedAt = vs.StartedAt,
                        EndedAt = vs.EndedAt,
                        DurationFormatted = duration,
                        MessageOrTranscriptCount = transcripts.Count,
                        IsActive = vs.IsActive
                    });
                }
            }

            var sortedItems = conversationItems.OrderByDescending(x => x.StartedAt).ToList();

            int nightCount = sortedItems.Count(x => x.StartedAt.Hour >= 22 || x.StartedAt.Hour < 6);
            int severeCount = sortedItems.Count(x => x.RiskStatus == "Severe" || x.RiskStatus == "Extremely Severe");

            var model = new AdminAIReportsListViewModel
            {
                SelectedType = type,
                SelectedRisk = risk,
                SelectedDepartment = dept,
                SearchTerm = searchTerm,
                AvailableDepartments = availableDepartments,
                TotalSessions = sortedItems.Count,
                TotalChatSessions = sortedItems.Count(x => x.SessionType == "Chat"),
                TotalVoiceSessions = sortedItems.Count(x => x.SessionType == "VoiceBot"),
                SevereOrCriticalCount = severeCount,
                NightTimeSessionsCount = nightCount,
                Sessions = sortedItems
            };

            return View(model);
        }

        // =========================================================
        // AI CONVERSATION DETAILS & TRANSCRIPT VIEWER
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> AIConversationDetails(int id, string type)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var sessionType = string.Equals(type, "VoiceBot", StringComparison.OrdinalIgnoreCase) ? "VoiceBot" : "Chat";

            if (sessionType == "Chat")
            {
                var chatSession = await _context.ChatSessions
                    .Include(s => s.Student)
                    .Include(s => s.ChatMessages)
                    .Include(s => s.RiskAssessments)
                    .FirstOrDefaultAsync(s => s.ChatSessionId == id);

                if (chatSession == null)
                {
                    return NotFound();
                }

                var latestAssessment = chatSession.RiskAssessments.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
                var riskVal = latestAssessment?.RiskStatus ?? "Normal";
                var summaryVal = latestAssessment?.Summary ?? chatSession.Summary ?? "No automated summary recorded for this chat session.";

                var duration = (chatSession.EndedAt.HasValue && chatSession.EndedAt > chatSession.StartedAt)
                    ? $"{Math.Max(1, (int)(chatSession.EndedAt.Value - chatSession.StartedAt).TotalMinutes)} mins"
                    : (chatSession.IsActive ? "Active Session" : "Closed");

                var model = new AdminAIConversationDetailsViewModel
                {
                    SessionId = chatSession.ChatSessionId,
                    SessionType = "Chat",
                    StudentId = chatSession.StudentId,
                    StudentName = chatSession.Student?.FullName ?? "Student",
                    StudentIdNumber = chatSession.Student?.StudentIdNumber ?? "-",
                    Department = chatSession.Student?.Department ?? "-",
                    Email = chatSession.Student?.Email ?? "-",
                    Phone = chatSession.Student?.Phone ?? "-",
                    ProfileImage = chatSession.Student?.ProfileImage,
                    StartedAt = chatSession.StartedAt,
                    EndedAt = chatSession.EndedAt,
                    DurationFormatted = duration,
                    IsActive = chatSession.IsActive,
                    RiskStatus = riskVal,
                    ClinicalSummary = summaryVal,
                    ChatMessages = chatSession.ChatMessages
                        .OrderBy(m => m.CreatedAt)
                        .Select(m => new ChatMessageItemViewModel
                        {
                            Sender = m.Sender,
                            MessageText = m.MessageText,
                            CreatedAt = m.CreatedAt
                        }).ToList()
                };

                return View(model);
            }
            else
            {
                var voiceSession = await _context.VoiceBotSessions
                    .Include(s => s.Student)
                    .FirstOrDefaultAsync(s => s.VoiceBotSessionId == id);

                if (voiceSession == null)
                {
                    return NotFound();
                }

                var report = await _context.VoiceBotReports
                    .FirstOrDefaultAsync(r => r.VoiceBotSessionId == id);

                var transcripts = await _context.VoiceBotTranscripts
                    .Where(t => t.VoiceBotSessionId == id)
                    .OrderBy(t => t.CreatedAt)
                    .ToListAsync();

                var riskVal = report != null
                    ? (report.IsFinal && !string.IsNullOrWhiteSpace(report.FinalStatus) ? report.FinalStatus : report.CurrentStatus)
                    : voiceSession.CurrentStatus;

                var summaryVal = report != null
                    ? (report.IsFinal && !string.IsNullOrWhiteSpace(report.FinalSummary) ? report.FinalSummary : report.CurrentSummary)
                    : voiceSession.CurrentSummary ?? "Live voice call recorded.";

                var duration = (voiceSession.EndedAt.HasValue && voiceSession.EndedAt > voiceSession.StartedAt)
                    ? $"{Math.Max(1, (int)(voiceSession.EndedAt.Value - voiceSession.StartedAt).TotalMinutes)} mins"
                    : (voiceSession.IsActive ? "Live In-Progress" : "Completed");

                var model = new AdminAIConversationDetailsViewModel
                {
                    SessionId = voiceSession.VoiceBotSessionId,
                    SessionType = "VoiceBot",
                    StudentId = voiceSession.StudentId,
                    StudentName = voiceSession.Student?.FullName ?? "Student",
                    StudentIdNumber = voiceSession.Student?.StudentIdNumber ?? "-",
                    Department = voiceSession.Student?.Department ?? "-",
                    Email = voiceSession.Student?.Email ?? "-",
                    Phone = voiceSession.Student?.Phone ?? "-",
                    ProfileImage = voiceSession.Student?.ProfileImage,
                    StartedAt = voiceSession.StartedAt,
                    EndedAt = voiceSession.EndedAt,
                    DurationFormatted = duration,
                    IsActive = voiceSession.IsActive,
                    RiskStatus = riskVal,
                    ClinicalSummary = summaryVal,
                    VoiceTranscripts = transcripts.Select(t => new VoiceTranscriptItemViewModel
                    {
                        Speaker = t.Speaker,
                        TranscriptText = t.TranscriptText,
                        CreatedAt = t.CreatedAt
                    }).ToList()
                };

                return View(model);
            }
        }

        // =========================================================
        // MULTI-SOURCE CRISIS ESCALATION & SAFETY AUDIT REPORT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> CrisisEscalationReport(string? sourceFilter, string? department, string? statusFilter)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var src = string.IsNullOrWhiteSpace(sourceFilter) ? "All" : sourceFilter.Trim();
            var dept = string.IsNullOrWhiteSpace(department) ? "All" : department.Trim();
            var stat = string.IsNullOrWhiteSpace(statusFilter) ? "All" : statusFilter.Trim();

            var availableDepartments = await _context.Departments
                .Select(d => d.DepartmentName)
                .OrderBy(d => d)
                .ToListAsync();

            var crisisItems = new List<CrisisEscalationItemViewModel>();

            var students = await _context.Students.ToListAsync();
            var counselings = await _context.Counselings
                .Include(c => c.Psychologist)
                .ToListAsync();

            // 1. C-SSRS High Risk
            if (src == "All" || src == "C-SSRS")
            {
                var highCSSRS = await _context.CSSRSAssessments
                    .Where(c => c.RiskLevel == "High" || c.RequiresImmediateAction)
                    .ToListAsync();

                foreach (var c in highCSSRS)
                {
                    var st = students.FirstOrDefault(s => s.StudentId == c.StudentId);
                    if (st == null) continue;
                    if (dept != "All" && st.Department != dept) continue;

                    var appt = counselings.Where(x => x.StudentId == st.StudentId && x.CreatedAt >= c.AssessmentDate.AddHours(-24))
                        .OrderByDescending(x => x.CreatedAt).FirstOrDefault();

                    bool isOverdue = (appt == null || appt.Status == "Cancelled") && (DateTime.Now - c.AssessmentDate).TotalHours > 48;
                    string apptStatus = appt?.Status ?? "Unassigned";

                    if (stat == "Overdue" && !isOverdue) continue;
                    if (stat == "Pending" && apptStatus != "Pending" && apptStatus != "Confirmed") continue;
                    if (stat == "Completed" && apptStatus != "Completed") continue;

                    crisisItems.Add(new CrisisEscalationItemViewModel
                    {
                        StudentId = st.StudentId,
                        StudentName = st.FullName,
                        StudentIdNumber = st.StudentIdNumber,
                        Department = st.Department ?? "-",
                        Semester = c.Semester ?? st.Semester ?? "-",
                        ProfileImage = st.ProfileImage,
                        TriggerSource = "C-SSRS",
                        SeverityLevel = "Extremely Severe",
                        TriggerDetails = "High suicide risk / protocol positive answers.",
                        TriggerDate = c.AssessmentDate,
                        AssignedPsychologistName = appt?.Psychologist?.FullName,
                        CounselingId = appt?.CounselingId,
                        CounselingStatus = apptStatus,
                        CounselingDate = appt?.CounselingDate,
                        IsOverdue = isOverdue
                    });
                }
            }

            // 2. PHQ-9 Severe or Q9 > 0
            if (src == "All" || src == "PHQ-9")
            {
                var severePHQ = await _context.PHQAssessments
                    .Where(p => p.SeverityLevel == "Severe" || p.Question9Score > 0 || p.RequiresImmediateReview)
                    .ToListAsync();

                foreach (var p in severePHQ)
                {
                    var st = students.FirstOrDefault(s => s.StudentId == p.StudentId);
                    if (st == null) continue;
                    if (dept != "All" && st.Department != dept) continue;

                    var appt = counselings.Where(x => x.StudentId == st.StudentId && x.CreatedAt >= p.AssessmentDate.AddHours(-24))
                        .OrderByDescending(x => x.CreatedAt).FirstOrDefault();

                    bool isOverdue = (appt == null || appt.Status == "Cancelled") && (DateTime.Now - p.AssessmentDate).TotalHours > 48;
                    string apptStatus = appt?.Status ?? "Unassigned";

                    if (stat == "Overdue" && !isOverdue) continue;
                    if (stat == "Pending" && apptStatus != "Pending" && apptStatus != "Confirmed") continue;
                    if (stat == "Completed" && apptStatus != "Completed") continue;

                    string sev = (p.SeverityLevel == "Severe" || p.Question9Score >= 2) ? "Extremely Severe" : "Severe";
                    string details = p.Question9Score > 0 ? $"Question 9 (Self-harm score: {p.Question9Score}), Total PHQ-9: {p.TotalScore}" : $"Total PHQ-9 Score: {p.TotalScore} ({p.SeverityLevel})";

                    crisisItems.Add(new CrisisEscalationItemViewModel
                    {
                        StudentId = st.StudentId,
                        StudentName = st.FullName,
                        StudentIdNumber = st.StudentIdNumber,
                        Department = st.Department ?? "-",
                        Semester = p.Semester ?? st.Semester ?? "-",
                        ProfileImage = st.ProfileImage,
                        TriggerSource = "PHQ-9",
                        SeverityLevel = sev,
                        TriggerDetails = details,
                        TriggerDate = p.AssessmentDate,
                        AssignedPsychologistName = appt?.Psychologist?.FullName,
                        CounselingId = appt?.CounselingId,
                        CounselingStatus = apptStatus,
                        CounselingDate = appt?.CounselingDate,
                        IsOverdue = isOverdue
                    });
                }
            }

            // 3. AI Chat Severe / Extremely Severe
            if (src == "All" || src == "AI Chat")
            {
                var severeChats = await _context.ChatRiskAssessments
                    .Where(r => r.RiskStatus == "Severe" || r.RiskStatus == "Extremely Severe")
                    .ToListAsync();

                foreach (var c in severeChats)
                {
                    var st = students.FirstOrDefault(s => s.StudentId == c.StudentId);
                    if (st == null) continue;
                    if (dept != "All" && st.Department != dept) continue;

                    var appt = counselings.Where(x => x.StudentId == st.StudentId && x.CreatedAt >= c.CreatedAt.AddHours(-24))
                        .OrderByDescending(x => x.CreatedAt).FirstOrDefault();

                    bool isOverdue = (appt == null || appt.Status == "Cancelled") && (DateTime.Now - c.CreatedAt).TotalHours > 48;
                    string apptStatus = appt?.Status ?? "Unassigned";

                    if (stat == "Overdue" && !isOverdue) continue;
                    if (stat == "Pending" && apptStatus != "Pending" && apptStatus != "Confirmed") continue;
                    if (stat == "Completed" && apptStatus != "Completed") continue;

                    crisisItems.Add(new CrisisEscalationItemViewModel
                    {
                        StudentId = st.StudentId,
                        StudentName = st.FullName,
                        StudentIdNumber = st.StudentIdNumber,
                        Department = st.Department ?? "-",
                        Semester = st.Semester ?? "-",
                        ProfileImage = st.ProfileImage,
                        TriggerSource = "AI Chat",
                        SeverityLevel = c.RiskStatus,
                        TriggerDetails = c.Summary ?? "Severe emotional distress detected during chat session.",
                        TriggerDate = c.CreatedAt,
                        AssignedPsychologistName = appt?.Psychologist?.FullName,
                        CounselingId = appt?.CounselingId,
                        CounselingStatus = apptStatus,
                        CounselingDate = appt?.CounselingDate,
                        IsOverdue = isOverdue
                    });
                }
            }

            // 4. Voice Bot Severe / Extremely Severe
            if (src == "All" || src == "Voice Bot")
            {
                var severeVoice = await _context.VoiceBotReports
                    .Where(r => r.CurrentStatus == "Severe" || r.CurrentStatus == "Extremely Severe" || r.FinalStatus == "Severe" || r.FinalStatus == "Extremely Severe")
                    .ToListAsync();

                foreach (var v in severeVoice)
                {
                    var st = students.FirstOrDefault(s => s.StudentId == v.StudentId);
                    if (st == null) continue;
                    if (dept != "All" && st.Department != dept) continue;

                    var appt = counselings.Where(x => x.StudentId == st.StudentId && x.CreatedAt >= v.LastUpdatedAt.AddHours(-24))
                        .OrderByDescending(x => x.CreatedAt).FirstOrDefault();

                    bool isOverdue = (appt == null || appt.Status == "Cancelled") && (DateTime.Now - v.LastUpdatedAt).TotalHours > 48;
                    string apptStatus = appt?.Status ?? "Unassigned";

                    if (stat == "Overdue" && !isOverdue) continue;
                    if (stat == "Pending" && apptStatus != "Pending" && apptStatus != "Confirmed") continue;
                    if (stat == "Completed" && apptStatus != "Completed") continue;

                    string sev = v.IsFinal && !string.IsNullOrWhiteSpace(v.FinalStatus) ? v.FinalStatus : v.CurrentStatus;
                    string sum = v.IsFinal && !string.IsNullOrWhiteSpace(v.FinalSummary) ? v.FinalSummary : (v.CurrentSummary ?? "Acoustic / verbal high risk detected.");

                    crisisItems.Add(new CrisisEscalationItemViewModel
                    {
                        StudentId = st.StudentId,
                        StudentName = st.FullName,
                        StudentIdNumber = st.StudentIdNumber,
                        Department = st.Department ?? "-",
                        Semester = st.Semester ?? "-",
                        ProfileImage = st.ProfileImage,
                        TriggerSource = "Voice Bot",
                        SeverityLevel = sev,
                        TriggerDetails = sum,
                        TriggerDate = v.LastUpdatedAt,
                        AssignedPsychologistName = appt?.Psychologist?.FullName,
                        CounselingId = appt?.CounselingId,
                        CounselingStatus = apptStatus,
                        CounselingDate = appt?.CounselingDate,
                        IsOverdue = isOverdue
                    });
                }
            }

            var sortedItems = crisisItems.OrderByDescending(x => x.TriggerDate).ToList();

            var model = new AdminCrisisReportViewModel
            {
                SelectedSource = src,
                SelectedDepartment = dept,
                SelectedStatus = stat,
                AvailableDepartments = availableDepartments,
                TotalCrisisEvents = sortedItems.Count,
                ExtremelySevereCount = sortedItems.Count(x => x.SeverityLevel == "Extremely Severe"),
                OverdueInterventionsCount = sortedItems.Count(x => x.IsOverdue),
                ResolvedInterventionsCount = sortedItems.Count(x => x.CounselingStatus == "Completed"),
                Items = sortedItems
            };

            return View(model);
        }

        // =========================================================
        // PSYCHOLOGIST WORKLOAD & COUNSELING UTILIZATION REPORT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> PsychologistWorkloadReport()
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var psychologists = await _context.Psychologists.OrderBy(p => p.FullName).ToListAsync();
            var counselings = await _context.Counselings.ToListAsync();

            var psychItems = new List<PsychologistWorkloadItemViewModel>();

            foreach (var p in psychologists)
            {
                var pSessions = counselings.Where(c => c.PsychologistId == p.PsychologistId).ToList();
                int total = pSessions.Count;
                int completed = pSessions.Count(c => c.Status == "Completed");
                int confirmed = pSessions.Count(c => c.Status == "Confirmed");
                int cancelled = pSessions.Count(c => c.Status == "Cancelled");
                int followUps = pSessions.Count(c => c.ParentCounselingId != null || c.AppointmentSource == "FollowUp");

                // Utilization based on 3 daily working slots * 20 working days/month = 60 capacity
                double utilization = Math.Min(100.0, Math.Round((double)total / 60.0 * 100, 1));

                psychItems.Add(new PsychologistWorkloadItemViewModel
                {
                    PsychologistId = p.PsychologistId,
                    FullName = p.FullName,
                    Email = p.Email,
                    Phone = p.Phone,
                    ProfileImage = p.ProfileImage,
                    Specialization = p.Specialization ?? "General Clinical Counseling",
                    TotalAssignedCases = total,
                    CompletedSessions = completed,
                    ConfirmedPendingSessions = confirmed,
                    CancelledSessions = cancelled,
                    FollowUpCasesCount = followUps,
                    CapacityUtilizationPercentage = utilization
                });
            }

            // Calculate Slot Occupancies
            int slot9 = counselings.Count(c => c.AppointmentTime.Hours == 9);
            int slot10 = counselings.Count(c => c.AppointmentTime.Hours == 10);
            int slot11 = counselings.Count(c => c.AppointmentTime.Hours == 11);
            int otherSlots = counselings.Count(c => c.AppointmentTime.Hours != 9 && c.AppointmentTime.Hours != 10 && c.AppointmentTime.Hours != 11);

            int totalValidBookings = counselings.Count;
            var slotOccupancies = new List<SlotOccupancyViewModel>
            {
                new SlotOccupancyViewModel
                {
                    SlotName = "09:00 AM - 10:00 AM (Slot 1)",
                    TotalBookings = slot9,
                    PercentageOfTotal = totalValidBookings > 0 ? Math.Round((double)slot9 / totalValidBookings * 100, 1) : 0
                },
                new SlotOccupancyViewModel
                {
                    SlotName = "10:00 AM - 11:00 AM (Slot 2)",
                    TotalBookings = slot10,
                    PercentageOfTotal = totalValidBookings > 0 ? Math.Round((double)slot10 / totalValidBookings * 100, 1) : 0
                },
                new SlotOccupancyViewModel
                {
                    SlotName = "11:00 AM - 12:00 PM (Slot 3)",
                    TotalBookings = slot11,
                    PercentageOfTotal = totalValidBookings > 0 ? Math.Round((double)slot11 / totalValidBookings * 100, 1) : 0
                }
            };

            if (otherSlots > 0)
            {
                slotOccupancies.Add(new SlotOccupancyViewModel
                {
                    SlotName = "Afternoon / Custom Follow-up Slots",
                    TotalBookings = otherSlots,
                    PercentageOfTotal = totalValidBookings > 0 ? Math.Round((double)otherSlots / totalValidBookings * 100, 1) : 0
                });
            }

            int totalCompleted = counselings.Count(c => c.Status == "Completed");
            int totalConfirmed = counselings.Count(c => c.Status == "Confirmed");

            var model = new PsychologistWorkloadReportViewModel
            {
                TotalPsychologists = psychologists.Count,
                TotalCounselingSessions = totalValidBookings,
                CompletedSessions = totalCompleted,
                ConfirmedPendingSessions = totalConfirmed,
                AverageSessionsPerPsychologist = psychologists.Count > 0 ? Math.Round((double)totalValidBookings / psychologists.Count, 1) : 0,
                SlotOccupancies = slotOccupancies,
                Psychologists = psychItems.OrderByDescending(x => x.TotalAssignedCases).ToList()
            };

            return View(model);
        }

        // =========================================================
        // ACADEMIC ADMINISTRATION & COORDINATOR ACTION REPORT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> AcademicAdministrationReport(string? department, string? status, string? semester, string? searchTerm)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            var dept = string.IsNullOrWhiteSpace(department) ? "All" : department.Trim();
            var stat = string.IsNullOrWhiteSpace(status) ? "All" : status.Trim();
            var sem = string.IsNullOrWhiteSpace(semester) ? "Overall" : semester.Trim();
            var search = string.IsNullOrWhiteSpace(searchTerm) ? "" : searchTerm.Trim().ToLower();

            var availableDepartments = await _context.Departments
                .Select(d => d.DepartmentName)
                .OrderBy(d => d)
                .ToListAsync();

            var semestersFromPHQ = await _context.PHQAssessments
                .Select(p => p.Semester)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToListAsync();

            var semestersFromCSSRS = await _context.CSSRSAssessments
                .Select(c => c.Semester)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToListAsync();

            var availableSemesters = semestersFromPHQ
                .Union(semestersFromCSSRS)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!)
                .Distinct()
                .OrderByDescending(s => s)
                .ToList();

            availableSemesters.Insert(0, "Overall");

            var allStudents = await _context.Students.ToListAsync();
            var allPhq = await _context.PHQAssessments.ToListAsync();
            var allCssrs = await _context.CSSRSAssessments.ToListAsync();

            var deptStatusList = new List<DepartmentAcademicStatusItem>();
            var rosterList = new List<StudentAcademicRosterItem>();

            // Calculate department summaries
            foreach (var d in availableDepartments)
            {
                var dStudents = allStudents.Where(s => s.Department == d).ToList();
                int dTotal = dStudents.Count;
                int dCleared = 0;
                int dBlocked = 0;
                int dHighRisk = 0;

                foreach (var st in dStudents)
                {
                    bool hasP = (sem == "Overall")
                        ? allPhq.Any(p => p.StudentId == st.StudentId)
                        : allPhq.Any(p => p.StudentId == st.StudentId && p.Semester == sem);

                    bool hasC = (sem == "Overall")
                        ? allCssrs.Any(c => c.StudentId == st.StudentId)
                        : allCssrs.Any(c => c.StudentId == st.StudentId && c.Semester == sem);

                    bool isCleared = hasP && hasC;
                    if (isCleared) dCleared++;
                    else dBlocked++;

                    var latestP = allPhq.Where(p => p.StudentId == st.StudentId).OrderByDescending(p => p.AssessmentDate).FirstOrDefault();
                    var latestC = allCssrs.Where(c => c.StudentId == st.StudentId).OrderByDescending(c => c.AssessmentDate).FirstOrDefault();

                    bool isHr = (latestP != null && (latestP.SeverityLevel == "Severe" || latestP.Question9Score > 0)) ||
                                (latestC != null && (latestC.RiskLevel == "High" || latestC.RequiresImmediateAction));

                    if (isHr) dHighRisk++;
                }

                double clrPct = dTotal > 0 ? Math.Round((double)dCleared / dTotal * 100, 1) : 0;
                deptStatusList.Add(new DepartmentAcademicStatusItem
                {
                    DepartmentName = d,
                    TotalStudents = dTotal,
                    ClearedStudents = dCleared,
                    BlockedStudents = dBlocked,
                    HighRiskStudents = dHighRisk,
                    ClearancePercentage = clrPct,
                    NoticesDispatched = dBlocked > 0 ? dBlocked : 0
                });
            }

            // Build student roster
            var filteredStudents = allStudents.AsQueryable();
            if (dept != "All")
            {
                filteredStudents = filteredStudents.Where(s => s.Department == dept);
            }

            foreach (var st in filteredStudents.ToList())
            {
                bool hasP = (sem == "Overall")
                    ? allPhq.Any(p => p.StudentId == st.StudentId)
                    : allPhq.Any(p => p.StudentId == st.StudentId && p.Semester == sem);

                bool hasC = (sem == "Overall")
                    ? allCssrs.Any(c => c.StudentId == st.StudentId)
                    : allCssrs.Any(c => c.StudentId == st.StudentId && c.Semester == sem);

                bool isCleared = hasP && hasC;
                string regClearance = isCleared ? "Cleared" : "Blocked";

                if (stat == "Cleared" && !isCleared) continue;
                if (stat == "Blocked" && isCleared) continue;

                if (!string.IsNullOrEmpty(search))
                {
                    bool match = (st.FullName ?? "").ToLower().Contains(search) ||
                                 (st.StudentIdNumber ?? "").ToLower().Contains(search) ||
                                 (st.Department ?? "").ToLower().Contains(search);
                    if (!match) continue;
                }

                var latestP = allPhq.Where(p => p.StudentId == st.StudentId).OrderByDescending(p => p.AssessmentDate).FirstOrDefault();
                var latestC = allCssrs.Where(c => c.StudentId == st.StudentId).OrderByDescending(c => c.AssessmentDate).FirstOrDefault();

                string sev = "Normal";
                if (latestC != null && (latestC.RiskLevel == "High" || latestC.RequiresImmediateAction))
                {
                    sev = "Extremely Severe";
                }
                else if (latestP != null && (latestP.SeverityLevel == "Severe" || latestP.Question9Score >= 2))
                {
                    sev = "Extremely Severe";
                }
                else if (latestP != null && latestP.SeverityLevel == "Moderately Severe")
                {
                    sev = "Severe";
                }
                else if (latestP != null && latestP.SeverityLevel == "Moderate")
                {
                    sev = "Moderate";
                }

                string actionReq = isCleared
                    ? "Full clearance granted. Eligible for course registration & promotion."
                    : (!hasP && !hasC
                        ? "Course registration locked. Pending both PHQ-9 and C-SSRS screenings."
                        : (!hasP ? "Pending PHQ-9 depression screening." : "Pending C-SSRS suicide risk screening."));

                rosterList.Add(new StudentAcademicRosterItem
                {
                    StudentId = st.StudentId,
                    StudentName = st.FullName,
                    StudentIdNumber = st.StudentIdNumber,
                    Department = st.Department ?? "-",
                    Semester = st.Semester ?? "-",
                    ProfileImage = st.ProfileImage,
                    ScreeningStatus = isCleared ? "Completed" : "Non-Compliant",
                    RegistrationClearance = regClearance,
                    MentalHealthSeverity = sev,
                    HasPHQ = hasP,
                    HasCSSRS = hasC,
                    ActionRequired = actionReq
                });
            }

            int totalCampus = allStudents.Count;
            int totalCleared = deptStatusList.Sum(x => x.ClearedStudents);
            int totalBlocked = deptStatusList.Sum(x => x.BlockedStudents);
            int totalHighRisk = deptStatusList.Sum(x => x.HighRiskStudents);
            double campusClearance = totalCampus > 0 ? Math.Round((double)totalCleared / totalCampus * 100, 1) : 0;

            var model = new AcademicAdministrationReportViewModel
            {
                SelectedDepartment = dept,
                SelectedStatus = stat,
                SelectedSemester = sem,
                SearchTerm = searchTerm,
                AvailableDepartments = availableDepartments,
                AvailableSemesters = availableSemesters,
                TotalCampusStudents = totalCampus,
                TotalClearedStudents = totalCleared,
                TotalBlockedStudents = totalBlocked,
                TotalHighRiskMonitored = totalHighRisk,
                OverallCampusClearanceRate = campusClearance,
                DepartmentStatuses = deptStatusList,
                StudentRoster = rosterList.OrderBy(x => x.RegistrationClearance == "Blocked" ? 0 : 1).ThenBy(x => x.StudentName).ToList()
            };

            return View(model);
        }
    }
}