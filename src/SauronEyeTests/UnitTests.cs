using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SauronEye;


namespace SauronEyeTests {
    [TestClass]
    public class ArgumentParserTester {

        [TestMethod]
        public void TestExcessiveWhitespace() {
            var ArgumentParser = new SauronEye.ArgumentParser();
            var input = new string[12] { @"-Dirs", @" ", " ", @"C:\,      ", @"  ", @"D:", @"-FILETYPES", @".txt,.bat", @"      ", @",.conf", "-Contents", "-keywords test" };
            var expected = new List<string>() { @"c:\", @"d:" };
            ArgumentParser.ParseArguments(input);
            CollectionAssert.AreEqual(ArgumentParser.Directories, expected);

        }

        // Unzip src/SauronEyeTests/testfiles.zip first
        [TestMethod]
        public void TestLongPaths() {
            var LongDirectories = new List<string> {
                Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName + @"\testfiles\ininfafasf ienflaflieflanfeifnalnfae\ininfafasf ienflaflieflanfeifnalnfae\ininfafasf ienflaflieflanfeifnalnfae\ininfafasf ienflaflieflanfeifnalnfae\ienflaflieflanfeifnalnfae\dfiwnfwnfwnfownefinewnf.txt",
            };
            var Regex = new SauronEye.RegexSearch(new List<string> { "pass" } );
            var Keywords = new List<string> { "pass" };
            var ContentSearcher = new SauronEye.ContentsSearcher(LongDirectories, Keywords, Regex);
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
