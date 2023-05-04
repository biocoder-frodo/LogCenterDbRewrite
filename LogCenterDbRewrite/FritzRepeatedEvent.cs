using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Sqlite.Synology.LogCenter
{
    class FritzRepeatedEvent
    {
        internal long ts;
        internal string message;
        internal int count;
        internal string program;
        internal long id;
        internal long? firstId;

        public FritzRepeatedEvent(Match match, FritzEvent fritzEvent, List<FritzEvent> fritzEvents)
        {
            id = fritzEvent.id;
            program = fritzEvent.program;
            var timestamp = $"{match.Groups["d1"].Value}-{match.Groups["d2"].Value}-20{match.Groups["d3"].Value} {match.Groups["ts"].Value}";
            ts = DateTime.ParseExact(timestamp, "dd-MM-yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.AssumeLocal).ToUnixInteger();
            message = match.Groups["message"].Value;
            count = int.Parse(match.Groups["count"].Value);
            
            var srch = fritzEvents.Where(e => ((e.utcsec - ts) < 2 && (e.utcsec - ts) > -2) && e.message == message);
            if (srch.Any()) firstId = srch.Single().id;
        }

        public string Key => $"{ts}_{message}";
    }
}
