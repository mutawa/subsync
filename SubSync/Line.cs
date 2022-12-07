using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubSync
{
    public class Line
    {
        
        

        public Line(string sequence, DateTime startTime, DateTime endTime, string text)
        {
            Sequence = int.Parse(sequence);
            StartTime = startTime;
            EndTime = endTime;
            Duration = (int)(EndTime - StartTime).TotalMilliseconds;
            Text = text.Trim();
        }

        public int Sequence { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Text { get; set; }
        public int Duration { get; set; }
        public string Timing => $"{StartTime:HH:mm:ss,fff} --> {EndTime:HH:mm:ss,fff}";
        public override string ToString()
        {
            return $"{StartTime:HH:mm:ss,fff} --> {EndTime:HH:mm:ss,fff}\r\n{Text}";
        }

        public void Shift(int milliseconds)
        {
            StartTime = StartTime.AddMilliseconds(milliseconds);
            EndTime = EndTime.AddMilliseconds(milliseconds);
        }

        public void Sync(DateTime start1, DateTime stop1, DateTime start2, DateTime stop2)
        {
            StartTime = Map(StartTime, start1, stop1, start2, stop2);
            EndTime = StartTime.AddMilliseconds(Duration);

        }

        
        private static readonly DateTime origin = "00:00:00,000".ToDateTime();
        public static DateTime Map(DateTime n, DateTime start1, DateTime stop1, DateTime start2, DateTime stop2)
        {
            double d = (n - start1).TotalMilliseconds;
            double total1 = (stop1 - start1).TotalMilliseconds;
            double total2 = (stop2 - start2).TotalMilliseconds;
            double s2 = (start2 - origin).TotalMilliseconds;
            double r = d / total1 * total2 + s2;

            return origin.AddMilliseconds(r);
        }
    }

    public static class Utils
    {
        public static void Shift(this IEnumerable<Line> lines, int milliseconds)
        {
            foreach(Line line in lines)
            {
                line.Shift(milliseconds);
            }
        }
        public static void Shift(this IEnumerable<Line> lines, int lineNumber, DateTime correctTime)
        {
            var line = lines.Where(l => l.Sequence == lineNumber).FirstOrDefault();
            if(line == null)
            {
                throw new Exception($"line number [{lineNumber}] not found in subtitles");
            } else
            {
                Console.WriteLine($"Timestamp of line {lineNumber} in file is {line.StartTime:HH:mm:ss,fff} ");
                int difference = (int)(correctTime - line.StartTime).TotalMilliseconds;
                if(difference == 0)
                {
                    Console.WriteLine("No shifting required");
                }
                else
                {
                    Console.WriteLine($"Shifting all lines by {difference} milliseconds");
                    lines.Shift(difference);
                }
            }
        }
        public static void Sync(this IEnumerable<Line> lines, int startLine, int endLine, DateTime correctEndTime)
        {
            DateTime startTimeInFile = lines.Where(l => l.Sequence == startLine).First().StartTime;
            DateTime endTimeInFile = lines.Where(l => l.Sequence == endLine).First().StartTime;
            var before = lines.Last().StartTime;

            foreach(Line line in lines)
            {
                line.Sync(startTimeInFile, endTimeInFile, startTimeInFile, correctEndTime);
            }

            var after = lines.Last().StartTime;

            int difference = (int)(after - before).TotalSeconds;
            if(difference == 0) Console.WriteLine("No change noticed");
            else Console.WriteLine($"subsync adjusted file to movie difference of [{difference}] seconds.");

        }

        public static string Find(this string[] args, string field, string defaultValue)
        {
            for(int i = 0; i<args.Length - 1; i++)
            {
                if(args[i] == field)
                {
                    return args[i + 1];
                }
            }
            return defaultValue;
        }
        public static string Find(this string[] args, string field, int offset = 1)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == field)
                {
                    if((i+offset)<args.Length)
                    {
                        return args[i + offset];
                    }
                    else
                    {
                        throw new Exception($"offset [{i + offset}] exceeds arguments length of ({args.Length})");
                    }
                }
            }
            throw new Exception($"field ({field}) not found");
        }

        public static DateTime ToDateTime(this string text)
        {
            string[] times = text.Split(",");
            return DateTime.Parse(times[0]).AddMilliseconds(int.Parse(times[1]));

        }

        public static bool IsDateTime(this string text)
        {
            return DateTime.TryParse(text, out _);
        }

        public static bool IsTimeStamp(this string text)
        {
            string[] parts = text.Split(",");
            if(parts.Length == 2)
            {
                return parts[0].IsDateTime() && parts[1].IsMilliseconds();
            }
            return false;
        }

        public static bool IsMilliseconds(this string text)
        {
            if(text.Length == 3)
            {
                return int.TryParse(text, out _);
            }
            return false;
        }

        public static bool IsPositiveInteger(this string text)
        {
            int val;
            if( int.TryParse(text, out val))
            {
                return val > 0;
            }
            return false;
        }

        public static bool IsInteger(this string text)
        {

            return int.TryParse(text, out _);

        }

        public static bool IsEmpty(this string text)
        {
            return string.IsNullOrEmpty(text);
        }


    }
}
