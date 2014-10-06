using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Library.WordProcessor
{
    public class WordBreaker
    {
        private static char[] s_SeparatorCharacters = CreateSeparatorCharacters();
        private char[] _SeparatorCharacters;
        
        public WordBreaker()
        {
            _SeparatorCharacters = s_SeparatorCharacters;
        }

        public WordBreaker(char[] charactersToExcludeFromSeparators)
        {
            List<char> chars = new List<char>(s_SeparatorCharacters);
            foreach (char c in charactersToExcludeFromSeparators)
            {
                chars.Remove(c);
            }

            _SeparatorCharacters = chars.ToArray();
        }

        private static char[] CreateSeparatorCharacters()
        {
            List<char> characters = new List<char>();
            for (int i = 0; i < 'A'; i++)
            {
                characters.Add((char)i);
            }

            for (int i = 'Z' + 1; i < 'a'; i++)
            {
                characters.Add((char)i);
            }

            for (int i = 'z' + 1; i < 256; i++)
            {
                characters.Add((char)i);
            }

            return characters.ToArray();
        }

        public string[] BreakParagraph(string paragraph)
        {
            return paragraph.Split(_SeparatorCharacters, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
