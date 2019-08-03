using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EPocalipse.IFilter;

namespace SauronEye {
    class FSSearcher {

        private static List<string> Directories;
        private static List<string> Filetypes;
        private static List<string> Keywords;
        private static List<string> Results;
        private static bool searchContents;
        private const int MAX_PATH = 260;
       


        public FSSearcher(List<string> d, List<string> f, List<string> k, bool s) {
            Directories = d;
            Filetypes = f;
            Keywords = k;
            Results = new List<string>();
            searchContents = s;
        }

        public void Search() {
            foreach (string dir in Directories) {
                if (!Directory.Exists(dir)) {
                    continue;
                }
                Console.WriteLine("Searching dir: " + dir);
                DirectoryInfo dirInfo = new DirectoryInfo(dir);

                IEnumerable<string> filteredOnExtension =  EnumerateFiles(dir, "*.*", SearchOption.AllDirectories);
                foreach (string filepath in filteredOnExtension) {
                    if (ContainsKeyword(Path.GetFileName(filepath))) {
                        Results.Add(filepath);
                    }
                }
                foreach (string i in Results) {
                    Console.WriteLine(i);
                }
                if (searchContents) {
                    Console.WriteLine("[!] Done searching file system, now searching contents");
                    ContentsSearcher s = new ContentsSearcher(filteredOnExtension, Keywords);
                    s.Search();
                } 
            }
            return;
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
                //Console.WriteLine("[-] Cannot access: " + path);
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

        // Returns true iff path is not %WINDIR% or %APPDATA%
        private bool IsFolderValid(string p) {
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

        public void Search() {
            foreach(string dir in Directories) {
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
                    continue;
                }
            }
        }

        // Prints the file and keyword iff a keyword is found in its contents.
        private void CheckForKeywords(string contents, FileInfo fi) {
            try {
                // Office docs are weird, do not contains newlines when extracted.
                var found = HasKeywordInLargeString(contents);
                if (!found.Equals("")) {
                    Console.WriteLine("[+] {0}: \n {1}", fi.FullName, found);
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
                        res = string.Join(" ", splitted, i - 2, 5);
                        //res = splitted[i - 2] + splitted[i - 1] + splitted[i] + splitted[i + 1] + splitted[i + 2];
                    } else {
                        res = string.Join(" ", splitted, i - 2, i) + string.Join(" ", splitted.Skip(i));
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
