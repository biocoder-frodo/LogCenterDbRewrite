﻿using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;

namespace Sqlite.Synology.LogCenter
{
    class Program
    {
        private static readonly string fields = ((logs[])Enum.GetValues(typeof(logs))).Select(f => f.ToString()).Aggregate("", (s, a) => $"{a},{s}", (a) => a.Substring(0, a.Length - 1));
        private static readonly Regex regexrepeatsFilter = new Regex(@"^(?<message>.*)\s+\[(?<count>[0-9]+) messages since (?<d1>[0-9]{2}).(?<d2>[0-9]{2}).(?<d3>[0-9]{2})\s+(?<ts>[0-9]{2}:[0-9]{2}:[0-9]{2})\]$", RegexOptions.Compiled);

        static bool CheckFile(FileInfo db)
        {
            if (db.Exists)
            {
                if (IsLogCenterDb(db))
                {
                    return true;
                }
                else
                {
                    Console.WriteLine($"Your input database '{db.FullName}' is not a LogCenter database or could not be read.");
                }
            }
            else
            {
                Console.WriteLine($"Your input database '{db.FullName}' could not be found.");
            }
            return false;
        }
        static void Main(string[] args)
        {

            bool dlh = false;

            var input = new List<FileInfo>();
            var startInfo = new Queue<string>(args);

            while (startInfo.Count > 0)
            {
                if (startInfo.TryPeek(out string arg))
                {
                    switch (arg.ToLowerInvariant())
                    {
                        case "dsl-link-history":
                        case "dlh":

                            dlh = true;
                            _ = startInfo.Dequeue();
                            break;

                        case "--help":
                        case "--?":
                            Console.WriteLine($"Usage : ./LogCenterRewrite [[database] database2 ....] options");
                            Console.WriteLine("Options:");
                            Console.WriteLine("dlh, dsl-link-history\t\tExports the link speed history of your DSL connection.");
                            return;
                    }
                }
                if (startInfo.Count > 0)
                {
                    var dbArg = new FileInfo(startInfo.Dequeue());

                    if (CheckFile(dbArg) == false) return;

                    input.Add(dbArg);
                }
            }
            if (input.Count == 0) input.Add(new FileInfo("SYNOSYSLOGDB_fritz.box.DB"));

            var db = input.First();

            if (CheckFile(db) == false) return;

            string hostName;
            if (GetHostNameFromFile(db, out FileInfo exportCsv, out FileInfo parsedCsv, out hostName) == false)
            {
                Console.WriteLine($"The first input database should have your Fritz's hostname in the filename.");
                return;
            }

            var dest = new FileInfo(db.FullName + ".processed.DB");
            db.CopyTo(dest.FullName, true);


            var messages = new Dictionary<int, Dictionary<string, FritzEvent>>();

            long newid = 1;

            using (var target = OpenDatabase(dest, true)) { }

            using (var parsed = new StreamWriter(parsedCsv.FullName))
            {
                long offset = 0;
                long lastId;
                foreach (var src in input)
                {

                    using (var con = OpenDatabase(src))
                    {
                        var cmd = new SqliteCommand("select * from logs order by utcsec, r_utcsec, ID", con);
                        var reader = cmd.ExecuteReader();

                        _ = GetHostNameFromFile(src, out exportCsv, out FileInfo _, out string _);
                        using (var export = new StreamWriter(exportCsv.FullName))
                        {
                            ReadRows(reader, messages, offset, out lastId, export);
                        }

                    }

                    offset += 100 + lastId;
                }

                var cached = new List<FritzEvent>();
                var repeats = new List<FritzRepeatedEvent>();
                var lastRepeats = new Dictionary<string, FritzRepeatedEvent>();
                FritzEvent previous = default;

                foreach (var day in messages.Keys.OrderBy(d => d))
                {
                    foreach (var m in messages[day].Values.OrderBy(row => row.SortOrder))
                    {
                        // duplicates due to firmware bug, message retrieval was time dependant
                        if ((previous.program == m.program && previous.message == m.message && previous.host == m.host && previous.ip == m.ip
                            && previous.utcsec == m.utcsec - 1) == false)
                        {
                            cached.Add(m);

                            Match match = regexrepeatsFilter.Match(m.message);

                            if (match.Success)
                            {

                                var repeat = new FritzRepeatedEvent(match, m, cached);
                                repeats.Add(repeat);
                                if (lastRepeats.ContainsKey(repeat.Key) == false)
                                {
                                    lastRepeats.Add(repeat.Key, repeat);
                                }
                                else
                                {
                                    if (lastRepeats[repeat.Key].count < repeat.count) lastRepeats[repeat.Key] = repeat;
                                }
                            }
                        }
                        previous = m;
                    }
                }

                foreach (var m in repeats)
                {
                    if (m.firstId.HasValue)
                    {
                        Console.WriteLine($"message #{m.count}: firstId {m.firstId.Value}");

                        cached.Remove(cached.SingleOrDefault(r => r.id == m.firstId.Value));
                    }

                    if (lastRepeats[m.Key].count != m.count)
                    {
                        Console.WriteLine($"message #{m.count} superseded by #{lastRepeats[m.Key].count}");
                        Console.WriteLine($"message #{m.count}: id {m.id}");

                        cached.Remove(cached.Single(r => r.id == m.id));
                    }
                }

                using (var target = OpenDatabase(dest))
                {
                    foreach (var m in cached)
                    {
                        m.WriteRow(target, newid++);
                        parsed?.WriteLine($"{m.LocalTime.ToString("yyyy-MM-ddTHH:mm:ss")};{m.host};{m.ip};{m.message}");
                    }
                    VacuumDatabase(target);
                }

                if (dlh)
                {
                    var rates = new Regex(@"^.*DSL synchronization established with (?<dlrate>[0-9]+)\/(?<ulrate>[0-9]+)\s+kbit\/s.*$", RegexOptions.Compiled);
                    using (var sw = new StreamWriter($"{hostName}.dsl-link-history.csv"))
                    {
                        sw.WriteLine("timestamp\tdownload(kbit/s)\tupload(kbit/s)");

                        foreach (var m in cached.Where(m => m.message.Contains("synchronization established")))
                        {
                            var speeds = rates.Match(m.message);
                            if (speeds.Success)
                                sw.WriteLine($"{m.LocalTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")}\t{speeds.Groups["dlrate"]}\t{speeds.Groups["ulrate"]}");
                            else
                            {
                                Console.WriteLine();
                            }
                        }

                    }
                }

            }

        }
        static bool GetHostNameFromFile(FileInfo fileInfo, out FileInfo exportCsv, out FileInfo parsedCsv, out string name)
        {
            name = string.Empty;
            Regex hostName = new Regex(@"^.*SYNOSYSLOGDB_(.*)\.DB$");

            parsedCsv = new FileInfo(fileInfo.FullName + ".parsed.csv");
            exportCsv = new FileInfo(fileInfo.FullName + ".csv");

            var match = hostName.Match(fileInfo.FullName);
            if (match.Success)
            {
                name = match.Groups[1].Value;
                parsedCsv = new FileInfo(Path.Combine(fileInfo.DirectoryName, name + ".syslog.parsed.csv"));
                exportCsv = new FileInfo(Path.Combine(fileInfo.DirectoryName, name + ".syslog.csv"));
                return true;
            }
            return false;
        }
        private static void ReadRows(SqliteDataReader reader, Dictionary<int, Dictionary<string, FritzEvent>> messages, long multiTableOffset, out long lastId, StreamWriter export = null)
        {
            DateTime ts = default;
            lastId = multiTableOffset;
            if (reader.HasRows)
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    export?.Write(reader.GetName(i) + ";");
                }
                export?.WriteLine();

                var values = new object[reader.FieldCount];
                while (reader.Read())
                {
                    reader.GetValues(values);

                    if (export != null)
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            logs column = (logs)i;
                            switch (column)
                            {
                                case logs.utcsec:
                                case logs.r_utcsec:

                                    ts = ts.FromUnixInteger((long)values[i]);

                                    export.Write($"{ts.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss")};");
                                    break;
                                case logs.ltime:
                                case logs.ldate:
                                case logs.tzoffset:
                                    break;
                                default:
                                    export.Write($"{values[i]};");
                                    break;
                            }
                        }
                        export.WriteLine();
                    }


                    var m = new FritzEvent(reader, multiTableOffset);
                    if (lastId < m.id) lastId = m.id;

                    if (messages.ContainsKey(m.DateNumber) == false) messages.Add(m.DateNumber, new Dictionary<string, FritzEvent>());
                    if (messages[m.DateNumber].ContainsKey(m.Key) == false)
                    {
                        messages[m.DateNumber].Add(m.Key, m);
                    }
                }
            }
        }
        private static void DeleteRows(SqliteConnection connection)
        {
            var cmd = new SqliteCommand("delete from logs", connection);
            cmd.ExecuteNonQuery();
        }
        private static void VacuumDatabase(SqliteConnection connection)
        {
            var cmd = new SqliteCommand("vacuum", connection);
            cmd.ExecuteNonQuery();
        }

        static bool IsLogCenterDb(FileInfo fileInfo)
        {

            try
            {
                using (var con = OpenDatabase(fileInfo))
                {
                    var cmd = new SqliteCommand($"select count(*) from (select {fields} from logs limit 1)", con);
                    var reader = cmd.ExecuteReader();
                    reader.Read();
                    var rows = (long)reader.GetValue(0);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }
        static SqliteConnection OpenDatabase(FileInfo fileInfo, bool wipeLogs = false)
        {
            SqliteConnectionStringBuilder cb = new SqliteConnectionStringBuilder()
            {
                DataSource = fileInfo.FullName,
                Mode = SqliteOpenMode.ReadWrite

            };
            SqliteConnection con = new SqliteConnection(cb.ConnectionString);
            con.Open();

            if (wipeLogs)
            {
                DeleteRows(con);
                VacuumDatabase(con);
            }
            return con;
        }
        static List<object[]> GetRecordset(SqliteConnection connection, string sql)
        {
            var result = new List<object[]>();

            var cmd = new SqliteCommand(sql, connection);
            var reader = cmd.ExecuteReader();

            while (reader.Read())

            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                result.Add(values);
            }
            return result;
        }
    }
}