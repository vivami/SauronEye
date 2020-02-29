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
        public RegexSearch regexSearcher;

        public ArgumentParser() {
            Directories = new List<string>();
            FileTypes = new List<string>();
            Keywords = new List<string>();
            DefaultFileTypes = new string[] { ".docx", ".txt" };
            DefaultKeywords = new string[] { "pass*", "wachtw*" };
            SearchContents = true;
            SystemDirs = false;
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
                { "c|contents","Search file contents", c => SearchContents = c != null},
                { "s|systemdirs","Search in filesystem directories %APPDATA% and %WINDOWS%", s => SystemDirs = s != null},
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

            if (Keywords.Count == 0) {
                Console.WriteLine("[!] No keywords entered. Adding 'wacht' and 'pass' as defaults.");
                foreach (string s in DefaultKeywords) {
                    Keywords.Add(s);
                }
            }

            if (FileTypes.Count == 0) {
                Console.WriteLine("[!] No filetypes entered. Adding '.txt' and '.docx' as defaults.");
                foreach (string s in DefaultFileTypes) {
                    FileTypes.Add(s);
                }
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
