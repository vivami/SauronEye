using Mono.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace SauronEye {
    public class ArgumentParser {

        public List<string> Directories, FileTypes, Keywords;
        public string[] DefaultFileTypes, DefaultKeywords;
        public bool SearchContents;
        public bool SystemDirs;
        public UInt64 MaxFileSizeInKB;
        public RegexSearch regexSearcher;
        public DateTime BeforeDate;
        public DateTime AfterDate;
        public bool CheckForMacro;

        public ArgumentParser() {
            Directories = new List<string>();
            FileTypes = new List<string>();
            Keywords = new List<string>();
            DefaultFileTypes = new string[] { ".docx", ".txt" };
            DefaultKeywords = new string[] { "pass*", "wachtw*" };
            SearchContents = false;
            MaxFileSizeInKB = 1024; // In kilobytes: 1MB
            SystemDirs = false;
            BeforeDate = DateTime.MinValue;
            AfterDate = DateTime.MinValue;
            CheckForMacro = false;
        }

        // Parses the arguments passed to SauronEye, using Mono.Options
        public void ParseArgumentsOptions(string[] args) {
            var shouldShowHelp = false;
            string currentParameter = "";
            var options = new OptionSet(){
                { "d|directories=", "Directories to search", v => {
                    Directories.Add(v);
                    currentParameter = "d";
                } },
                { "f|filetypes=","Filetypes to search for/in", v => {
                    FileTypes.Add(v);
                    currentParameter = "f";
                } },
                { "k|keywords=","Keywords to search for", v => {
                    Keywords.Add(v);
                    currentParameter = "k";
                } },
                { "c|contents","Search file contents", c =>  SearchContents = c != null },
                { "m|maxfilesize=", "Max file size to search contents in, in kilobytes", m => { CheckInteger(m); } },
                { "b|beforedate=", "Filter files last modified before this date, \n format: yyyy-MM-dd", b => { CheckDate(b, "before"); } },
                { "a|afterdate=", "Filter files last modified after this date, \n format: yyyy-MM-dd", a => { CheckDate(a, "after"); } },
                { "s|systemdirs","Search in filesystem directories %APPDATA% and %WINDOWS%", s => SystemDirs = s != null },
                { "v|vbamacrocheck","Check if 2003 Office files (*.doc and *.xls) contain a VBA macro", s => CheckForMacro = s != null },
                { "h|help","Show help", h => shouldShowHelp = h != null },
                { "<>", v => {
                    switch(currentParameter) {
                        case "d":
                            Directories.Add(v);
                            break;
                        case "f":
                            FileTypes.Add(v);
                            break;
                        case "k":
                            Keywords.Add(v);
                            break;
                        case "":
                            break;
                    }
                }}
            };

            List<string> extra;
            try {
                extra = options.Parse(args);
            } catch (OptionException e) {
                Console.Write("SauronEye.exe: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try 'SauronEye.exe --help' for more information.");
                System.Environment.Exit(1);
            }

            if (shouldShowHelp) {
                ShowHelp(options);
                System.Environment.Exit(1);
            }
            CheckArgs();
            Directories = Directories.Where(s => !isNullOrWhiteSpace(s)).Distinct().ToList();
            FileTypes = FileTypes.Where(s => !isNullOrWhiteSpace(s)).Distinct().ToList();
            Keywords = Keywords.Where(s => !isNullOrWhiteSpace(s)).Distinct().ToList();
            regexSearcher = new RegexSearch(Keywords);
            return;

        }

        // Checks the input of the args and adds defaults for empty args
        public void CheckArgs() {
            if (Directories.Count == 0) {
                Console.WriteLine("[!] No directories entered. Adding all mounted drives.");
                foreach (DriveInfo d in DriveInfo.GetDrives()) {
                    Directories.Add(d.Name);
                }
            }


            if (FileTypes.Count == 0) {
                Console.WriteLine("[!] No filetypes entered. Adding '.txt' and '.docx' as defaults.");
                foreach (string s in DefaultFileTypes) {
                    FileTypes.Add(s);
                }
            }


        }

        private void CheckInteger(string maybeInt) {
            bool isParsable = UInt64.TryParse(maybeInt, out this.MaxFileSizeInKB);

            if (!isParsable) {
                Console.WriteLine("[!] MaxFileSize is not an Integer, defaulting to 1MB.");
            }
        }

        private void CheckDate(string date, string whichdate) {
            try {
                bool result = false;
                if (whichdate.Equals("before")) {
                    result = DateTime.TryParseExact(
                        date,
                        "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out this.BeforeDate);
                } else {
                    result = DateTime.TryParseExact(
                        date,
                        "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out this.AfterDate);
                }
                if (!result) {
                    Console.WriteLine("[!] Incorrect --{0}date format. Try SauronEye.exe --help", whichdate);
                    System.Environment.Exit(1);
                }
                if (BeforeDate != DateTime.MinValue && AfterDate != DateTime.MinValue) {
                    Console.WriteLine("[!] Parameters --beforedate and --afterdate are mutually exclusive. Try SauronEye.exe --help");
                    System.Environment.Exit(1);
                }
            } catch (Exception) {
                Console.WriteLine("[!] Incorrect --{0}date format. Try SauronEye.exe --help", whichdate);
                System.Environment.Exit(1);
            }
        }

        private bool isNullOrWhiteSpace(string s) {
            return String.IsNullOrEmpty(s) || s.Trim().Length == 0;
        }

        // Remove whitespaces before and after ',' and split at ',' afterwards
        private string[] strimAndSplit(string s) {
            return Regex.Replace(s, " *, *", ",").Split(',');
        }

        private static void ShowHelp(OptionSet p) {
            Console.WriteLine("Usage: SauronEye.exe [OPTIONS]+ argument");
            Console.WriteLine("Search directories for files containing specific keywords.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);

        }
    }
}
