using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SStemmer = AutoRSS.Library.WordProcessor.PorterStemmer;

namespace AutoRSS.Library.WordProcessor
{
    public class StemmedDictionary
    {
        private const string DictionaryPath =@"WordProcessor\dictionary.txt";
        private HashSet<string> _words;
        private static StemmedDictionary _instance;
        private static readonly object SyncObject = new object();
        private SStemmer _stemmer = new SStemmer();

        public static StemmedDictionary Instance
        {
            get
            {
                if (null == _instance)
                {
                    lock (SyncObject)
                    {
                        if (null == _instance)
                        {
                            _instance = new StemmedDictionary();
                        }
                    }
                }

                return _instance;
            }
        }

        private StemmedDictionary()
        {
            _words = new HashSet<string>(new CaseInsensitiveStringEqualityComparer());
            if (File.Exists(DictionaryPath))
            {
                using (StreamReader reader = new StreamReader(DictionaryPath))
                {
                    string line = "";
                    while (null != (line = reader.ReadLine()))
                    {
                        _words.Add(line.Trim());
                    }
                }
            }
        }

        public bool Add(string word)
        {
            return _words.Add(_stemmer.StemWord(word.ToLowerInvariant()));
        }

        public bool Contains(string word)
        {
            return _words.Contains(_stemmer.StemWord(word));
        }

        public void Serialize()
        {
            if (File.Exists(DictionaryPath))
            {
                File.Move(DictionaryPath, DictionaryPath + Guid.NewGuid().ToString() + ".txt");
            }

            using (StreamWriter sw = new StreamWriter(DictionaryPath))
            {
                foreach(string word in _words)
                {
                    sw.WriteLine(word);
                }
            }
        }
    }
}
