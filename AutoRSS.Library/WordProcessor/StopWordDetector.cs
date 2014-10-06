using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SStemmer = AutoRSS.Library.WordProcessor.PorterStemmer;

namespace AutoRSS.Library.WordProcessor
{
    public class StopWordDetector
    {
        private static readonly object s_syncObj = new object();
        private static StopWordDetector s_Local;
        private HashSet<string> _stopWords;

        public static StopWordDetector Local
        {
            get
            {
                if (null == s_Local)
                {
                    lock (s_syncObj)
                    {
                        if (null == s_Local)
                        {
                            s_Local = new StopWordDetector();
                        }
                    }
                }

                return s_Local;
            }
        }

        public StopWordDetector()
        {
            _stopWords = new HashSet<string>();
            SStemmer stemmer = new SStemmer();
            using (StreamReader reader = new StreamReader(@"WordProcessor\stopwords.txt"))
            {
                string line = "";
                while (null != (line = reader.ReadLine()))
                {
                    _stopWords.Add(stemmer.StemWord(line.Trim()));
                }
            }
        }

        public bool IsStopWord(string word)
        {
            return _stopWords.Contains(word);
        }
    }
}
