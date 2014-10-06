using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Library.WordProcessor
{
    public class StatementBreaker
    {
        private static char[] s_SeparatorCharacters = new char[] { '.', '?', ':' };

        public string[] BreakParagraph(string paragraph)
        {
            return paragraph.Split(s_SeparatorCharacters, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
