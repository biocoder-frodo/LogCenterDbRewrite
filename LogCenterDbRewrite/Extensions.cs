using Microsoft.Data.Sqlite;
using System;

namespace Sqlite.Synology.LogCenter
{
    internal static class Extensions
    {
        private static readonly DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static object GetValue(this SqliteDataReader reader, logs field)
        {
            return reader.GetValue((int)field);
        }

        internal static DateTime FromUnixInteger(this DateTime @his, long value)
        {
            return epochStart.AddSeconds(value);
        }
        internal static long ToUnixInteger(this DateTime @this)
        {
            return Convert.ToInt64((@this.ToUniversalTime() - epochStart).TotalSeconds);
        }

    }
}