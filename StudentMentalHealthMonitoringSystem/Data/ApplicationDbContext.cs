using Microsoft.EntityFrameworkCore;
using StudentMentalHealthMonitoringSystem.Models;

namespace StudentMentalHealthMonitoringSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        // =========================================================
        // DATABASE CONTEXT CONSTRUCTOR
        // =========================================================

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        // =========================================================
        // DATABASE TABLES
        // =========================================================


        // ================= Student Table =================

        public DbSet<Student> Students { get; set; }


        // ================= Psychologist Table =================

        public DbSet<Psychologist> Psychologists { get; set; }


        // ================= Admin Table =================

        public DbSet<Admin> Admins { get; set; }


        // ================= Department Table =================

        public DbSet<Department> Departments { get; set; }


        // ================= PHQ-9 Table =================

        public DbSet<PHQAssessment> PHQAssessments { get; set; }


        // ================= C-SSRS Table =================

        public DbSet<CSSRSAssessment> CSSRSAssessments { get; set; }


        // ================= Semester Record Table =================

        public DbSet<StudentSemesterRecord> StudentSemesterRecords { get; set; }


        // ================= Counseling Table =================

        public DbSet<Counseling> Counselings { get; set; }


        // ================= Screening Report Table =================

        public DbSet<ScreeningReport> ScreeningReports { get; set; }


        // ================= Counseling Observation Table =================

        public DbSet<CounselingObservation> CounselingObservations { get; set; }


        // ================= Observation Report Table =================

        public DbSet<ObservationReport> ObservationReports { get; set; }


        // ================= Student Availability Table =================

        public DbSet<StudentAvailability> StudentAvailabilities { get; set; }


        // ================= Psychologist Availability Table =================

        public DbSet<PsychologistAvailability> PsychologistAvailabilities { get; set; }


        // =========================================================
        // AI CHAT TABLES
        // =========================================================


        // ================= Chat Session Table =================

        public DbSet<ChatSession> ChatSessions { get; set; }


        // ================= Chat Message Table =================

        public DbSet<ChatMessage> ChatMessages { get; set; }


        // ================= Chat Risk Assessment Table =================

        public DbSet<ChatRiskAssessment> ChatRiskAssessments { get; set; }



        // =========================================================
        // LIVE VOICE BOT TABLES
        // =========================================================


        // ================= Voice Bot Session Table =================

        public DbSet<VoiceBotSession> VoiceBotSessions { get; set; }


        // ================= Voice Bot Transcript Table =================

        public DbSet<VoiceBotTranscript> VoiceBotTranscripts { get; set; }


        // ================= Voice Bot Report Table =================

        public DbSet<VoiceBotReport> VoiceBotReports { get; set; }


        // ================= Voice Bot Risk Assessment Table =================

        public DbSet<VoiceBotRiskAssessment> VoiceBotRiskAssessments { get; set; }



        // =========================================================
        // DATABASE MODEL CONFIGURATION
        // =========================================================

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // =====================================================
            // PHQ-9 CONFIGURATION
            // =====================================================

            // Semester is required

            modelBuilder.Entity<PHQAssessment>()
                .Property(p => p.Semester)
                .HasMaxLength(50)
                .IsRequired();


            // One Student can submit PHQ-9 only once
            // in the same semester

            modelBuilder.Entity<PHQAssessment>()
                .HasIndex(p => new
                {
                    p.StudentId,
                    p.Semester
                })
                .IsUnique();


            // Student → PHQ Assessment relationship

            modelBuilder.Entity<PHQAssessment>()
                .HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Cascade);



            // =====================================================
            // C-SSRS CONFIGURATION
            // =====================================================

            // Semester is required

            modelBuilder.Entity<CSSRSAssessment>()
                .Property(c => c.Semester)
                .HasMaxLength(50)
                .IsRequired();


            // One Student can submit C-SSRS only once
            // in the same semester

            modelBuilder.Entity<CSSRSAssessment>()
                .HasIndex(c => new
                {
                    c.StudentId,
                    c.Semester
                })
                .IsUnique();


            // Student → C-SSRS relationship

            modelBuilder.Entity<CSSRSAssessment>()
                .HasOne(c => c.Student)
                .WithMany()
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Cascade);



            // =====================================================
            // STUDENT SEMESTER RECORD CONFIGURATION
            // =====================================================

            // Semester is required

            modelBuilder.Entity<StudentSemesterRecord>()
                .Property(r => r.Semester)
                .HasMaxLength(50)
                .IsRequired();


            // One Student can have only one
            // semester record for the same semester

            modelBuilder.Entity<StudentSemesterRecord>()
                .HasIndex(r => new
                {
                    r.StudentId,
                    r.Semester
                })
                .IsUnique();


            // Student → Semester Record relationship

            modelBuilder.Entity<StudentSemesterRecord>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Cascade);



            // =====================================================
            // SCREENING REPORT CONFIGURATION
            // =====================================================

            // One Counseling Appointment can have
            // only one combined Screening Report

            modelBuilder.Entity<ScreeningReport>()
                .HasIndex(r => r.CounselingId)
                .IsUnique();


            // Student → Screening Report

            modelBuilder.Entity<ScreeningReport>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);


            // Psychologist → Screening Report

            modelBuilder.Entity<ScreeningReport>()
                .HasOne(r => r.Psychologist)
                .WithMany()
                .HasForeignKey(r => r.PsychologistId)
                .OnDelete(DeleteBehavior.Restrict);


            // Counseling → Screening Report

            modelBuilder.Entity<ScreeningReport>()
                .HasOne(r => r.Counseling)
                .WithMany()
                .HasForeignKey(r => r.CounselingId)
                .OnDelete(DeleteBehavior.Restrict);


            // Trigger Source

            modelBuilder.Entity<ScreeningReport>()
                .Property(r => r.TriggerSource)
                .HasMaxLength(100)
                .IsRequired();


            // Trigger Severity

            modelBuilder.Entity<ScreeningReport>()
                .Property(r => r.TriggerSeverity)
                .HasMaxLength(50)
                .IsRequired();


            // Combined Report Content

            modelBuilder.Entity<ScreeningReport>()
                .Property(r => r.ReportContent)
                .HasColumnType("text")
                .IsRequired();


            // Report Date

            modelBuilder.Entity<ScreeningReport>()
                .Property(r => r.CreatedAt)
                .IsRequired();



            // =====================================================
            // COUNSELING OBSERVATION CONFIGURATION
            // =====================================================

            // One Counseling session can have only one Observation

            modelBuilder.Entity<CounselingObservation>()
                .HasIndex(o => o.CounselingId)
                .IsUnique();


            // Root Counseling lookup index

            modelBuilder.Entity<CounselingObservation>()
                .HasIndex(o => o.RootCounselingId);


            // Counseling → Observation

            modelBuilder.Entity<CounselingObservation>()
                .HasOne(o => o.Counseling)
                .WithMany()
                .HasForeignKey(o => o.CounselingId)
                .OnDelete(DeleteBehavior.Cascade);


            // Student → Observation

            modelBuilder.Entity<CounselingObservation>()
                .HasOne(o => o.Student)
                .WithMany()
                .HasForeignKey(o => o.StudentId)
                .OnDelete(DeleteBehavior.Restrict);


            // Psychologist → Observation

            modelBuilder.Entity<CounselingObservation>()
                .HasOne(o => o.Psychologist)
                .WithMany()
                .HasForeignKey(o => o.PsychologistId)
                .OnDelete(DeleteBehavior.Restrict);


            // Overall Progress Status

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.OverallProgressStatus)
                .HasMaxLength(50)
                .IsRequired();


            // Current Mental Health Status

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.CurrentMentalHealthStatus)
                .HasMaxLength(50)
                .IsRequired();


            // PHQ Official Interpretation

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.PHQOfficialInterpretation)
                .HasMaxLength(100);


            // PHQ Project Status

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.PHQProjectStatus)
                .HasMaxLength(50);


            // C-SSRS Risk Level

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.CSSRSRiskLevel)
                .HasMaxLength(50);


            // C-SSRS Project Status

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.CSSRSProjectStatus)
                .HasMaxLength(50);


            // Academic Functioning

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.AcademicFunctioning)
                .HasMaxLength(50)
                .IsRequired();


            // Sleep Condition

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.SleepCondition)
                .HasMaxLength(50)
                .IsRequired();


            // Social Interaction

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.SocialInteraction)
                .HasMaxLength(50)
                .IsRequired();


            // Daily Activities

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.DailyActivities)
                .HasMaxLength(50)
                .IsRequired();


            // Emotional Regulation

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.EmotionalRegulation)
                .HasMaxLength(50)
                .IsRequired();


            // Current Safety Risk

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.CurrentSafetyRisk)
                .HasMaxLength(50)
                .IsRequired();


            // Assessment Basis

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.AssessmentBasis)
                .HasColumnType("text")
                .IsRequired();


            // Clinical Observation

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.ClinicalObservation)
                .HasColumnType("text")
                .IsRequired();


            // Student-Reported Improvement

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.StudentReportedImprovement)
                .HasMaxLength(50)
                .IsRequired();


            // Assessment Summary

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.AssessmentSummary)
                .HasColumnType("text")
                .IsRequired();


            // Recommended Action

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.RecommendedAction)
                .HasColumnType("text")
                .IsRequired();


            // Created Date

            modelBuilder.Entity<CounselingObservation>()
                .Property(o => o.CreatedAt)
                .IsRequired();



            // =====================================================
            // OBSERVATION REPORT CONFIGURATION
            // =====================================================

            // One counseling chain can have
            // only one Observation Report

            modelBuilder.Entity<ObservationReport>()
                .HasIndex(r => r.RootCounselingId)
                .IsUnique();


            // Root Counseling → Observation Report

            modelBuilder.Entity<ObservationReport>()
                .HasOne(r => r.RootCounseling)
                .WithMany()
                .HasForeignKey(r => r.RootCounselingId)
                .OnDelete(DeleteBehavior.Restrict);


            // Student → Observation Report

            modelBuilder.Entity<ObservationReport>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);


            // Psychologist → Observation Report

            modelBuilder.Entity<ObservationReport>()
                .HasOne(r => r.Psychologist)
                .WithMany()
                .HasForeignKey(r => r.PsychologistId)
                .OnDelete(DeleteBehavior.Restrict);


            // Initial Status

            modelBuilder.Entity<ObservationReport>()
                .Property(r => r.InitialStatus)
                .HasMaxLength(50)
                .IsRequired();


            // Current Status

            modelBuilder.Entity<ObservationReport>()
                .Property(r => r.CurrentStatus)
                .HasMaxLength(50)
                .IsRequired();


            // Overall Progress Status

            modelBuilder.Entity<ObservationReport>()
                .Property(r => r.OverallProgressStatus)
                .HasMaxLength(50)
                .IsRequired();


            // Current Safety Risk

            modelBuilder.Entity<ObservationReport>()
                .Property(r => r.CurrentSafetyRisk)
                .HasMaxLength(50)
                .IsRequired();


            // Latest Assessment Basis

            modelBuilder.Entity<ObservationReport>()
                .Property(r => r.LatestAssessmentBasis)
                .HasColumnType("text")
                .IsRequired();


            // Latest Recommended Action

            modelBuilder.Entity<ObservationReport>()
                .Property(r => r.LatestRecommendedAction)
                .HasColumnType("text")
                .IsRequired();


            // Latest Condition Summary

            modelBuilder.Entity<ObservationReport>()
                .Property(r => r.LatestConditionSummary)
                .HasColumnType("text")
                .IsRequired();


            // Final Status

            modelBuilder.Entity<ObservationReport>()
                .Property(r => r.IsFinal)
                .HasDefaultValue(false);


            // Created Date

            modelBuilder.Entity<ObservationReport>()
                .Property(r => r.CreatedAt)
                .IsRequired();


            // Updated Date

            modelBuilder.Entity<ObservationReport>()
                .Property(r => r.UpdatedAt)
                .IsRequired();



            // =====================================================
            // STUDENT AVAILABILITY CONFIGURATION
            // =====================================================

            // Student → StudentAvailability

            modelBuilder.Entity<StudentAvailability>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Cascade);



            // =====================================================
            // PSYCHOLOGIST AVAILABILITY CONFIGURATION
            // =====================================================

            // Psychologist → PsychologistAvailability

            modelBuilder.Entity<PsychologistAvailability>()
                .HasOne(a => a.Psychologist)
                .WithMany()
                .HasForeignKey(a => a.PsychologistId)
                .OnDelete(DeleteBehavior.Cascade);



            // =====================================================
            // DEPARTMENT CONFIGURATION
            // =====================================================

            // Department Name

            modelBuilder.Entity<Department>()
                .Property(d => d.DepartmentName)
                .HasMaxLength(100)
                .IsRequired();


            // Department Email

            modelBuilder.Entity<Department>()
                .Property(d => d.Email)
                .HasMaxLength(150)
                .IsRequired();


            // Department Password

            modelBuilder.Entity<Department>()
                .Property(d => d.Password)
                .HasMaxLength(100)
                .IsRequired();


            // Department Phone

            modelBuilder.Entity<Department>()
                .Property(d => d.Phone)
                .HasMaxLength(30)
                .IsRequired();


            // Head of Department

            modelBuilder.Entity<Department>()
                .Property(d => d.HeadOfDepartment)
                .HasMaxLength(150);



            // =====================================================
            // AI CHAT SESSION CONFIGURATION
            // =====================================================

            // ================= Student → Chat Sessions =================

            modelBuilder.Entity<ChatSession>()
                .HasOne(s => s.Student)
                .WithMany(s => s.ChatSessions)
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Cascade);


            // ================= Session Summary =================

            modelBuilder.Entity<ChatSession>()
                .Property(s => s.Summary)
                .HasColumnType("text");


            // ================= Session Start =================

            modelBuilder.Entity<ChatSession>()
                .Property(s => s.StartedAt)
                .IsRequired();


            // ================= Active Status =================

            modelBuilder.Entity<ChatSession>()
                .Property(s => s.IsActive)
                .HasDefaultValue(true);


            // ================= Student Session Index =================

            modelBuilder.Entity<ChatSession>()
                .HasIndex(s => new
                {
                    s.StudentId,
                    s.IsActive
                });



            // =====================================================
            // AI CHAT MESSAGE CONFIGURATION
            // =====================================================

            // ================= Chat Session → Messages =================

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.ChatSession)
                .WithMany(s => s.ChatMessages)
                .HasForeignKey(m => m.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);


            // ================= Sender =================

            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.Sender)
                .HasMaxLength(20)
                .IsRequired();


            // ================= Message =================

            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.MessageText)
                .HasColumnType("text")
                .IsRequired();


            // ================= Message Time =================

            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.CreatedAt)
                .IsRequired();


            // ================= Message Lookup Index =================

            modelBuilder.Entity<ChatMessage>()
                .HasIndex(m => new
                {
                    m.ChatSessionId,
                    m.CreatedAt
                });



            // =====================================================
            // CHAT RISK ASSESSMENT CONFIGURATION
            // =====================================================

            // ================= Session → Risk Assessments =================

            modelBuilder.Entity<ChatRiskAssessment>()
                .HasOne(r => r.ChatSession)
                .WithMany(s => s.RiskAssessments)
                .HasForeignKey(r => r.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);


            // ================= Student → Risk Assessments =================
            //
            // Restrict is used here because the assessment is already
            // connected to ChatSession, and ChatSession belongs to Student.
            // This avoids unnecessary multiple cascade paths.
            //

            modelBuilder.Entity<ChatRiskAssessment>()
                .HasOne(r => r.Student)
                .WithMany(s => s.ChatRiskAssessments)
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);


            // ================= Risk Status =================

            modelBuilder.Entity<ChatRiskAssessment>()
                .Property(r => r.RiskStatus)
                .HasMaxLength(50)
                .IsRequired();


            // ================= Assessment Summary =================

            modelBuilder.Entity<ChatRiskAssessment>()
                .Property(r => r.Summary)
                .HasColumnType("text");


            // ================= Assessment Date =================

            modelBuilder.Entity<ChatRiskAssessment>()
                .Property(r => r.CreatedAt)
                .IsRequired();


            // ================= Student Risk Lookup Index =================

            modelBuilder.Entity<ChatRiskAssessment>()
                .HasIndex(r => new
                {
                    r.StudentId,
                    r.CreatedAt
                });



            // =====================================================
            // STUDENT AI CHAT STATUS CONFIGURATION
            // =====================================================

            modelBuilder.Entity<Student>()
                .Property(s => s.LatestChatRiskStatus)
                .HasMaxLength(50);



            // =====================================================
            // LIVE VOICE BOT SESSION CONFIGURATION
            // =====================================================

            // ================= Student → Voice Bot Sessions =================

            modelBuilder.Entity<VoiceBotSession>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Cascade);


            // ================= Live Model Name =================

            modelBuilder.Entity<VoiceBotSession>()
                .Property(s => s.ModelName)
                .HasMaxLength(100)
                .IsRequired();


            // ================= Current Live Status =================

            modelBuilder.Entity<VoiceBotSession>()
                .Property(s => s.CurrentStatus)
                .HasMaxLength(50)
                .IsRequired();


            // ================= Current Live Summary =================

            modelBuilder.Entity<VoiceBotSession>()
                .Property(s => s.CurrentSummary)
                .HasColumnType("longtext");


            // ================= Session Start =================

            modelBuilder.Entity<VoiceBotSession>()
                .Property(s => s.StartedAt)
                .IsRequired();


            // ================= Active Session Status =================

            modelBuilder.Entity<VoiceBotSession>()
                .Property(s => s.IsActive)
                .HasDefaultValue(true);


            // ================= Student Active Session Lookup =================

            modelBuilder.Entity<VoiceBotSession>()
                .HasIndex(s => new
                {
                    s.StudentId,
                    s.IsActive
                });



            // =====================================================
            // LIVE VOICE BOT TRANSCRIPT CONFIGURATION
            // =====================================================

            // ================= Voice Session → Transcripts =================

            modelBuilder.Entity<VoiceBotTranscript>()
                .HasOne(t => t.VoiceBotSession)
                .WithMany()
                .HasForeignKey(t => t.VoiceBotSessionId)
                .OnDelete(DeleteBehavior.Cascade);


            // ================= Transcript Speaker =================

            modelBuilder.Entity<VoiceBotTranscript>()
                .Property(t => t.Speaker)
                .HasMaxLength(30)
                .IsRequired();


            // ================= Transcript Text =================

            modelBuilder.Entity<VoiceBotTranscript>()
                .Property(t => t.TranscriptText)
                .HasColumnType("longtext")
                .IsRequired();


            // ================= Transcript Time =================

            modelBuilder.Entity<VoiceBotTranscript>()
                .Property(t => t.CreatedAt)
                .IsRequired();


            // ================= Transcript Lookup Index =================

            modelBuilder.Entity<VoiceBotTranscript>()
                .HasIndex(t => new
                {
                    t.VoiceBotSessionId,
                    t.CreatedAt
                });



            // =====================================================
            // LIVE VOICE BOT REPORT CONFIGURATION
            // =====================================================

            // One Voice Bot Session can have
            // only one Voice Bot Report

            modelBuilder.Entity<VoiceBotReport>()
                .HasIndex(r => r.VoiceBotSessionId)
                .IsUnique();


            // ================= Voice Session → Report =================

            modelBuilder.Entity<VoiceBotReport>()
                .HasOne(r => r.VoiceBotSession)
                .WithMany()
                .HasForeignKey(r => r.VoiceBotSessionId)
                .OnDelete(DeleteBehavior.Cascade);


            // ================= Student → Voice Bot Report =================
            //
            // Restrict is used because the report is already
            // connected to VoiceBotSession, and VoiceBotSession
            // belongs to the Student.
            //

            modelBuilder.Entity<VoiceBotReport>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);


            // ================= Current Status =================

            modelBuilder.Entity<VoiceBotReport>()
                .Property(r => r.CurrentStatus)
                .HasMaxLength(50)
                .IsRequired();


            // ================= Current Summary =================

            modelBuilder.Entity<VoiceBotReport>()
                .Property(r => r.CurrentSummary)
                .HasColumnType("longtext");


            // ================= Last Updated =================

            modelBuilder.Entity<VoiceBotReport>()
                .Property(r => r.LastUpdatedAt)
                .IsRequired();


            // ================= Final Status =================

            modelBuilder.Entity<VoiceBotReport>()
                .Property(r => r.FinalStatus)
                .HasMaxLength(50);


            // ================= Final Summary =================

            modelBuilder.Entity<VoiceBotReport>()
                .Property(r => r.FinalSummary)
                .HasColumnType("longtext");


            // ================= Final Report Status =================

            modelBuilder.Entity<VoiceBotReport>()
                .Property(r => r.IsFinal)
                .HasDefaultValue(false);


            // ================= Student Report Lookup =================

            modelBuilder.Entity<VoiceBotReport>()
                .HasIndex(r => new
                {
                    r.StudentId,
                    r.LastUpdatedAt
                });



            // =====================================================
            // LIVE VOICE BOT RISK ASSESSMENT CONFIGURATION
            // =====================================================

            // ================= Voice Session → Risk Assessments =================

            modelBuilder.Entity<VoiceBotRiskAssessment>()
                .HasOne(r => r.VoiceBotSession)
                .WithMany()
                .HasForeignKey(r => r.VoiceBotSessionId)
                .OnDelete(DeleteBehavior.Cascade);


            // ================= Student → Risk Assessments =================
            //
            // Restrict avoids unnecessary multiple cascade paths.
            // VoiceBotSession already belongs to the Student.
            //

            modelBuilder.Entity<VoiceBotRiskAssessment>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);


            // ================= Risk Status =================

            modelBuilder.Entity<VoiceBotRiskAssessment>()
                .Property(r => r.RiskStatus)
                .HasMaxLength(50)
                .IsRequired();


            // ================= Risk Summary =================

            modelBuilder.Entity<VoiceBotRiskAssessment>()
                .Property(r => r.Summary)
                .HasColumnType("longtext");


            // ================= Risk Assessment Time =================

            modelBuilder.Entity<VoiceBotRiskAssessment>()
                .Property(r => r.CreatedAt)
                .IsRequired();


            // ================= Session Risk History Index =================

            modelBuilder.Entity<VoiceBotRiskAssessment>()
                .HasIndex(r => new
                {
                    r.VoiceBotSessionId,
                    r.CreatedAt
                });


            // ================= Student Risk Lookup Index =================

            modelBuilder.Entity<VoiceBotRiskAssessment>()
                .HasIndex(r => new
                {
                    r.StudentId,
                    r.CreatedAt
                });
        }
    }
}