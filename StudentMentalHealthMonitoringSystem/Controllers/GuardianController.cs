using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentMentalHealthMonitoringSystem.Data;

namespace StudentMentalHealthMonitoringSystem.Controllers
{
    public class GuardianController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GuardianController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // GUARDIAN LOGIN - GET
        // =====================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        // =====================================================
        // GUARDIAN LOGIN - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
            {
                ViewBag.Error = "Please enter Student ID.";

                return View();
            }


            studentId = studentId.Trim();


            // Find student using Student ID Number

            var student = await _context.Students
                .FirstOrDefaultAsync(
                    s => s.StudentIdNumber == studentId
                );


            // Student not found

            if (student == null)
            {
                ViewBag.Error = "Student ID not found.";

                return View();
            }


            // Save student information in session

            HttpContext.Session.SetInt32(
                "GuardianStudentId",
                student.StudentId
            );


            HttpContext.Session.SetString(
                "GuardianStudentName",
                student.FullName
            );


            // Go to Dashboard

            return RedirectToAction("Dashboard");
        }


        // =====================================================
        // GUARDIAN DASHBOARD
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // Get logged-in student's ID

            var studentId =
                HttpContext.Session.GetInt32(
                    "GuardianStudentId"
                );


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


            // =================================================
            // PHQ-9 REPORTS
            // =================================================

            var phqReports =
                await _context.PHQAssessments
                    .Where(
                        p => p.StudentId == studentId.Value
                    )
                    .OrderByDescending(
                        p => p.AssessmentDate
                    )
                    .ToListAsync();


            // =================================================
            // C-SSRS REPORTS
            // =================================================

            var cssrsReports =
                await _context.CSSRSAssessments
                    .Where(
                        c => c.StudentId == studentId.Value
                    )
                    .OrderByDescending(
                        c => c.AssessmentDate
                    )
                    .ToListAsync();


            // Send data to View

            ViewBag.Student = student;

            ViewBag.PHQReports = phqReports;

            ViewBag.CSSRSReports = cssrsReports;


            return View();
        }


        // =====================================================
        // GUARDIAN LOGOUT
        // =====================================================

        [HttpGet]
        public IActionResult Logout()
        {
            // Remove Guardian session

            HttpContext.Session.Clear();


            // Return to Login page

            return RedirectToAction("Login");
        }
    }
}