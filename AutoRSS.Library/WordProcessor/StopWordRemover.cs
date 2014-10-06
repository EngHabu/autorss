using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Library.WordProcessor
{
    public class StopWordRemover
    {
        public string[] RemoveStopWords(string[] words)
        {
            List<string> result = new List<string>();
            foreach (string word in words)
            {
                if (StemmedDictionary.Instance.Contains(word) 
                    && !StopWordDetector.Local.IsStopWord(word))
                {
                    result.Add(word);
                }
            }

            return result.ToArray();
        }
    }
}
