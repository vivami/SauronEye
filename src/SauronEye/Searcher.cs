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

        private string SearchDirectory;
        private List<string> Filetypes;
        private List<string> Keywords;
        private List<string> Results;
        private bool searchContents;
        private bool SystemDirs;
        private IEnumerable<string> FilesFilteredOnExtension;
        private RegexSearch RegexSearcher;

        public FSSearcher(string d, List<string> f, List<string> k, bool s, bool systemdirs, RegexSearch regex) {
            this.SearchDirectory = d;
            this.Filetypes = f;
            this.Keywords = k;
            this.Results = new List<string>();
            this.searchContents = s;
            this.SystemDirs = systemdirs;
            this.RegexSearcher = regex;
        }


        public void Search() {
            if (Directory.Exists(SearchDirectory)) {

                FilesFilteredOnExtension = EnumerateFiles(SearchDirectory, "*.*", SearchOption.AllDirectories);
                foreach (string filepath in FilesFilteredOnExtension) {
                    if (ContainsKeyword(Path.GetFileName(filepath))) {
                        Results.Add(filepath);
                    }
                }
                foreach (string i in Results) {
                    Console.WriteLine("[+] {0}", i);
                }

                // Now search contents
                if (searchContents) {
                    Console.WriteLine("[*] Done searching file system, now searching contents");
                    var contentsSearcher = new ContentsSearcher(FilesFilteredOnExtension, Keywords, RegexSearcher);
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

        private bool EndsWithExtension(string path) {
            foreach (string ext in Filetypes) {
                if (path.ToLower().EndsWith(ext.ToLower())) {
                    return true;
                }
            }
            return false;
        }
    }


    public class ContentsSearcher {

        private IEnumerable<string> Directories;
        private List<string> Keywords;
        private int MAX_FILE_SIZE = 1000000; // 1MB
        private static readonly string[] OfficeExtentions = { ".doc", ".docx", ".xls", ".xlsx" };
        private RegexSearch RegexSearcher;

        public ContentsSearcher(IEnumerable<string> directories, List<string> keywords, RegexSearch regex) {
            this.Directories = directories;
            this.Keywords = keywords;
            this.RegexSearcher = regex;
        }

        // Searches the contents of filtered files. Does not care about exceptions.
        public void Search() {
            foreach (String dir in Directories) {
                try {
                    var NTdir = @"\\?\" + dir;
                    var fileInfo = new FileInfo(NTdir);

                    string fileContents;
                    if (fileInfo.Length < MAX_FILE_SIZE) {
                        if (IsOfficeExtension(fileInfo.Extension)) {
                            try {
                                var reader = new FilterReader(fileInfo.FullName);
                                fileContents = reader.ReadToEnd();
                                CheckForKeywords(fileContents, fileInfo);
                            } catch (Exception e) { Console.WriteLine("[-] Could not read contents of {0}", fileInfo.FullName.Replace(@"\\?\", "")); }
                        } else {
                            //normal file
                            try {
                                CheckForKeywords(File.ReadAllText(fileInfo.FullName), fileInfo);
                            } catch (Exception e) {
                                Console.WriteLine("[-] Could not read contents of {0}", fileInfo.FullName.Replace(@"\\?\", "")); }

                        }
                    } else {
                        Console.WriteLine("[-] File exceeds 1MB file size {0}", fileInfo.FullName.Replace(@"\\?\", ""));
                    }
                } catch (PathTooLongException ex) {
                    Console.WriteLine("[-] Path {0} is too long. Skipping.", dir);
                    continue;
                } catch (Exception e) {
                    Console.WriteLine("[-] Some unknown exception {0} occured while processing {1}. Continuing with the next directory.", e.Message, dir);
                }
            }
        }

        // Prints the file and keyword iff a keyword is found in its contents.
        private void CheckForKeywords(string contents, FileInfo fileInfo) {
            try {
                // Office docs are weird, do not contains newlines when extracted.
                var found = HasKeywordInLargeString(contents);
                if (!found.Equals("")) {
                    Console.WriteLine("[+] {0}: \n\t {1}\n", fileInfo.FullName.Replace(@"\\?\", ""), found);
                }
            } catch (Exception e) {
                Console.WriteLine("[!] The {0} could not be read.", fileInfo.FullName.Replace(@"\\?\", ""));
            }
        }

        // Checks if a string contains any of the keywords we're looking for and returns keyword incl. limited context
        private string HasKeywordInLargeString(string keywordLine) {
            var res = "";
            var splitted = keywordLine.Split(' ');
            for (int i = 0; i < splitted.Length; i++) {
                if (ContainsAny(splitted[i].ToLower())) {
                    if (i >= 2 && i + 2 <= splitted.Length) {
                        // word1 word2 keyword word3 word4
                        res += Regex.Replace(string.Join(" ", splitted, i - 2, 4), @"\t|\n|\r", " ");
                        //res += Regex.Replace(string.Join(" ", splitted, i - 2, 4), @"\t|\n|\r", " ") + ", ";
                        //res = splitted[i - 2] + splitted[i - 1] + splitted[i] + splitted[i + 1] + splitted[i + 2];
                    } else if (i + 2 < splitted.Length) {
                        // keyword word1 word2
                        res += Regex.Replace(string.Join(" ", splitted, i, 2) + string.Join(" ", splitted.Skip(i)), @"\t|\n|\r", " ");
                        //res += Regex.Replace(string.Join(" ", splitted, i, 2) + string.Join(" ", splitted.Skip(i)), @"\t|\n|\r", " ") + ", ";
                    } else if (i >= 2) {
                        // word1 word2 keyword
                        //res += string.Join(" ", splitted, i - 2, 2) + " " + splitted[i];
                        res += Regex.Replace(string.Join(" ", splitted, i - 2, 2) + " " + splitted[i], @"\t|\n|\r", " ");
                        //res = Regex.Replace(string.Join(" ", splitted, i - 2, 2) + " " + splitted[i], @"\t|\n|\r", " ") + ", ";
                    } else {
                        //res += string.Join(" ", splitted, i, 1) + string.Join(" ", splitted.Skip(i));
                        res += Regex.Replace(string.Join(" ", splitted, i, 1) + string.Join(" ", splitted.Skip(i)), @"\t|\n|\r", " ") + ", ";
                    }
                }
            }
            return res;
        }


        // Return true iff contents contain any of the keywords.
        private bool ContainsAny(string contents) {
            foreach (string keyword in Keywords) {
                //if (contents.Contains(keyword)) {
                if (RegexSearcher.checkForRegexPatterns(contents.ToLower())) {
                    return true;
                }
            }
            return false;
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
