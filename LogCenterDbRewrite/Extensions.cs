using Microsoft.Data.Sqlite;
using System;

namespace Sqlite.Synology.LogCenter
{
    internal static class Extensions
    {
        private static readonly DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static S GetValue<S>(this SqliteDataReader reader, logs field) where S : struct => (S)reader.GetValue((int)field);
        public static string GetText(this SqliteDataReader reader, logs field) => (string)reader.GetValue((int)field);

        internal static DateTime FromUnixInteger(this DateTime @his, long value) => epochStart.AddSeconds(value);
        internal static long ToUnixInteger(this DateTime @this) => Convert.ToInt64((@this.ToUniversalTime() - epochStart).TotalSeconds);

    }
}