using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Library.Correlation
{
    public class SimilarityAlgorithm
    {
        private CorrelationMatrix _matrix;
        private double _permissibleThreshold = 0.25;
        private double _variationThreshold = 0.5;
        //private double _permissibleThreshold = 0.15;
        //private double _permissibleThreshold = 0.39;
        //private double _variationThreshold = 0.11;
        //private double _variationThreshold = 0.688;

        public SimilarityAlgorithm(CorrelationMatrix matrix)
        {
            _matrix = matrix;
        }

        public SimilarityAlgorithm(CorrelationMatrix matrix, double permissibleThreshold, double variationThreshold)
            : this(matrix)
        {
            _permissibleThreshold = permissibleThreshold;
            _variationThreshold = variationThreshold;
        }

        public double WordSimilarityToWord(Word word1, Word word2)
        {
            if (string.Equals(word1.OriginalString, word2.OriginalString, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }
            else
            {
                return _matrix.GetNormalized(word1.OriginalString, word2.OriginalString);
            }
        }

        public double WordSimilarityToStatement(Word word, Statement statementWords)
        {
            double result = 1;
            foreach (Word statementWord in statementWords.Words)
            {
                result *= (1 - WordSimilarityToWord(word, statementWord));
            }

            return 1 - result;
        }

        private static Dictionary<double, Dictionary<double, double>> _statementToStatementSimCache = new Dictionary<double, Dictionary<double, double>>();
        public double StatementSimilarityToStatement(Statement statement1, Statement statement2)
        {
            double result = 0;

            if (!TryGetFromCache(statement1.Id, statement2.Id, out result))
            {
                Word[] statement1Words = statement1.Words;
                if (statement1Words.Length > 0)
                {
                    foreach (Word statement1Word in statement1Words)
                    {
                        result += WordSimilarityToStatement(statement1Word, statement2);
                    }

                    result /= statement1Words.Length;
                }

                CacheValue(statement1.Id, statement2.Id, result);
            }

            return result;
        }

        private void CacheValue(double id1, double id2, double result)
        {
            Dictionary<double, double> otherStatements;
            if (_statementToStatementSimCache.TryGetValue(id1, out otherStatements))
            {
                otherStatements[id2] = result;
            }
            else if (_statementToStatementSimCache.TryGetValue(id2, out otherStatements))
            {
                otherStatements[id1] = result;
            }
            else
            {
                otherStatements = _statementToStatementSimCache[id1] = new Dictionary<double, double>();
                otherStatements[id2] = result;
            }
        }

        private bool TryGetFromCache(double id1, double id2, out double result)
        {
            result = 0;
            Dictionary<double, double> otherStatements;
            if (_statementToStatementSimCache.TryGetValue(id1, out otherStatements))
            {
                if (otherStatements.TryGetValue(id2, out result))
                {
                    return true;
                }
            }
            else if (_statementToStatementSimCache.TryGetValue(id2, out otherStatements))
            {
                if (otherStatements.TryGetValue(id1, out result))
                {
                    return true;
                }
            }

            return false;
        }

        public bool StatementEqualsToStatement(Statement statement1, Statement statement2)
        {
            bool result = false;
            double sim12 = StatementSimilarityToStatement(statement1, statement2);
            double sim21 = StatementSimilarityToStatement(statement2, statement1);
            if (Math.Min(sim12, sim21) >= _permissibleThreshold && 
                Math.Abs(sim12 - sim21) <= _variationThreshold)
            {
                result = true;
            }

            return result;
        }

        public double CalculateDegreeOfResemblance(Document document1, Document document2)
        {
            double result = 0;
            foreach (Statement statement1 in document1.Statements)
            {
                double multi = 1;
                foreach (Statement statement2 in document2.Statements)
                {
                    multi *= (1 - BoolToInt(StatementEqualsToStatement(statement1, statement2)));
                }

                result += (1 - multi);
            }

            result /= document1.Statements.Length;

            return result;
        }

        public double CalculateOddsRatio(Document document1, Document document2)
        {
            double result = 0;
            result = CalculateDegreeOfResemblance(document1, document2) * CalculateDegreeOfResemblance(document2, document1);
            result /= (1 - result);

            return result;
        }

        private int BoolToInt(bool value)
        {
            return value ? 1 : 0;
        }
    }

    public struct Word
    {
        private string _word;

        public Word(string word)
        {
            _word = word;
        }

        public string OriginalString
        {
            get
            {
                return _word;
            }
        }

        public override bool Equals(object obj)
        {
            return string.Equals(OriginalString, obj as string, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return _word;
        }
    }

    public struct Statement
    {
        private double _id;
        private Word[] _words;
        private string _toString;
        public Statement(Word[] words)
        {
            _words = words;
            _toString = "";
            _id = new Random(Environment.TickCount).NextDouble();
        }

        public double Id
        {
            get
            {
                return _id;
            }
        }

        public Word[] Words
        {
            get
            {
                return _words;
            }
        }

        public override bool Equals(object obj)
        {
            return Words.Equals<Word>(obj as Word[]);
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(_toString))
            {
                StringBuilder sb = new StringBuilder();
                foreach (Word word in Words)
                {
                    sb.Append(word.ToString());
                    sb.Append(" ");
                }

                _toString = sb.ToString();
            }

            return _toString;
        }
    }

    public class Document
    {
        private Statement[] _statements;

        public Document(Statement[] statements)
        {
            _statements = statements;
        }

        public Statement[] Statements
        {
            get
            {
                return _statements;
            }
        }

        public string Id { get; set; }

        public override bool Equals(object obj)
        {
            return Id == (obj as Document).Id;
            //return Statements.Equals<Statement>(obj as Statement[]);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Statement statement in Statements)
            {
                sb.Append(statement.ToString());
                sb.Append(". ");
            }

            return sb.ToString();
        }

    }

    public static class Extentions
    {
        public static bool Equals<T>(this T[] self, T[] second)
        {
            if (self == null && second != null)
            {
                return false;
            }
            else if (self != null && second == null)
            {
                return false;
            }
            else if (self.Length != second.Length)
            {
                return false;
            }

            for (int i = 0; i < self.Length; i++)
            {
                if (self[i].Equals(second[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
