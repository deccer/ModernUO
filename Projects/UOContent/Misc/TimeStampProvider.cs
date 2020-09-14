using System;

namespace Server.Misc
{
    public static class TimeStampProvider
    {
        public static string GetTimeStamp() => $"{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}";
    }
}
