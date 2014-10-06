using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoRSS.Library.Correlation
{
    public struct WordMetadata
    {
        public int Index { get; set; }
        public int Occurances { get; set; }
    }

    [DataContract]
    [Serializable]
    public class CorrelationMatrix
    {
        private int lastWordIndex = 0;
        private HashSet<string> _visitedWords = new HashSet<string>();
        private ConcurrentDictionary<string, int> _words = new ConcurrentDictionary<string, int>(new CaseInsensitiveStringEqualityComparer());
        private Dictionary<WordPair, double> _matrix = new Dictionary<WordPair, double>();
        private ConcurrentDictionary<double, double> _matrix2 = new ConcurrentDictionary<double, double>();
        private ConcurrentDictionary<string, int> _occurances = new ConcurrentDictionary<string, int>(new CaseInsensitiveStringEqualityComparer());
        private ConcurrentDictionary<string, WordMetadata> _wordsMetadata = new ConcurrentDictionary<string, WordMetadata>(new CaseInsensitiveStringEqualityComparer());

        public CorrelationMatrix()
        {
        }

        public CorrelationMatrix(int initialWordsCount, int initialPairsCount)
        {
            _matrix2 = new ConcurrentDictionary<double,double>(1, initialPairsCount);
            _wordsMetadata = new ConcurrentDictionary<string, WordMetadata>(1, initialWordsCount);
        }

        [DataMember]
        public ConcurrentDictionary<string, int> Words
        {
            get
            {
                return _words;
            }
            set
            {
                _words = value;
            }
        }

        [DataMember]
        public ConcurrentDictionary<double, double> Matrix
        {
            get
            {
                return _matrix2;
            }
            set
            {
                _matrix2 = value;
            }
        }

        [DataMember]
        public ConcurrentDictionary<string, int> Occurances
        {
            get
            {
                return _occurances;
            }
            set
            {
                _occurances = value;
            }
        }

        [DataMember]
        public ConcurrentDictionary<string, WordMetadata> WordsMetadata
        {
            get
            {
                return _wordsMetadata;
            }
            set
            {
                _wordsMetadata = value;
            }
        }

        public override string ToString()
        {
            return "Unique Words: " + WordsMetadata.Count + ", Pairs: " + Matrix.Count;
        }

        public void Add(string[] words)
        {
            //int totalAddOccuranceCount = 0;
            //int totalAddFrequencyCount = 0;
            //long totalAddOccuranceTime = 0;
            //long totalAddFrequencyTime = 0;
            for (int i = 0; i < words.Length; i++)
            {
                //using (MonitoredScope scope = new MonitoredScope("AddOccurance"))
                {
                    AddOccurance(WordPair.Normalize(words[i]));
                    //totalAddOccuranceTime += scope.ElapsedMilliseconds;
                    //totalAddOccuranceCount++;
                }

                for (int j = i + 1; j < words.Length; j++)
                {
                    //using (MonitoredScope scope = new MonitoredScope("AddFrequency"))
                    {
                        if (string.Compare(words[i], words[j], ignoreCase: true, culture: CultureInfo.InvariantCulture) < 0)
                        {
                            //Add(GetWordIndex(words[i]), GetWordIndex(words[j]), 1.0 / (j - i + 1));
                            Add2(GetWordIndex2(words[i]), GetWordIndex2(words[j]), 1.0 / (j - i + 1));
                        }
                        else
                        {
                            //Add(GetWordIndex(words[j]), GetWordIndex(words[i]), 1.0 / (j - i + 1));
                            Add2(GetWordIndex2(words[j]), GetWordIndex2(words[i]), 1.0 / (j - i + 1));
                        }

                        //totalAddFrequencyTime += scope.ElapsedMilliseconds;
                        //totalAddFrequencyCount++;
                    }
                }
            }

            //Logger.Log("Average AddOccuranceTime: " + (double)totalAddOccuranceTime / totalAddOccuranceCount);
            //Logger.Log("Average AddFrequencyTime: " + (double)totalAddFrequencyTime / totalAddFrequencyCount);
        }

        public int GetWordIndex2(string word)
        {
            //int index;
            //index = _words.GetOrAdd(word, Interlocked.Increment(ref lastWordIndex));
            WordMetadata newMetadata =
            _wordsMetadata.GetOrAdd(word, (key) => new WordMetadata
            {
                Index = Interlocked.Increment(ref lastWordIndex),
                Occurances = 0
            });


            return newMetadata.Index;
        }

        private WordMetadata GetWordMetadata(string word)
        {
            WordMetadata newMetadata =
            _wordsMetadata.GetOrAdd(word, (key) => new WordMetadata
            {
                Index = Interlocked.Increment(ref lastWordIndex),
                Occurances = 0
            });


            return newMetadata;
        }

        private bool TryGetExistingWordMetadata(string word, out WordMetadata result)
        {
            return _wordsMetadata.TryGetValue(word, out result);
        }

        public string GetWordIndex(string word)
        {
            int index;
            if (_visitedWords.Contains(word))
            {
                index = _words[word];
            }
            else
            //if (!_words.TryGetValue(word, out index))
            {
                index = _words[word] = _words.Count + 1;
                _visitedWords.Add(word);
            }

            return index.ToString();
        }

        public void Add2(int word1, int word2, double value)
        {
            double key = word1 + ((Int64)word2 << 32);
            //double existingValue;
            _matrix2.AddOrUpdate(key, value, (existingKey, existingValue) => value + existingValue);
            //if (_matrix2.TryGetValue(key, out existingValue))
            //{
            //    _matrix2[key] = value + existingValue;
            //}
            //else
            //{
            //    _matrix2[key] = value;
            //}
        }

        public void Add(string word1, string word2, double value)
        {
            //if (word1.CompareTo(word2) > 0)
            //{
            //    Add(word2, word1, value);
            //}
            //else
            //{
            //    if (_matrix2.ContainsKey(word1))
            //    {
            //        Dictionary<string, double> word2Dictionary = _matrix2[word1];
            //        if (word2Dictionary.ContainsKey(word2))
            //        {
            //            word2Dictionary[word2] += value;
            //        }
            //        else
            //        {
            //            word2Dictionary[word2] = value;
            //        }
            //    }
            //    else
            //    {
            //        Dictionary<string, double> word2Dictionary = _matrix2[word1] = new Dictionary<string, double>();
            //        word2Dictionary[word2] = value;
            //    }
            //}
            WordPair word = new WordPair(word1, word2);
            double existingValue;
            if (_matrix.TryGetValue(word, out existingValue))
            {
                _matrix[word] = value + existingValue;
            }
            else
            {
                _matrix[word] = value;
            }
        }

        public double GetNormalized(WordPair word)
        {
            double absolute = Get(word);
            int n1 = GetOccurances(word.Word1);
            int n2 = GetOccurances(word.Word2);
            int totalOccurances = n1 * n2;
            if (totalOccurances > 0)
            {
                return absolute / totalOccurances;
            }
            else
            {
                return 0;
            }
        }

        public double GetNormalized(string word1, string word2)
        {
            bool wordFound = false;
            WordMetadata word1Metadata;
            wordFound = TryGetExistingWordMetadata(word1, out word1Metadata);
            if (!wordFound)
            {
                return 0;
            }
            WordMetadata word2Metadata;
            wordFound = TryGetExistingWordMetadata(word2, out word2Metadata);
            if (!wordFound)
            {
                return 0;
            }

            int word1Int = word1Metadata.Index;// GetWordIndex2(word1);
            int word2Int = word2Metadata.Index;// GetWordIndex2(word2);
            double key = word1Int + ((Int64)word2Int << 32);
            double result = 0;

            if (_matrix2.TryGetValue(key, out result))//_matrix2.ContainsKey(key))
            {
                //result = _matrix2[key];
                int n1 = word1Metadata.Occurances;// GetOccurances(word1);
                int n2 = word2Metadata.Occurances;// GetOccurances(word2);
                int totalOccurances = n1 * n2;
                if (totalOccurances > 0)
                {
                    result = result / totalOccurances;
                }
                else
                {
                    if (!Debugger.IsAttached)
                    {
                        Debugger.Launch();
                    }

                    Debugger.Break();
                    result = 0;
                }
            }
            else
            {
                result = 0;
            }

            return result;
        }

        public double Get(WordPair word)
        {
            double result = 0;
            if (_matrix.ContainsKey(word))
            {
                result = _matrix[word];
            }
            else
            {
                result = 0;
            }

            return result;
        }

        private int GetOccurances(string word)
        {
            WordMetadata result;
            //_occurances.TryGetValue(word, out result);
            _wordsMetadata.TryGetValue(word, out result);
            return result.Occurances;
        }

        private void AddOccurance(string word)
        {
            //_occurances.AddOrUpdate(word, 1, (key, existingValue) => existingValue + 1);
            _wordsMetadata.AddOrUpdate(word, new WordMetadata { Occurances = 1 }, (key, existingValue) =>
            {
                existingValue.Occurances++;
                return existingValue;
            });
        }
    }

    [Serializable]
    public class WordPair
    {
        private string _key;
        public string Word1 { get; private set; }
        public string Word2 { get; private set; }

        public WordPair()
        {
        }

        public WordPair(string word1, string word2)
        {
            //word1 = Normalize(word1);
            //word2 = Normalize(word2);
            //if (word1.CompareTo(word2) < 0)
            //{
            Word1 = word1;
            Word2 = word2;
            //}
            //else
            //{
            //    Word1 = word2;
            //    Word2 = word1;
            //}

            //StringBuilder sb = new StringBuilder();
            //sb.Append(Word1);
            //sb.Append("_");
            //sb.Append(Word1);
            //_key = sb.ToString();
            _key = Word1 + "_" + Word2;
        }

        public static string Normalize(string word)
        {
            return word;
            //return word.ToLowerInvariant();
        }

        public override string ToString()
        {
            return _key;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            WordPair other = obj as WordPair;
            if (null != other)
            {
                return ToString().Equals(other.ToString());
            }

            return false;
        }
    }
}
