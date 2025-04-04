﻿namespace Genshin_Checker.Core.General.Convert
{
    public static class Time
    {
        public static DateTime GetUTCFromUnixTime(long unixTime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTime);
        }
        public static double GetUnixTimeFromDateTime(DateTime datetime)
        {
            return (datetime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }
        public static double? GetUnixTimeFromDateTime(DateTime? datetime)
        {
            if (datetime == null) return null;
            return GetUnixTimeFromDateTime((DateTime)datetime);
        }
    }
}
