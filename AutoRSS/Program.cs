using AutoRSS.Library;
using AutoRSS.Library.Correlation;
using AutoRSS.Library.UnitTests;
using AutoRSS.Library.WordProcessor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SStemmer = AutoRSS.Library.WordProcessor.PorterStemmer;

namespace AutoRSS
{
    class Program
    {
        static void Main(string[] args)
        {
            ProgramArguments programArgs = ProgramArguments.Create(args);

            //Console.WriteLine("Stemmer Tests");
            //new StemmerTest().Test();

            //Console.WriteLine("============================================");
            //Console.WriteLine("StopWordRemover Tests");
            //new StopWordRemoverTest().Test();

            //CalculateCorrelationFromWikipediaDB(programArgs);

            //CreateSimilarityReport(programArgs);

            //CreateThresholdTrainingData(programArgs);

            //new NLP().Test("this is life. I'm haytham. You are someone living in D.C.");
        }

        private static void CreateThresholdTrainingData(ProgramArguments programArgs)
        {
            CorrelationMatrix correlationMatrix = LoadCorrelationMatrix(programArgs);
            SimilarityAlgorithm sim = new SimilarityAlgorithm(correlationMatrix);

            while (true)
            {
                ScanTrainData(sim);
                Console.WriteLine("Press Enter to rescan");
                Console.ReadLine();
            }
        }

        private static void ScanTrainData(SimilarityAlgorithm sim)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string path in Directory.GetFiles("TrainData"))
            {
                string fileContents = "";
                using (StreamReader sr = new StreamReader(path))
                {
                    fileContents = sr.ReadToEnd();
                }

                Document document = ConstructDocument(fileContents);
                for (int i = 0; i < document.Statements.Length - 1; i += 2)
                {
                    Statement s1 = document.Statements[i];
                    Statement s2 = document.Statements[i + 1];
                    double s12 = sim.StatementSimilarityToStatement(s1, s2);
                    double s21 = sim.StatementSimilarityToStatement(s2, s1);
                    sb.AppendFormat("{0},{1},{2},{3}\r\n",
                        s1.ToString(),
                        s2.ToString(),
                        Math.Min(s12, s21),
                        Math.Abs(s12 - s21));
                }
            }

            string filename = "autorss_Threshold_" + Guid.NewGuid().ToString() + ".csv";
            using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
            {
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(sb.ToString());
                sw.Flush();
            }

            Console.WriteLine("Threshold Report: " + filename);
        }

        private static void CreateSimilarityReport(ProgramArguments programArgs)
        {
            CorrelationMatrix correlationMatrix = LoadCorrelationMatrix(programArgs);

            SimilarityAlgorithm sim = new SimilarityAlgorithm(correlationMatrix);
            string wikipediaPath = @"C:\Users\haabu\Downloads\enwiki-latest-pages-articles.xml\enwiki-latest-pages-articles.xml";
            using (XmlReader sr = XmlReader.Create(new FileStream(wikipediaPath, FileMode.Open)))
            {
                // Skip first 100
                for (int i = 0; i < 100; i++)
                {
                    bool elementFound = sr.ReadToFollowing("text");
                    if (!elementFound)
                    {
                        break;
                    }
                }

                string filename = "autorss_test_" + Guid.NewGuid().ToString() + ".csv";
                using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                {
                    StreamWriter sw = new StreamWriter(fs);
                    Document prevDocument = null;
                    for (int i = 0; i < 100; i++)
                    {
                        bool elementFound = sr.ReadToFollowing("text");
                        if (elementFound)
                        {
                            string pageContents;
                            //using (MonitoredScope scope = new MonitoredScope("Xml Read Element", TraceLevel.Medium))
                            {
                                sr.ReadStartElement();
                                pageContents = sr.ReadContentAsString();
                            }

                            Document document = ConstructDocument(pageContents);
                            //Console.WriteLine("Ratio: " + sim.CalculateOddsRatio(document, document) + "\r\nDocument Contents: " + pageContents);
                            if (null == prevDocument)
                            {
                                prevDocument = document;
                            }

                            sw.WriteLine(sim.CalculateOddsRatio(document, prevDocument));
                            prevDocument = document;
                        }
                    }

                    sw.Flush();
                }
            }
        }

        private static CorrelationMatrix LoadCorrelationMatrix(ProgramArguments programArgs)
        {
            if (!File.Exists(programArgs.WikipediaDBPath))
            {
                throw new ArgumentException("Invalid Path: " + programArgs.WikipediaDBPath, "programArgs");
            }

            CorrelationMatrix correlationMatrix;
            using (FileStream fs = new FileStream(programArgs.WikipediaDBPath, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                correlationMatrix = formatter.Deserialize(fs) as CorrelationMatrix;
            }
            return correlationMatrix;
        }

        private static Document ConstructDocument(string pageContents)
        {
            StopWordRemover stopWordRemover = new StopWordRemover();
            SStemmer stemmer = new SStemmer();
            WordBreaker wb = new WordBreaker();
            SentenceBreaker sb = SentenceBreaker.Instance;
            List<Statement> statements = new List<Statement>();
            string[] statementsString = sb.BreakIntoSentences(pageContents);
            foreach (string statementString in statementsString)
            {
                string[] wordsString = wb.BreakParagraph(statementString);
                wordsString = stopWordRemover.RemoveStopWords(wordsString);
                wordsString = stemmer.StemWords(wordsString);
                List<Word> words = new List<Word>();
                foreach (string wordString in wordsString)
                {
                    words.Add(new Word(wordString));
                }

                statements.Add(new Statement(words.ToArray()));
            }

            return new Document(statements.ToArray());
        }

        private static void CalculateCorrelationFromWikipediaDB(ProgramArguments programArgs)
        {
            WordBreaker wordBreaker = new WordBreaker();
            StopWordRemover stopwordRemover = new StopWordRemover();
            SStemmer stemmer = new SStemmer();
            CorrelationMatrix correlationMatrix = new CorrelationMatrix();

            string wikipediaPath = @"C:\Users\haabu\Downloads\enwiki-latest-pages-articles.xml\enwiki-latest-pages-articles.xml";
            using (XmlReader sr = XmlReader.Create(new FileStream(wikipediaPath, FileMode.Open)))
            {
                for (int i = 0; i < programArgs.WikipediaStartArticle; i++)
                {
                    bool elementFound = sr.ReadToFollowing("text");
                    if (!elementFound)
                    {
                        break;
                    }
                }

                for (int i = programArgs.WikipediaStartArticle; i < programArgs.WikipediaEndArticle; i++)
                {
                    bool elementFound = sr.ReadToFollowing("text");
                    if (elementFound)
                    {
                        string pageContents;
                        //using (MonitoredScope scope = new MonitoredScope("Xml Read Element", TraceLevel.Medium))
                        {
                            sr.ReadStartElement();
                            pageContents = sr.ReadContentAsString();
                        }

                        string[] words;
                        //using (MonitoredScope scope = new MonitoredScope("Break Paragraph", TraceLevel.Medium))
                        {
                            words = wordBreaker.BreakParagraph(pageContents);
                        }

                        //using (MonitoredScope scope = new MonitoredScope("Remove Stop Words", TraceLevel.Medium))
                        {
                            words = stopwordRemover.RemoveStopWords(words);
                        }

                        //using (MonitoredScope scope = new MonitoredScope("Stem Words", TraceLevel.Medium))
                        {
                            words = stemmer.StemWords(words);
                        }

                        //using (MonitoredScope scope = new MonitoredScope("Calculate correlation", TraceLevel.Medium))
                        {
                            correlationMatrix.Add(words);
                        }

                        Logger.Log("Finished document number: " + (i + 1).ToString());
                    }
                }
            }

            string filename = "autorss_" + Guid.NewGuid().ToString();
            using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, correlationMatrix);
            }

            Logger.Log("Saved to file: " + filename);

            filename = "autorss_Scopes_" + Guid.NewGuid().ToString();
            using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
            {
                MonitoredScope.SerializeStatistics(fs);
            }

            Logger.Log("Saved to file: " + filename);
        }
    }
}
