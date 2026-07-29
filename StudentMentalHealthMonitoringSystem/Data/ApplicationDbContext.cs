using Microsoft.EntityFrameworkCore;
using StudentMentalHealthMonitoringSystem.Models;

namespace StudentMentalHealthMonitoringSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ================= Student Table =================

        public DbSet<Student> Students { get; set; }

        // ================= PHQ-9 Table =================

        public DbSet<PHQAssessment> PHQAssessments { get; set; }

        // ================= C-SSRS Table =================

        public DbSet<CSSRSAssessment> CSSRSAssessments { get; set; }

        // ================= Semester Record Table =================

        public DbSet<StudentSemesterRecord> StudentSemesterRecords { get; set; }

        // ================= Database Configuration =================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ================= PHQ-9 Configuration =================

            // Semester is required
            modelBuilder.Entity<PHQAssessment>()
                .Property(p => p.Semester)
                .HasMaxLength(50)
                .IsRequired();

            // একই Student একই Semester-এ একবারই
            // PHQ-9 submit করতে পারবে
            modelBuilder.Entity<PHQAssessment>()
                .HasIndex(p => new
                {
                    p.StudentId,
                    p.Semester
                })
                .IsUnique();

            // Student এবং PHQAssessment relationship
            modelBuilder.Entity<PHQAssessment>()
                .HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Cascade);


            // ================= C-SSRS Configuration =================

            // Semester is required
            modelBuilder.Entity<CSSRSAssessment>()
                .Property(c => c.Semester)
                .HasMaxLength(50)
                .IsRequired();

            // একই Student একই Semester-এ একবারই
            // C-SSRS submit করতে পারবে
            modelBuilder.Entity<CSSRSAssessment>()
                .HasIndex(c => new
                {
                    c.StudentId,
                    c.Semester
                })
                .IsUnique();

            // Student এবং CSSRSAssessment relationship
            modelBuilder.Entity<CSSRSAssessment>()
                .HasOne(c => c.Student)
                .WithMany()
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Cascade);


            // ================= Semester Record Configuration =================

            // Semester is required
            modelBuilder.Entity<StudentSemesterRecord>()
                .Property(r => r.Semester)
                .HasMaxLength(50)
                .IsRequired();

            // একই Student-এর একই Semester-এ
            // একটি record থাকবে
            modelBuilder.Entity<StudentSemesterRecord>()
                .HasIndex(r => new
                {
                    r.StudentId,
                    r.Semester
                })
                .IsUnique();

            // Student এবং StudentSemesterRecord relationship
            modelBuilder.Entity<StudentSemesterRecord>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}