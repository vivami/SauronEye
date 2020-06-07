using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EPocalipse.IFilter;

namespace SauronEye {

    /* 
     *  FSSearcher searches the input directories for files qualifying for the input filters. 
     *  Not implemented to run in parallel, since this will likely only decrease performance (increases HDD needle changes).
     */
    class FSSearcher {

        private static readonly string[] Office2003Extentions = { ".doc", ".xls" };
        private string SearchDirectory;
        private List<string> Filetypes;
        private List<string> Keywords;
        private List<string> Results;
        private bool searchContents;
        private bool SystemDirs;
        private UInt64 maxFileSizeInKB;
        private IEnumerable<string> FilesFilteredOnExtension;
        private RegexSearch RegexSearcher;
        private DateTime BeforeDate;
        private DateTime AfterDate;
        private bool CheckForMacro;
        private OLEExplorer OLXExplorer;


        public FSSearcher(string d, List<string> f, List<string> k, bool s, UInt64 maxfs, bool systemdirs, RegexSearch regex, DateTime beforedate, DateTime afterdate, bool CheckForMacro) {
            this.SearchDirectory = d;
            this.Filetypes = f;
            this.Keywords = k;
            this.Results = new List<string>();
            this.searchContents = s;
            this.maxFileSizeInKB = maxfs;
            this.SystemDirs = systemdirs;
            this.RegexSearcher = regex;
            if (beforedate != null) {
                this.BeforeDate = beforedate;
            }
            if (afterdate != null) {
                this.AfterDate = afterdate;
            }
            this.CheckForMacro = CheckForMacro;
            this.OLXExplorer = null;
        }


        public void Search() {
            if (Directory.Exists(SearchDirectory)) {

                FilesFilteredOnExtension = EnumerateFiles(SearchDirectory, "*.*", SearchOption.AllDirectories);
                foreach (string filepath in FilesFilteredOnExtension) {
                    if (ContainsKeyword(Path.GetFileName(filepath))) {
                        Results.Add(filepath);
                    }
                }
                if (CheckForMacro) {
                    OLXExplorer = new OLEExplorer();
                }
                foreach (string i in Results) {
                    bool containsVBA = false;
                    if (CheckForMacro && EndsWithOffice2003Extension(i)) {
                        containsVBA = OLXExplorer.CheckForVBAMacros(i);
                        if (!containsVBA)
                            continue;
                    }
                    if (BeforeDate != DateTime.MinValue || AfterDate != DateTime.MinValue) {
                        if (MatchesLastWrite(i)) {
                            Console.WriteLine("[+] {0}", i);
                        } else {
                            continue;
                        }
                    } else {
                        Console.WriteLine("[+] {0}", i);
                    }
                    
                }

                // Now search contents
                if (searchContents) {
                    Console.WriteLine("[*] Done searching file system, now searching contents");
                    var contentsSearcher = new ContentsSearcher(FilesFilteredOnExtension, Keywords, RegexSearcher, this.maxFileSizeInKB);
                    contentsSearcher.Search();
                }
            }
        }

        IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOpt) {
            try {
                var dirFiles = Enumerable.Empty<string>();
                if (searchOpt == SearchOption.AllDirectories && IsFolderValid(path)) {
                    dirFiles = Directory.EnumerateDirectories(path)
                                            .SelectMany(x => EnumerateFiles(x, searchPattern, searchOpt)
                                            .Where(fi => EndsWithExtension(fi) && IsFolderValid(fi))
                                        );

                }
                return dirFiles.Concat(Directory.EnumerateFiles(path, searchPattern).Where(fi => EndsWithExtension(fi)));

            } catch (UnauthorizedAccessException ex) {
                return Enumerable.Empty<string>();

            } catch (PathTooLongException ex) {
                // Microsoft solution: https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-enumerate-directories-and-files
                Console.WriteLine("[!] {0} is too long. Continuing with next directory.", path);
                return Enumerable.Empty<string>();

            } catch (System.IO.IOException ex) {
                return Enumerable.Empty<string>();

            }
        }

        private bool ContainsKeyword(string fname) {
            if (Keywords.Count > 0) {
                foreach (string keyword in Keywords) {
                    //if (fname.ToLower().Contains(keyword.ToLower())) {
                    if (RegexSearcher.checkForRegexPatterns(fname.ToLower())) {
                        return true;
                    }
                }
                return false;
            } else {
                // No Keywords provided, we want to match on anything.
                return true;
            }

        }

        // Returns true iff path is not %WINDIR% or %APPDATA% or not Program Files when SystemDir is False.
        private bool IsFolderValid(string p) {
            if (!SystemDirs && p.Contains(":\\Program Files")) {
                return false;
            }
            return (p.Contains(":\\Windows") || (p.Contains(":\\Users") && p.Contains("\\AppData"))) == false;
        }

        public bool MatchesLastWrite(string path) {
            FileInfo fi = new FileInfo(path);
            var lastmodified = fi.LastWriteTime;
            if (BeforeDate != DateTime.MinValue && lastmodified.Date < BeforeDate.Date) {
                return true;
            }
            if (AfterDate != DateTime.MinValue && lastmodified.Date > AfterDate.Date) {
                return true;
            }
            return false;
        }

        private bool EndsWithExtension(string path) {
            foreach (string ext in Filetypes) {
                if (path.ToLower().EndsWith(ext.ToLower())) {
                    return true;
                }
            }
            return false;
        }

        private bool EndsWithOffice2003Extension(string path) {
            foreach (string OfficeExt in Office2003Extentions) {
                if ((path.ToLower().EndsWith(OfficeExt))) {
                    return true;
                }
            }
            return false;
        }
    }


    public class ContentsSearcher {

        private IEnumerable<string> Directories;
        private List<string> Keywords;
        private UInt64 MAX_FILE_SIZE;
        private static readonly string[] OfficeExtentions = { ".doc", ".docx", ".xls", ".xlsx" };
        private RegexSearch RegexSearcher;

        public ContentsSearcher(IEnumerable<string> directories, List<string> keywords, RegexSearch regex, UInt64 maxFileSizeInKB) {
            this.Directories = directories;
            this.Keywords = keywords;
            this.RegexSearcher = regex;
            this.MAX_FILE_SIZE = maxFileSizeInKB;
        }

        // Searches the contents of filtered files. Does not care about exceptions.
        public void Search() {
            foreach (String dir in Directories) {
                try { 
                    var fileInfo = new FileInfo(ConvertToNTPath(dir));

                    string fileContents;
                    if (Convert.ToUInt64(fileInfo.Length) < 1024 * this.MAX_FILE_SIZE) {
                        if (IsOfficeExtension(fileInfo.Extension)) {
                            try {
                                var reader = new FilterReader(fileInfo.FullName);
                                fileContents = reader.ReadToEnd();
                                CheckForKeywords(fileContents, fileInfo);
                            } catch (Exception e) { Console.WriteLine("[-] Could not read contents of {0}", PrettyPrintNTPath(fileInfo.FullName)); }
                        } else {
                            //normal file
                            try {
                                CheckForKeywords(File.ReadAllText(fileInfo.FullName), fileInfo);
                            } catch (Exception e) {
                                Console.WriteLine("[-] Could not read contents of {0}", PrettyPrintNTPath(fileInfo.FullName)); }

                        }
                    } else {
                        Console.WriteLine("[-] File exceeds max file size {0}", PrettyPrintNTPath(fileInfo.FullName));
                    }
                } catch (PathTooLongException ex) {
                    Console.WriteLine("[-] Path {0} is too long. Skipping.", dir);
                    continue;
                } catch (Exception e) {
                    Console.WriteLine("[-] Some unknown exception {0} occured while processing {1}. Continuing with the next directory.", e.Message, dir);
                }
            }
        }

        // Converts DOS path to NT path to support > 260 chars. Also takes into account UNC for shares with '$' signs in them.
        private String ConvertToNTPath(String path) {
            if (path.StartsWith(@"\\")) {
                return @"\\?\UNC\" + path.TrimStart('\\');
            } else {
                return @"\\?\" + path;
            }
        }

        // Remove NT prefixes
        private String PrettyPrintNTPath(String NTPath) {
            if (NTPath.StartsWith(@"\\?\UNC\")) {
                return NTPath.Replace(@"\\?\UNC\", @"\\");
            } else if (NTPath.StartsWith(@"\\?\")) {
                return NTPath.Replace(@"\\?\", "");
            } else {
                return NTPath;
            }
        }

        // Prints the file and keyword iff a keyword is found in its contents.
        private void CheckForKeywords(string contents, FileInfo fileInfo) {
            try {
                // Office docs are weird, do not contains newlines when extracted.
                var found = HasKeywordInLargeString(contents);
                if (!found.Equals("")) {
                    Console.WriteLine("[+] {0}: \n\t {1}\n", PrettyPrintNTPath(fileInfo.FullName), found);
                }
            } catch (Exception e) {
                Console.WriteLine("[!] The {0} could not be read.", PrettyPrintNTPath(fileInfo.FullName));
            }
        }

        // Checks if a string contains any of the keywords we're looking for and returns keyword incl. limited context
        public string HasKeywordInLargeString(string keywordLine) {
            var res = "";
            int buffer = 15;
            foreach (string keyword in Keywords) {
                //int location = keywordLine.IndexOf(keyword);
                int location = ContainsAny(keywordLine);
                while (location != -1) {
                    // take buffer before and after:
                    int start = location - Math.Min(buffer, location); // don't take before start
                    int end = location + keyword.Length
                            + Math.Min(buffer, keywordLine.Length - location - keyword.Length); // don't take after end
                    res += "..." + keywordLine.Substring(start, end - start) + "... ";
                    location = keywordLine.IndexOf(keyword, location + 1);
                }
            }
            return res;
        }


        // Return true iff contents contain any of the keywords.
        private int ContainsAny(string contents) {
            var res = -1;
            res = RegexSearcher.GetIndexOfRegexPattern(contents.ToLower());
            if (res != -1) {
                return res;
            }
            return res;
        }

        private bool IsOfficeExtension(string ext) {
            foreach (string OfficeExt in OfficeExtentions) {
                if (ext.ToLower().Equals(OfficeExt)) {
                    return true;
                }
            }
            return false;
        }


    }


}
