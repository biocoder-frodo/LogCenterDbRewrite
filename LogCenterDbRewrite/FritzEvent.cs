using Microsoft.Data.Sqlite;
using System;

namespace Sqlite.Synology.LogCenter
{
    struct FritzEvent
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
        //readonly string ldate;
        //readonly string ltime;
        public readonly string program;
        public readonly string message;
        public readonly string tzoffset;

        public readonly int DateNumber;
        public readonly DateTime LocalTime;
        public readonly DateTime RecordedTime;

        
        public FritzEvent(SqliteDataReader reader, long multiTableOffset=0)
        {
            id = (long)reader.GetValue(logs.id) + multiTableOffset;
            host = (string)reader.GetValue(logs.host);
            ip = (string)reader.GetValue(logs.ip);
            facility = (string)reader.GetValue(logs.fac);
            priority = (string)reader.GetValue(logs.prio);
            loglevel = (string)reader.GetValue(logs.llevel);
            tag = (string)reader.GetValue(logs.tag);
            utcsec = (long)reader.GetValue(logs.utcsec);
            recorded_utcsec = (long)reader.GetValue(logs.r_utcsec);
            //ldate = (string)reader.GetValue(logs.ldate);
            //ltime = (string)reader.GetValue(logs.ltime);
            program = (string)reader.GetValue(logs.prog);
            message = (string)reader.GetValue(logs.msg);
            tzoffset = (string)reader.GetValue(logs.tzoffset);

            RecordedTime = default(DateTime).FromUnixInteger(recorded_utcsec).ToLocalTime();
            LocalTime = default(DateTime).FromUnixInteger(utcsec).ToLocalTime();
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


        public void WriteRow(SqliteConnection connection, long id)
        {
            DateTime ts = default;

            ts = ts.FromUnixInteger(utcsec).ToLocalTime();
            string ldate = ts.ToString("yyyy-MM-dd");
            string ltime = ts.ToString("HH:mm:ss");
            string sql = "insert into logs (id,host,ip,fac,prio,llevel,tag,utcsec,r_utcsec,tzoffset,ldate,ltime,prog,msg) values "
                + $"({id},'{host}','{ip}','{facility}','{priority}','{loglevel}','{tag}',{utcsec},{recorded_utcsec},'{tzoffset}','{ldate}','{ltime}','{program}','{message}')";
            SqliteCommand cmd = new SqliteCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }
    }
}