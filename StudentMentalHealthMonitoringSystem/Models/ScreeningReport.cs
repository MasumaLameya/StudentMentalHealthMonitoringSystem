using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentMentalHealthMonitoringSystem.Models
{
    public class ScreeningReport
    {
        // ================= Primary Key =================

        [Key]
        public int ScreeningReportId { get; set; }


        // ================= Student =================

        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; set; }


        // ================= Assigned Psychologist =================

        [Required]
        public int PsychologistId { get; set; }

        [ForeignKey(nameof(PsychologistId))]
        public Psychologist? Psychologist { get; set; }


        // ================= Counseling Appointment =================

        [Required]
        public int CounselingId { get; set; }

        [ForeignKey(nameof(CounselingId))]
        public Counseling? Counseling { get; set; }


        // ================= Trigger Information =================

        [Required]
        [StringLength(100)]
        public string TriggerSource { get; set; } =
            string.Empty;


        [Required]
        [StringLength(50)]
        public string TriggerSeverity { get; set; } =
            string.Empty;


        // ================= Combined Screening Report =================
        //
        // Contains latest available screening information
        // from questionnaires, AI Chat, Feelings and
        // other supported screening sources.
        //

        [Required]
        [Column(TypeName = "text")]
        public string ReportContent { get; set; } =
            string.Empty;


        // ================= Report Date =================

        [Required]
        public DateTime CreatedAt { get; set; } =
            DateTime.Now;
    }
}
