using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SubSync
{
    class Program
    {
        static void Main(string[] args)
        {
            bool fileFlag = false;
            bool commandFlag = false;
            bool valuesFlag = false;
            bool outExists = false;
            bool outputFlag = false;

            if (args.Length == 3)
            {
                fileFlag = CheckInputFile(args[0]);
                commandFlag = CheckCommand(args[1]);
                valuesFlag = CheckValues(args[1], args[2]);
                outputFlag = true;
            }
            else if (args.Length == 4)
            {
                fileFlag = CheckInputFile(args[0]);
                commandFlag = CheckCommand(args[1]);
                valuesFlag = CheckValues(args[1], args[2], args[3]);
                outExists = CheckIfOutExists(args[2], args[3]);
                if (!outExists)
                {
                    outputFlag = true;
                }
                
            }
            else if(args.Length == 5)
            {
                outExists = CheckIfOutExists(args[3], args[4]);
                if(outExists)
                {
                    fileFlag = CheckInputFile(args[0]);
                    commandFlag = CheckCommand(args[1]);
                    valuesFlag = CheckValues(args[1], args[2]);
                } else
                {
                    fileFlag = CheckInputFile(args[0]);
                    commandFlag = CheckCommand(args[1]);
                    valuesFlag = CheckValues(args[1], args[2], args[3], args[4]);
                }
                outputFlag = true;
            }
            else if (args.Length == 6)
            {
                fileFlag = CheckInputFile(args[0]);
                commandFlag = CheckCommand(args[1]);
                outExists = CheckIfOutExists(args[4], args[5]);
                if (outExists)
                {

                    valuesFlag = CheckValues(args[1], args[2], args[3]);
                    outputFlag = true;
                }
                else
                {
                    outputFlag = false;
                }
            }
            else if(args.Length == 7)
            {
                outputFlag = CheckIfOutExists(args[5], args[6]);
                if(outputFlag)
                {
                    fileFlag = CheckInputFile(args[0]);
                    commandFlag = CheckCommand(args[1]);
                    valuesFlag = CheckValues(args[1], args[2], args[3], args[4]);
                }
            }
            else
            {
                PrintHelp();
                return;
            }

            if(!fileFlag || !commandFlag || !valuesFlag || !outputFlag)
            {
                PrintHelp();
                return;
            }


            string inputFile = args[0];

            string outputFile = args.Find("out", args[0]);
            List<Line> lines;
            try
            {
                lines = ReadFile(inputFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while reading input file: {ex.Message}");
                return;
            }

            

            string command = args[1];
            switch(command)
            {
                case "shift":
                    if(args.Length == 3 || args.Length == 5)
                    {
                        int value = int.Parse(args.Find("shift"));
                        lines.Shift(value);
                    } else if (args.Length == 4 || args.Length == 6)
                    {
                        int lineNumber = int.Parse(args.Find("shift"));
                        DateTime correctTime = args.Find("shift", 2).ToDateTime();
                        try
                        {
                            lines.Shift(lineNumber, correctTime);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                            return;
                        }
                    }
                    break;
                case "sync":
                    int startLine = int.Parse(args.Find("sync", 1));
                    int endLine = int.Parse(args.Find("sync", 2));
                    DateTime endTime = args.Find("sync", 3).ToDateTime();
                    lines.Sync(startLine, endLine, endTime);
                    break;
                default:
                    Console.WriteLine($"unrecognized command [{command}]");
                    return;

            }
            


            StringBuilder sb = new();
            int cnt = 1;
            foreach(Line line in lines)
            {
                sb.Append($"{cnt}\r\n");
                sb.Append($"{line.Timing}\r\n");
                sb.Append($"{line.Text}\r\n");
                sb.AppendLine();
                cnt += 1;
            }


            File.WriteAllText(outputFile, sb.ToString());

#if DEBUG
            //Process.Start("notepad", output);
            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
#endif
        }

        private static List<Line> ReadFile(string inputFile)
        {
            string sourceText;
            bool IsSignatureFound = false;
            bool IsSignInserted = false;

            try
            {
                sourceText = File.ReadAllText(inputFile);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to access input file [{inputFile}]: {ex.Message}");
            }

            

            List<Line> lines = new();
            var regex = new Regex(@"(?<sequence>\d+)\r\n(?<start_time>\d\d:\d\d:\d\d,\d\d\d) --\> (?<end_time>\d\d:\d\d:\d\d,\d\d\d)\r\n(?<text>[\s\S]*?\r\n\r\n)", RegexOptions.Compiled | RegexOptions.ECMAScript);
            var sign = new Regex("subsync", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var signMatch = sign.Match(sourceText);
            if(signMatch.Success) { IsSignatureFound = true; }

            var matches = regex.Matches(sourceText);
            int cnt = 0;


            foreach (Match match in matches)
            {
                string sequence = match.Groups["sequence"].Value;
                string startTime = match.Groups["start_time"].Value;
                string endTime = match.Groups["end_time"].Value;

                string text = match.Groups["text"].Value;
                lines.Add(new Line(sequence, startTime.ToDateTime(), endTime.ToDateTime(), text));
                
                cnt += 1;
                if(IsSignatureFound == false)
                {
                    if (cnt == matches.Count / 2 || cnt == matches.Count)
                    {
                        lines.Add(new Line(sequence + 111, startTime.ToDateTime().AddSeconds(2), endTime.ToDateTime().AddSeconds(4),
                            "subsynced by subsync@abunoor.com"));
                        IsSignInserted = true;
                    }
                }
            }
            if(IsSignInserted) Console.WriteLine("subsync signature inserted");
            return lines;

        }

        private static bool CheckIfOutExists(string v1, string v2)
        {
            if(v1 == "out")
            {
                if(!string.IsNullOrEmpty(v2)) return true;
                else return false;
            }
            return false;
            
        }

        private static bool CheckInputFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"input file [{path}] does not exist.");
                    return false;
                } else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error trying to check input file [{path}]: {ex.Message}");
                return false;
            }
        }

        private static bool CheckCommand(string comm)
        {
            if (comm == "shift" || comm == "sync") return true;
            Console.WriteLine($"Invalid command [{comm}]");
            return false;
        }

        private static bool CheckValues(string command, string v1, string v2 = null, string v3 = null)
        {
            bool ok1 = false;
            bool ok2 = false;
            bool ok3 = false;

            if(command == "sync")
            {
                ok1 = v1.IsPositiveInteger();
                if(!ok1)
                {
                    Console.WriteLine($"starting reference line [{v1}] must be a valid integer");
                }
                if (v2.IsEmpty()) Console.WriteLine("you must provide a reference line number for sync1");
                else
                {
                    ok2 = v2.IsPositiveInteger();
                    if (!ok2) Console.WriteLine($"line number [{v2}] must be a valid positive integer");
                }
                if (v3.IsEmpty()) Console.WriteLine("sync requires timestamp for reference line");
                else
                {
                    ok3 = v3.IsTimeStamp();
                    if(!ok3)
                    {
                        Console.WriteLine($"timestamp [{v3}] must be valid with format HH:mm:ss,fff");
                    }
                }

            } else if (command == "shift")
            {
                ok3 = v3.IsEmpty();

                if(v2.IsEmpty())
                {
                    ok1 = v1.IsInteger();
                    ok2 = true;
                    
                    if(!ok1)
                    {
                        Console.WriteLine($"shift value [{v1}] must be a valid integer");
                    }
                } else
                {
                    ok1 = v1.IsPositiveInteger();
                    ok2 = v2.IsTimeStamp();
                    if(!ok1)
                    {
                        Console.WriteLine($"line number [{v1}] must be a valid positive integer");
                    }
                    if(!ok2)
                    {
                        Console.WriteLine($"timestamp [{v2}] must be valid with format HH:mm:ss,fff");
                    }
                }
            }
            
            

            return ok1 && ok2 && ok3;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage:");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("    subsync inputFile <command> <value(s)> [out outputFile]");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Examples:  ");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("    subsync the_village.srt shift 1300");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("       shift timing in each subtitles lines by adding 1300 milliseconds, and overwrite original file");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("    subsync the_village.srt shift 5 00:02:24,415");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("       the 5th line in the subtitles file should be on screen at the timestamp 00:02:24,415.");
            Console.WriteLine("       all other lines are shifted according to the found time span difference");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("    subsync the_village.srt shift -26000 out the_village_fixed.srt");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("       shift timing in each subtitles lines by subtracting 26000 milliseconds, and save to the_village_fixed.srt");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("    subsync the_village.srt sync 5 643 01:14:23,015");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("       the 5th line is a reference time and is correct both in file and in movie");
            Console.WriteLine("       the 643rd line is a out of sync and should be at the timestamp 01:14:23,015");
            Console.WriteLine("       all other lines will get decrement/increment ratio based on the reference times");
            Console.WriteLine();
        }
    }


}
