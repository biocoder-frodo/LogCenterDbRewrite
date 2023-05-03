using Microsoft.Data.Sqlite;
using System;

namespace Sqlite.Synology.LogCenter
{
    internal static class Extensions
    {
        public static object GetValue(this SqliteDataReader reader, logs field)
        {
            return reader.GetValue((int)field);
        }

        internal static DateTime FromUnixInteger(this DateTime @his, long value)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(value);
        }
    }
}