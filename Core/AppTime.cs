using System;

namespace Core
{
    /// <summary>
    /// Provides global access to Kyiv time for the entire application.
    /// </summary>
    public static class AppTime
    {
        private static readonly TimeZoneInfo KyivZone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");

        /// <summary>
        /// Returns current time in Kyiv timezone.
        /// CRITICAL: This is the ONLY source of truth for application time.
        /// Uses UTC as base and converts to Kyiv timezone (UTC+2/UTC+3 with DST).
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, KyivZone);

        /// <summary>
        /// Returns current time in Kyiv timezone (same as Now, kept for backwards compatibility).
        /// </summary>
        public static DateTime KyivNow => Now;

        /// <summary>
        /// Returns current date in Kyiv timezone (time set to 00:00:00).
        /// </summary>
        public static DateTime KyivToday => Now.Date;
    }
}
