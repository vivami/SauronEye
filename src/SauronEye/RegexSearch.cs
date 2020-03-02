using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SauronEye {

    public class RegexSearch {
        private List<string> Keywords;
        private List<Regex> Regexes;

        public RegexSearch(List<string> keywords) {
            this.Keywords = keywords;
            this.Regexes = new List<Regex>();
            constructRegexes();
        }

        private void constructRegexes() {
            foreach (string keyword in Keywords) {
                Regex regex = new Regex(keyword, RegexOptions.Singleline | RegexOptions.Compiled);
                Regexes.Add(regex);
            }
        }

        // We only match on the first hit, if the file is interesting, we'll be inspecting it anyway
        public bool checkForRegexPatterns(string word) {
            var res = false;
            foreach (Regex regex in Regexes) {
                res = regex.Match(word).Success;
                if (res)
                    return res;
            }
            return res;
        }

        public int GetIndexOfRegexPattern(string word) {
            var res = -1;
            foreach (Regex regex in Regexes) {
                var match = regex.Match(word);
                if (match.Success) {
                    return match.Index;
                }
            }
            return res;
        }
    }
}
