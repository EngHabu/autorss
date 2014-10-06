using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Library.WordProcessor
{
    public class CaseProcessor
    {
        public string[] Lower(string[] input)
        {
            List<string> result = new List<string>();
            foreach (string word in input)
            {
                result.Add(word.ToLowerInvariant());
            }

            return result.ToArray();
        }
    }
}
