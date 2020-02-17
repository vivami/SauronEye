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

        // Implemented my own args parser. This is probably a very bad idea...
        // Transforms: 
        // -Dirs C:\,D:\,\\NLDOMFS\testing\ -Filetype .txt, .ini,.conf , .bat
        // To:
        // List<string>{ "C:\", "D:\", "\\NLDOMFS\testing\" } and List<string>{ ".txt", ".ini", ".conf", ".bat" }
        public void ParseArguments(string[] args) {
            for (var arg = 0; arg < args.Length;) {
                if (args[arg].StartsWith("-")) {
                    switch (args[arg].ToLower()) {
                        case "-dirs": {
                                if (args[arg + 1].ToLower().Equals("-dirs")) { arg++; break; }
                                while (arg + 1 != args.Length && !args[arg + 1].StartsWith("-")) {
                                    Directories.AddRange(strimAndSplit(args[arg + 1].ToLower()));
                                    arg++;
                                }
                            }
                            break;
                        case "-filetypes": {
                                if (args[arg + 1].ToLower().Equals("-filetype")) { arg++; break; }
                                while (arg + 1 != args.Length && !args[arg + 1].StartsWith("-")) {
                                    FileTypes.AddRange(strimAndSplit(args[arg + 1].ToLower()));
                                    arg++;
                                }
                            }
                            break;
                        case "-keywords": {
                                if (args[arg + 1].ToLower().Equals("-keywords")) { arg++; break; }
                                while (arg + 1 != args.Length && !args[arg + 1].StartsWith("-")) {
                                    Keywords.AddRange(strimAndSplit(args[arg + 1].ToLower()));
                                    arg++;
                                }
                            }
                            break;
                        case "-contents": {
                                SearchContents = true;
                                arg++;
                            }
                            break;
                        case "-systemdirs": {
                                SystemDirs = true;
                                arg++;
                            }
                            break;
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
            regexSearcher = new RegexSearch(Keywords);
            //If any args are still empty, use default
            //setDefaultArgs();
            return;
        }
        public void setDefaultArgs() {
            if (Directories.Count == 0) {
                foreach (DriveInfo d in DriveInfo.GetDrives()) {
                    Directories.Add(d.Name);
                }
            }

            if (Keywords.Count == 0) {
                foreach (string s in DefaultKeywords) {
                    Keywords.Add(s);
                }
            }

            if (FileTypes.Count == 0) {
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
    }
}
