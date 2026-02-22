using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Sqlite.Synology.LogCenter
{
    struct SysLogEvent
    {
        internal readonly long id;
        public readonly string host;
        public readonly string ip;
        public readonly string facility;
        public readonly string priority;
        public readonly string loglevel;
        public readonly string tag;
        public readonly long utcsec;
        public readonly long recorded_utcsec;
        private readonly string ldate;
        private readonly string ltime;
        public readonly string program;
        public readonly string message;
        public readonly string tzoffset;

        public readonly int DateNumber;
        public readonly DateTime LocalTime;
        public readonly DateTime RecordedTime;

        private static readonly IReadOnlyList<logs> fields = (logs[])Enum.GetValues(typeof(logs));
        internal static readonly string FieldList = fields.Aggregate("", (s, a) => $"{s}{a},", (a) => a.Substring(0, a.Length - 1));
        private static readonly Dictionary<logs, Func<SysLogEvent, string>> _writeMap = new Dictionary<logs, Func<SysLogEvent, string>>()
        {
            { logs.id      , e=> Field(e.id) },
            { logs.host    , e=> Field(e.host) },
            { logs.ip      , e=> Field(e.ip) },
            { logs.fac     , e=> Field(e.facility) },
            { logs.prio    , e=> Field(e.priority) },
            { logs.llevel  , e=> Field(e.loglevel) },
            { logs.tag     , e=> Field(e.tag) },
            { logs.utcsec  , e=> Field(e.utcsec) },
            { logs.r_utcsec, e=> Field(e.recorded_utcsec) },
            { logs.tzoffset, e=> Field(e.tzoffset) },
            { logs.ldate   , e=> Field(e.ldate) },
            { logs.ltime   , e=> Field(e.ltime) },
            { logs.prog    , e=> Field(e.program) },
            { logs.msg     , e=> Field(e.message) }
        };
        private static string Field(long value) => value.ToString();
        private static string Field(string value) => $"'{value.Replace(@"'", "''")}'";
        public SysLogEvent(SqliteDataReader reader, long multiTableOffset = 0)
        {
            id = reader.GetValue<long>(logs.id) + multiTableOffset;
            host = reader.GetText(logs.host);
            ip = reader.GetText(logs.ip);
            facility = reader.GetText(logs.fac);
            priority = reader.GetText(logs.prio);
            loglevel = reader.GetText(logs.llevel);
            tag = reader.GetText(logs.tag);
            utcsec = reader.GetValue<long>(logs.utcsec);
            recorded_utcsec = reader.GetValue<long>(logs.r_utcsec);
            //ldate = reader.GetText(logs.ldate);
            //ltime = reader.GetText(logs.ltime);
            program = reader.GetText(logs.prog);
            message = reader.GetText(logs.msg);
            tzoffset = reader.GetText(logs.tzoffset);

            RecordedTime = default(DateTime).FromUnixInteger(recorded_utcsec).ToLocalTime();
            LocalTime = default(DateTime).FromUnixInteger(utcsec).ToLocalTime();
            ldate = LocalTime.ToString("yyyy-MM-dd");
            ltime = LocalTime.ToString("HH:mm:ss");
            if (LocalTime.Year == 2070)
            {
                utcsec = recorded_utcsec;
                LocalTime = RecordedTime;
                message += "[Timestamp adjusted with time of recording of this event]";
            }
            DateNumber = LocalTime.Year * 10000 + LocalTime.Month * 100 + LocalTime.Day;

        }

        public string Key => $"{LocalTime.ToUniversalTime().Ticks}_{tzoffset}_{host}_{ip}_{facility}_{priority}_{loglevel}_{tag}_{program}_{message}";
        public string SortOrder => $"{LocalTime.ToUniversalTime().Ticks}_{id}";

        public override string ToString() => $"{LocalTime.ToUniversalTime().ToString("u")}\t{host}\t{ip}\t{facility}\t{priority}\t{loglevel}\t{tag}\t{program}\t{message}";

        public void WriteRow(SqliteConnection connection, long id)
        {
            StringBuilder sql = new StringBuilder("insert into logs (" + FieldList + ") values (");
            int count = 0;
            foreach (var field in fields)
            {
                if (count++ > 0) sql.Append(',');
                if (field == logs.id)
                    sql.Append(id);
                else
                    sql.Append(_writeMap[field](this));
            }
            sql.Append(")");
            SqliteCommand cmd = new SqliteCommand(sql.ToString(), connection);
            cmd.ExecuteNonQuery();
        }
    }
}