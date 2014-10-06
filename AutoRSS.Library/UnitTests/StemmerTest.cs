using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SStemmer = AutoRSS.Library.WordProcessor.PorterStemmer;

namespace AutoRSS.Library.UnitTests
{
    public class StemmerTest
    {
        SStemmer stemmer = new SStemmer();
        public void Test()
        {
            Test("going", "go");
            Test("International", "intern");
            Test("Hello", "hello");
            Test("easiest", "easy");
            Test("guesses", "guess");
            Test("Goals", "goal");
            Test("seriously", "serious");
            Test("quietly", "quiet");
            Test("died", "die");
            Test("Sanitizer", "sanitize");
            Test("nation", "nation");
        }

        private void Test(string input, string expected)
        {
            string output = stemmer.StemWord(input);
            bool success = output.Equals(expected, StringComparison.OrdinalIgnoreCase);
            Console.Write("Test case: " + input + ", Expected: " + expected + ", Actual: " + output);
            ConsoleColor color = Console.ForegroundColor;
            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" - SUCCESS");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" - FAILURE");
            }

            Console.ForegroundColor = color;
        }
    }
}
