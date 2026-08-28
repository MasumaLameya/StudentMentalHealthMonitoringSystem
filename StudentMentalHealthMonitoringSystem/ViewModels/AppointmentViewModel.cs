using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StudentMentalHealthMonitoringSystem.ViewModels
{
    public class AppointmentViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Preferred Date")]
        public DateTime PreferredDate { get; set; }


        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Preferred Time")]
        public TimeSpan StartTime { get; set; }


        // Suggested available times
        public List<TimeSpan> SuggestedTimes { get; set; }
            = new List<TimeSpan>();


        public string? Message { get; set; }
    }
}