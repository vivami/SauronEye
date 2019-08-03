using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EPocalipse.IFilter;
using System.Diagnostics;


namespace SauronEye {
    class Program {

        private static List<string> Directories;
        private static List<string> FileTypes;
        private static List<string> Keywords;
        private static bool SearchContents; 
        static void Main(string[] args) {
            Console.WriteLine("\n\t === SauronEye === \n");
            Directories = new List<string>();
            FileTypes = new List<string>();
            Keywords = new List<string>();
            SearchContents = false;
            parseArguments(args);
            Console.WriteLine("Directories to search: " + string.Join(", ", Directories));
            Console.WriteLine("For file types:" + string.Join(", ", FileTypes));
            Console.WriteLine("Containing:" + string.Join(", ", Keywords));
            Console.WriteLine("Search contents: " + SearchContents.ToString());
            Stopwatch sw = new Stopwatch();

            sw.Start();

            FSSearcher s = new FSSearcher(Directories, FileTypes, Keywords, SearchContents);
            s.Search();

            sw.Stop();

            Console.WriteLine("Elapsed={0}", sw.Elapsed);

           

            Console.WriteLine("Done");
            //Console.ReadKey();
        }


        // Implemented my own args parser. This is probably a very bad idea...
        // Transforms: 
        // -Dirs C:\,D:\,\\NLDOMFS\testing\ -Filetype .txt, .ini,.conf , .bat
        // To:
        // List<string>{ "C:\", "D:\", "\\NLDOMFS\testing\" } and List<string>{ ".txt", ".ini", ".conf", ".bat" }
        private static void parseArguments(string[] args) {
            for (var arg = 0 ; arg < args.Length;) {
                if (args[arg].StartsWith("-")) {
                    switch (args[arg].ToLower()) {
                        case "-dirs": {
                                if (args[arg+1].ToLower().Equals("-dirs")) { arg++; break; }
                                while (arg+1 != args.Length && !args[arg+1].StartsWith("-")) {
                                    Directories.AddRange(strimAndSplit(args[arg + 1].ToLower()));
                                    arg++;
                                }
                            } break;
                        case "-filetypes": {
                                if (args[arg+1].ToLower().Equals("-filetype")) { arg++; break; }
                                while (arg+1 != args.Length && !args[arg+1].StartsWith("-")) { 
                                    FileTypes.AddRange(strimAndSplit(args[arg + 1].ToLower()));
                                    arg++;
                                }
                            } break;
                        case "-keywords": {
                                if (args[arg + 1].ToLower().Equals("-keywords")) { arg++; break; }
                                while (arg + 1 != args.Length && !args[arg + 1].StartsWith("-")) {
                                    Keywords.AddRange(strimAndSplit(args[arg + 1].ToLower()));
                                    arg++;
                                }
                            } break;
                        case "-contents": {
                                SearchContents = true;
                                arg++;
                            } break;
                        default:
                            // unknown arg, proceed to next
                            arg++;
                            break;
                    }
                } else {
                    // increment to next arg
                    arg++;
                } 
            }
            // remove empty or duplicate args
            Directories = Directories.Where(s => !isNullOrWhiteSpace(s)).Distinct().ToList();
            FileTypes = FileTypes.Where(s => !isNullOrWhiteSpace(s)).Distinct().ToList();
            Keywords = Keywords.Where(s => !isNullOrWhiteSpace(s)).Distinct().ToList();
            return;
        }
        
        private static bool isNullOrWhiteSpace(string s) {
            return String.IsNullOrEmpty(s) || s.Trim().Length == 0;
        }

        // Remove whitespaces before and after ',' and split at ',' afterwards
        private static string[] strimAndSplit(string s) {
            return Regex.Replace(s, " *, *", ",").Split(',');
        }
    }


}
