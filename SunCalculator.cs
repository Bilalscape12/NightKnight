using System;

namespace NightKnight
{
    public class SunCalculator
    {
        public static (TimeSpan Sunrise, TimeSpan Sunset) CalculateSunTimes(DateTime date, double latitude, double longitude)
        {
            // Convert to radians
            double latRad = latitude * Math.PI / 180.0;

            // Calculate day of year (1-366)
            int dayOfYear = date.DayOfYear;

            // Calculate solar declination (in radians)
            double declination = 0.4093 * Math.Sin(2 * Math.PI / 365 * (dayOfYear - 80));

            // Calculate hour angle for sunrise/sunset (in radians)
            // 0.83 degrees = 50 minutes of arc (standard atmospheric refraction)
            double hourAngle = Math.Acos((Math.Cos(1.5853) - Math.Sin(declination) * Math.Sin(latRad)) / 
                                        (Math.Cos(declination) * Math.Cos(latRad)));

            // Convert hour angle to hours
            double sunriseHour = 12 - (hourAngle * 180 / Math.PI) / 15;
            double sunsetHour = 12 + (hourAngle * 180 / Math.PI) / 15;

            // Convert to TimeSpan
            TimeSpan sunrise = TimeSpan.FromHours(sunriseHour);
            TimeSpan sunset = TimeSpan.FromHours(sunsetHour);

            return (sunrise, sunset);
        }

        public static TimeSpan CalculateSunsetDuration(DateTime date, double latitude, double longitude)
        {
            // Calculate how long it takes for the sun to set
            // This varies by latitude and season, but typically takes about 2-4 minutes
            // For more accuracy, we can calculate based on latitude and declination
            
            double latRad = Math.Abs(latitude * Math.PI / 180.0);
            int dayOfYear = date.DayOfYear;
            double declination = 0.4093 * Math.Sin(2 * Math.PI / 365 * (dayOfYear - 80));
            
            // Calculate the rate of change of the sun's altitude during sunset
            // This is approximately 0.25 degrees per minute at the equator
            // and varies with latitude and season
            double rateOfChange = 0.25 / Math.Cos(latRad);
            
            // The sun's disk is about 0.5 degrees, so time to set = 0.5 / rate
            double minutesToSet = 0.5 / rateOfChange;
            
            // Add some buffer for atmospheric effects
            minutesToSet += 2;
            
            // Round to the nearest minute
            int roundedMinutes = (int)Math.Round(Math.Max(2, Math.Min(8, minutesToSet)));
            
            return TimeSpan.FromMinutes(roundedMinutes);
        }

        public static TimeSpan CalculateSunriseDuration(DateTime date, double latitude, double longitude)
        {
            // Sunrise duration is typically the same as sunset duration
            return CalculateSunsetDuration(date, latitude, longitude);
        }
    }
} 