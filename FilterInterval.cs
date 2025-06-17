using System;

namespace NightKnight
{
    public class FilterInterval
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public double GreenReduction { get; set; }
        public double BlueReduction { get; set; }
        public bool GradualStart { get; set; }
        public bool GradualEnd { get; set; }
        public TimeSpan StartTransitionDuration { get; set; } = TimeSpan.FromMinutes(10);
        public TimeSpan EndTransitionDuration { get; set; } = TimeSpan.FromMinutes(5);
        public bool IsActive { get; set; } = true;
    }
} 