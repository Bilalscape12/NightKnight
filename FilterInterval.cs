using System;
using System.Text.Json.Serialization;

namespace NightKnight
{
    public class FilterInterval
    {
        [JsonPropertyName("startTime")]
        public TimeSpan StartTime { get; set; }
        
        [JsonPropertyName("endTime")]
        public TimeSpan EndTime { get; set; }
        
        [JsonPropertyName("greenReduction")]
        public double GreenReduction { get; set; }
        
        [JsonPropertyName("blueReduction")]
        public double BlueReduction { get; set; }
        
        [JsonPropertyName("gradualStart")]
        public bool GradualStart { get; set; }
        
        [JsonPropertyName("gradualEnd")]
        public bool GradualEnd { get; set; }
        
        [JsonPropertyName("startTransitionDuration")]
        public TimeSpan StartTransitionDuration { get; set; } = TimeSpan.FromMinutes(10);
        
        [JsonPropertyName("endTransitionDuration")]
        public TimeSpan EndTransitionDuration { get; set; } = TimeSpan.FromMinutes(5);
        
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;
    }
} 