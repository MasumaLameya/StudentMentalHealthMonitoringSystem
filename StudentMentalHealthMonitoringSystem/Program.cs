using Microsoft.EntityFrameworkCore;
using StudentMentalHealthMonitoringSystem.Data;
using StudentMentalHealthMonitoringSystem.Models;
using StudentMentalHealthMonitoringSystem.Services;

var builder = WebApplication.CreateBuilder(args);


// =========================================================
// ADD MVC SERVICES
// =========================================================

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddHttpContextAccessor();


// =========================================================
// SESSION CONFIGURATION
// =========================================================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    // Session expires after 30 minutes of inactivity

    options.IdleTimeout =
        TimeSpan.FromMinutes(30);


    // Session cookie cannot be accessed by JavaScript

    options.Cookie.HttpOnly =
        true;


    // Session cookie is required

    options.Cookie.IsEssential =
        true;
});


// =========================================================
// MYSQL DATABASE CONFIGURATION
// =========================================================

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection"
    );


builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseMySql(
            connectionString,
            ServerVersion.Parse("10.4.32-mariadb"),
            mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null
            )
        )
);


// =========================================================
// EMAIL SERVICE
// =========================================================
// Used for sending reports and notifications.
// =========================================================

builder.Services.AddScoped<EmailService>();


// =========================================================
// COUNSELING SCHEDULER SERVICE
// =========================================================

builder.Services.AddScoped<CounselingSchedulerService>();


// =========================================================
// GEMINI AI CHAT SERVICE
// =========================================================
//
// GeminiChatService requires:
// - HttpClient
// - IConfiguration
//
// IConfiguration is provided automatically by ASP.NET Core.
//
// AddHttpClient registers GeminiChatService
// together with a managed HttpClient instance.
//
// Gemini configuration is read from:
//
// appsettings.json
//
// "Gemini": {
//     "ApiKey": "...",
//     "Model": "gemini-3.5-flash-lite"
// }
//
// =========================================================

builder.Services.AddHttpClient<GeminiChatService>();


// =========================================================
// GEMINI LIVE VOICE SERVICE
// =========================================================
//
// GeminiLiveVoiceService requires:
// - HttpClient
// - IConfiguration
//
// The service now manages:
//
// - Gemini ephemeral / temporary Live token creation
// - Server-side Gemini API key protection
// - Conversation status analysis
// - Voice monitoring summary analysis
//
// Browser audio no longer passes through ASP.NET.
//
// New Live Voice flow:
//
// Browser
//     ↓
// VoiceBotController.GetLiveToken()
//     ↓
// GeminiLiveVoiceService
//     ↓
// Temporary Live token
//     ↓
// Browser ↔ Gemini Live API directly
//
// Live Voice model is read from:
//
// "Gemini": {
//     "LiveVoiceModel":
//         "gemini-3.1-flash-live-preview"
// }
//
// Voice analysis model is read from:
//
// "Gemini": {
//     "VoiceAnalysisModel":
//         "gemini-3.7-flash"
// }
//
// Permanent API key stays on the server.
//
// =========================================================

builder.Services.AddHttpClient<GeminiLiveVoiceService>();


// =========================================================
// BUILD APPLICATION
// =========================================================

var app = builder.Build();


// =========================================================
// DATABASE SEEDING
// =========================================================

using (var scope = app.Services.CreateScope())
{
    try
    {
        // =====================================================
        // GET DATABASE CONTEXT & APPLY MIGRATIONS
        // =====================================================

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        try
        {
            context.Database.Migrate();
        }
        catch (Exception)
        {
            // Ignore migration lock/sync warnings
        }

        try
        {
            context.Database.ExecuteSqlRaw(@"
                ALTER TABLE `Students` ADD COLUMN `AdmissionYear` int NULL;
            ");
        }
        catch (Exception)
        {
            // Ignore if column already exists
        }

        // Backfill any NULL AdmissionYear in database with default 2024
        try
        {
            context.Database.ExecuteSqlRaw(@"
                UPDATE `Students` SET `AdmissionYear` = 2024 WHERE `AdmissionYear` IS NULL;
            ");
        }
        catch (Exception) { }

        // Ensure ObservationReports and CounselingObservations tables are healthy in engine
        bool obsTableHealthy = false;
        try
        {
            context.Database.ExecuteSqlRaw("SELECT 1 FROM `ObservationReports` LIMIT 1;");
            obsTableHealthy = true;
        }
        catch
        {
            obsTableHealthy = false;
        }

        if (!obsTableHealthy)
        {
            try
            {
                context.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS = 0;");
                context.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS `ObservationReports`;");
                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE `ObservationReports` (
                      `ObservationReportId` int NOT NULL AUTO_INCREMENT,
                      `RootCounselingId` int NOT NULL,
                      `StudentId` int NOT NULL,
                      `PsychologistId` int NOT NULL,
                      `InitialStatus` varchar(50) NOT NULL DEFAULT 'Not Assessed',
                      `CurrentStatus` varchar(50) NOT NULL DEFAULT 'Not Assessed',
                      `OverallProgressStatus` varchar(50) NOT NULL DEFAULT '',
                      `CurrentSafetyRisk` varchar(50) NOT NULL DEFAULT '',
                      `LatestAssessmentBasis` text NOT NULL,
                      `LatestRecommendedAction` text NOT NULL,
                      `LatestConditionSummary` text NOT NULL,
                      `IsFinal` tinyint(1) NOT NULL DEFAULT 0,
                      `FinalizedAt` datetime(6) NULL,
                      `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
                      `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
                      PRIMARY KEY (`ObservationReportId`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                ");
                context.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS = 1;");
            }
            catch { }
        }

        bool counselingObsTableHealthy = false;
        try
        {
            context.Database.ExecuteSqlRaw("SELECT 1 FROM `CounselingObservations` LIMIT 1;");
            counselingObsTableHealthy = true;
        }
        catch
        {
            counselingObsTableHealthy = false;
        }

        if (!counselingObsTableHealthy)
        {
            try
            {
                context.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS = 0;");
                context.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS `CounselingObservations`;");
                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE `CounselingObservations` (
                      `CounselingObservationId` int NOT NULL AUTO_INCREMENT,
                      `CounselingId` int NOT NULL,
                      `RootCounselingId` int NOT NULL,
                      `StudentId` int NOT NULL,
                      `PsychologistId` int NOT NULL,
                      `OverallProgressStatus` varchar(50) NOT NULL DEFAULT '',
                      `CurrentMentalHealthStatus` varchar(50) NOT NULL DEFAULT '',
                      `PHQScore` int NULL,
                      `PHQOfficialInterpretation` varchar(100) NULL,
                      `PHQProjectStatus` varchar(50) NULL,
                      `CSSRSRiskLevel` varchar(50) NULL,
                      `CSSRSProjectStatus` varchar(50) NULL,
                      `AcademicFunctioning` varchar(50) NOT NULL DEFAULT '',
                      `SleepCondition` varchar(50) NOT NULL DEFAULT '',
                      `SocialInteraction` varchar(50) NOT NULL DEFAULT '',
                      `DailyActivities` varchar(50) NOT NULL DEFAULT '',
                      `EmotionalRegulation` varchar(50) NOT NULL DEFAULT '',
                      `CurrentSafetyRisk` varchar(50) NOT NULL DEFAULT '',
                      `AssessmentBasis` text NOT NULL,
                      `ClinicalObservation` text NOT NULL,
                      `StudentReportedImprovement` varchar(50) NOT NULL DEFAULT '',
                      `AssessmentSummary` text NOT NULL,
                      `RecommendedAction` text NOT NULL,
                      `FollowUpRequired` tinyint(1) NOT NULL DEFAULT 0,
                      `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
                      `UpdatedAt` datetime(6) NULL,
                      PRIMARY KEY (`CounselingObservationId`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                ");
                context.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS = 1;");
            }
            catch { }
        }


    // =====================================================
    // DEFAULT ADMIN
    // =====================================================

    var admin = context.Admins.FirstOrDefault(a => a.Email.ToLower() == "admin@smhms.com") ?? context.Admins.FirstOrDefault();
    if (admin == null)
    {
        admin = new Admin
        {
            FullName = "System Administrator",
            Email = "admin@smhms.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Phone = "01409015552"
        };
        context.Admins.Add(admin);
        context.SaveChanges();
    }

    // =====================================================
    // BACKFILL DEFAULT YEAR AND SEMESTER FOR EXISTING STUDENTS
    // =====================================================
    try
    {
        var registeredStudents = context.Students.ToList();
        bool updatedAny = false;

        foreach (var st in registeredStudents)
        {
            if (st.AdmissionYear == null || st.AdmissionYear == 0)
            {
                int year = 2024;
                if (!string.IsNullOrWhiteSpace(st.StudentIdNumber) && st.StudentIdNumber.Length >= 4)
                {
                    string yearPrefix = st.StudentIdNumber.Substring(0, 4);
                    if (int.TryParse(yearPrefix, out int parsedYear) && parsedYear >= 2015 && parsedYear <= 2030)
                    {
                        year = parsedYear;
                    }
                }
                st.AdmissionYear = year;
                updatedAny = true;
            }

            string activeTrimester = Student.GetCurrentActiveSemester();
            if (string.IsNullOrWhiteSpace(st.Semester) || 
                st.Semester.Trim().StartsWith("Semester", StringComparison.OrdinalIgnoreCase) || 
                st.Semester.Trim().Equals("Spring 2026", StringComparison.OrdinalIgnoreCase) ||
                !st.Semester.Contains("20"))
            {
                st.Semester = activeTrimester;
                updatedAny = true;
            }
        }

        if (updatedAny)
        {
            context.SaveChanges();
        }
    }
    catch (Exception) { }


    // =====================================================
    // DEFAULT DEPARTMENT ACCOUNTS (ONLY CREATED IF MISSING)
    // =====================================================
    var defaultDepartments = new[]
    {
        new { Name = "CSE", Email = "cse@smhms.com", Password = "CSE@123", Phone = "01409015553", Head = "Head of CSE" },
        new { Name = "EEE", Email = "eee@smhms.com", Password = "EEE@123", Phone = "01409015554", Head = "Head of EEE" },
        new { Name = "Mechanical", Email = "mechanical@smhms.com", Password = "Mechanical@123", Phone = "01409015555", Head = "Head of Mechanical" },
        new { Name = "Civil", Email = "civil@smhms.com", Password = "Civil@123", Phone = "01409015556", Head = "Head of Civil" },
        new { Name = "BBA", Email = "bba@smhms.com", Password = "BBA@123", Phone = "01409015557", Head = "Head of BBA" },
        new { Name = "BATHM", Email = "bathm@smhms.com", Password = "BATHM@123", Phone = "01409015558", Head = "Head of BATHM" }
    };

    bool deptAdded = false;
    foreach (var d in defaultDepartments)
    {
        if (!context.Departments.Any(existing => existing.DepartmentName == d.Name))
        {
            context.Departments.Add(new Department
            {
                DepartmentName = d.Name,
                Email = d.Email,
                Password = d.Password,
                Phone = d.Phone,
                HeadOfDepartment = d.Head
            });
            deptAdded = true;
        }
    }

    if (deptAdded)
    {
        context.SaveChanges();
    }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Database Seeding/Migration Warning] {ex.Message}");
    }
}


// =========================================================
// HTTP REQUEST PIPELINE
// =========================================================

if (!app.Environment.IsDevelopment())
{
    // Production error handling

    app.UseExceptionHandler(
        "/Home/Error"
    );


    // HTTP Strict Transport Security

    app.UseHsts();
}


// =========================================================
// HTTPS
// =========================================================

app.UseHttpsRedirection();


// =========================================================
// STATIC FILES
// =========================================================

app.UseStaticFiles();


// =========================================================
// ROUTING
// =========================================================

app.UseRouting();


// =========================================================
// SESSION
// =========================================================

app.UseSession();


// =========================================================
// AUTHORIZATION
// =========================================================

app.UseAuthorization();


// =========================================================
// DEFAULT MVC ROUTE
// =========================================================

app.MapControllerRoute(
    name:
        "default",

    pattern:
        "{controller=Home}/{action=Index}/{id?}"
);


// =========================================================
// RUN APPLICATION
// =========================================================

app.Run();