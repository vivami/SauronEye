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
        private const int MAX_PATH = 260;
        private IEnumerable<string> FilesFilteredOnExtension;

        public FSSearcher(string d, List<string> f, List<string> k, bool s, bool systemdirs) {
            this.SearchDirectory = d;
            this.Filetypes = f;
            this.Keywords = k;
            this.Results = new List<string>();
            this.searchContents = s;
            this.SystemDirs = systemdirs;
        }


        public void Search() {
                if (Directory.Exists(SearchDirectory)) {
                    //Console.WriteLine("Searching dir: " + SearchDirectory);
                    DirectoryInfo dirInfo = new DirectoryInfo(SearchDirectory);

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
                        ContentsSearcher s = new ContentsSearcher(FilesFilteredOnExtension, Keywords);
                        s.Search();
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
                return dirFiles.Concat(Directory.EnumerateFiles(path, searchPattern));
            } catch (UnauthorizedAccessException ex) {
                return Enumerable.Empty<string>();
            }
        }

        private bool ContainsKeyword(string fname) {
            foreach (string keyword in Keywords) {
                if (fname.ToLower().Contains(keyword.ToLower())) {
                    return true;
                }
            }
            return false;
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


    class ContentsSearcher {

        private static IEnumerable<string> Directories;
        private static List<string> Keywords;
        private const int MAX_FILE_SIZE = 1000000; // 1MB
        private static readonly string[] OfficeExtentions = { ".doc", ".docx", ".xls", ".xlsx" };


        public ContentsSearcher(IEnumerable<string> d, List<string> k) {
            Directories = d;
            Keywords = k;
        }

        // Searches the contents of filtered files. Does not care about exceptions.
        public void Search() {
            foreach (String dir in Directories) { 
                FileInfo fi = new FileInfo(dir);
                string fileContents;
                if (fi.Length < MAX_FILE_SIZE) {
                    if (IsOfficeExtension(fi.Extension)) {
                        try {
                            TextReader reader = new FilterReader(fi.FullName);
                            fileContents = reader.ReadToEnd();
                            CheckForKeywords(fileContents, fi);
                        } catch (Exception e) { Console.WriteLine("[-] Could not read contents of {0}", fi.FullName); }
                    } else {
                        //normal file
                        try {
                            CheckForKeywords(File.ReadAllText(fi.FullName), fi);
                        } catch (Exception e) { Console.WriteLine("[-] Could not read contents of {0}", fi.FullName); }

                    }
                } else {
                    Console.WriteLine("[-] File exceeds 1MB file size {0}", fi.FullName);
                }
            }
        }

        // Prints the file and keyword iff a keyword is found in its contents.
        private void CheckForKeywords(string contents, FileInfo fi) {
            try {
                // Office docs are weird, do not contains newlines when extracted.
                var found = HasKeywordInLargeString(contents);
                if (!found.Equals("")) {
                    Console.WriteLine("[+] {0}: \n\t {1}\n", fi.FullName, found);
                }
            } catch (Exception e) {
                Console.WriteLine("[!] The {0} could not be read.", fi.FullName);
            }
        }

        // Checks if a string contains any of the keywords we're looking for and returns keyword incl. limited context
        private string HasKeywordInLargeString(string keywordLine) {
            var res = "";
            var splitted = keywordLine.Split(' ');
            for (int i = 0; i < splitted.Length; i++) {
                if (ContainsAny(splitted[i].ToLower())) {
                    if (i + 2 <= splitted.Length) {
                        res = Regex.Replace(string.Join(" ", splitted, i - 2, 5), @"\t|\n|\r", " "); 
                        //res = splitted[i - 2] + splitted[i - 1] + splitted[i] + splitted[i + 1] + splitted[i + 2];
                    } else {
                        // this is ugly: res = "two words + keyword + two words" minus newlines because that is ugly.
                        res = Regex.Replace(string.Join(" ", splitted, i - 2, i) + string.Join(" ", splitted.Skip(i)), @"\t|\n|\r", " ");
                    }
                }
            }
            return res;
        }


        // Return true iff contents contain any of the keywords.
        private bool ContainsAny(string contents) {
            foreach (string keyword in Keywords) {
                if (contents.Contains(keyword)) {
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
