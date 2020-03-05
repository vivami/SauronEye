using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SauronEye;


namespace SauronEyeTests {
    [TestClass]
    public class ArgumentParserTester {

        // Unzip src/SauronEyeTests/testfiles.zip first
        [TestMethod]
        public void TestLongPaths() {
            var LongDirectories = new List<string> {
                Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName + @"\testfiles\ininfafasf ienflaflieflanfeifnalnfae\ininfafasf ienflaflieflanfeifnalnfae\ininfafasf ienflaflieflanfeifnalnfae\ininfafasf ienflaflieflanfeifnalnfae\ienflaflieflanfeifnalnfae\dfiwnfwnfwnfownefinewnf.txt",
            };
            var Regex = new SauronEye.RegexSearch(new List<string> { "pass" } );
            var Keywords = new List<string> { "pass" };
            var ContentSearcher = new SauronEye.ContentsSearcher(LongDirectories, Keywords, Regex, 1024);
            ContentSearcher.Search();

            var currentConsoleOut = Console.Out;

            var outputPath = @"ininfafasf ienflaflieflanfeifnalnfae\ininfafasf ienflaflieflanfeifnalnfae\ininfafasf ienflaflieflanfeifnalnfae\ininfafasf ienflaflieflanfeifnalnfae\ienflaflieflanfeifnalnfae\dfiwnfwnfwnfownefinewnf.txt";
            var outputMatch = @"this is pass";
            using (var consoleOutput = new ConsoleOutput())
            {
                ContentSearcher.Search();
                Assert.IsTrue(consoleOutput.GetOuput().Contains(outputPath));
                Assert.IsTrue(consoleOutput.GetOuput().Contains(outputMatch));
            }

            Assert.AreEqual(currentConsoleOut, Console.Out);

        }

        [TestMethod]
        public void TestHasKeywordInLargeString() {
            var Directories = new List<string> { "" };
            var Regex = new SauronEye.RegexSearch(new List<string> { "pass" });
            var Keywords = new List<string> { "pass" };
            var ContentSearcher = new SauronEye.ContentsSearcher(Directories, Keywords, Regex, 1024);

            var testCases = new Dictionary<string, string>();
            testCases.Add("this is a long string containing a password", "...g containing a password... ");
            testCases.Add("password is this yet another", "...password is this ye... ");
            testCases.Add("password", "...password... ");
            testCases.Add("not a wachtwoord", "");
            testCases.Add("another password in this test makes it to password twice", "...another password in this te... ...st makes it to password twice... ");
            testCases.Add("password begin and at the end password", "...password begin and ... ...and at the end password... ");
            testCases.Add("long path before the password=wetiife", "...ath before the password=wetiife... ");
            testCases.Add("this is anther password testcase that is a normal one", "...this is anther password testcase t... ");

            foreach (KeyValuePair<string, string> kvp in testCases) {
                Assert.AreEqual(kvp.Value, ContentSearcher.HasKeywordInLargeString(kvp.Key));
            }
        }
    }


    public class ConsoleOutput : IDisposable {
        private StringWriter stringWriter;
        private TextWriter originalOutput;

        public ConsoleOutput() {
            stringWriter = new StringWriter();
            originalOutput = Console.Out;
            Console.SetOut(stringWriter);
        }

        public string GetOuput() {
            return stringWriter.ToString();
        }

        public void Dispose() {
            Console.SetOut(originalOutput);
            stringWriter.Dispose();
        }
    }
}
