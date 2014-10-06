using AutoRSS.Library.WordProcessor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Library.UnitTests
{
    public class StopWordRemoverTest
    {
        WordBreaker wordBreaker = new WordBreaker();
        StopWordRemover stopWordRemover = new StopWordRemover();
        CaseProcessor caseProcessor = new CaseProcessor();

        public void Test()
        {
            RunTestCase("Hello world!", "hello", "world");
            RunTestCase("Welcome to this new life. Do you enjoy it? I'm certainly enjoying it", "new", "life", "enjoy", "enjoy");
        }

        private void RunTestCase(string paragraph, params string[] expected)
        {
            string[] words = wordBreaker.BreakParagraph(paragraph);
            words = caseProcessor.Lower(words);
            words = stopWordRemover.RemoveStopWords(words);
            Console.Write("Test case: " + paragraph + ", Expected: " + expected.Length + ", Actual: " + words.Length);
            bool success = words.Length == expected.Length;
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
