using AutoRSS.Labeling.GoogleNews;
using AutoRSS.Labeling.Readability;
using AutoRSS.Labeling.Training;
using AutoRSS.Library;
using AutoRSS.Library.Correlation;
using AutoRSS.Library.Serialization;
using AutoRSS.Library.WordProcessor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using SStemmer = AutoRSS.Library.WordProcessor.PorterStemmer;

namespace AutoRSS.Labeling
{
    class Program
    {
        static void Main(string[] args)
        {
            ProgramArguments programArgs = ProgramArguments.Create(args);
            if (programArgs.HasMode(ToolMode.DownloadGoogleNews))
            {
                DownloadNews();
            }

            if (programArgs.HasMode(ToolMode.TrainUsingGoogleNews))
            {
                TrainUsingGoogleNews(programArgs);
            }

            if (programArgs.HasMode(ToolMode.TrainUsingWikipediaDB))
            {
                TrainUsingWikipediaDB(programArgs);
            }

            if (programArgs.HasMode(ToolMode.Label))
            {
                List<Tuple<Statement, Statement>> pairs = DrawBiasedSample(programArgs.LabelSampleSize);
                OutputThresholdReport(pairs, programArgs);
            }

            if (programArgs.HasMode(ToolMode.Categorize))
            {
                IEnumerable<DocumentClusterErrorScore> errorScores = CategorizeLabeledNewsArticles(programArgs);

                double average = (from score in errorScores select score.Value).Average();
                Console.WriteLine("Average Error: " + average);
            }

            if (programArgs.HasMode(ToolMode.Optimize))
            {
                FindBestThresholds(programArgs);
            }

            if (programArgs.HasMode(ToolMode.OutputAvgErrors))
            {
                ExperimentPandVThresholds(programArgs);
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to exit");
                Console.ReadLine();
            }
        }

        private static CorrelationMatrix TrainUsingWikipediaDB(ProgramArguments programArgs)
        {
            ITrainer trainer = new WikipediaTrainer();
            IEnumerable<string> documents = trainer.Filter(programArgs);
            CorrelationMatrix matrix;
            if (string.IsNullOrWhiteSpace(programArgs.WikipediaDBPath))
            {
                matrix = trainer.CalculateCorrelationMatrix(documents);
            }
            else
            {
                matrix = LoadCorrelationMatrix(programArgs);
                matrix = trainer.UpdateCorrelationMatrix(matrix, documents);
            }

            return matrix;
        }
        private static void FindBestThresholds(ProgramArguments programArgs)
        {
            IEnumerable<DocumentCluster> originalClusters = GetSimilarNewsTopicFiles();
            IEnumerable<Document> documents = Flatten(originalClusters);
            CorrelationMatrix correlationMatrix = LoadCorrelationMatrix(programArgs);

            string fileName = Guid.NewGuid().ToString() + ".csv";
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                double errorGoal = 0.01;
                SortedDictionary<double, SortedDictionary<double, List<double>>> errorValues = new SortedDictionary<double, SortedDictionary<double, List<double>>>();
                ErrorOptimizer.Optimize(0, 1, 0, 1, (permissibleValue, variationValue) =>
                {
                    SimilarityAlgorithm similarityAlgorithm = new SimilarityAlgorithm(correlationMatrix, permissibleValue, variationValue);
                    DocumentCategorizer categorizer = new DocumentCategorizer(similarityAlgorithm);
                    IEnumerable<DocumentCluster> resultClusters = categorizer.Cluster(documents);
                    IEnumerable<DocumentClusterErrorScore> errorScores = CalculateErrorScore(originalClusters, resultClusters);

                    double average = (from score in errorScores select score.Value).Average();
                    Console.WriteLine("Average Error: " + average);

                    sw.WriteLine("{0}, {1}, {2}", permissibleValue, variationValue, average);

                    return Math.Abs(average) <= errorGoal;
                });
            }
        }

        private static void ExperimentPandVThresholds(ProgramArguments programArgs)
        {
            IEnumerable<DocumentCluster> originalClusters = GetSimilarNewsTopicFiles();
            IEnumerable<Document> documents = Flatten(originalClusters);
            CorrelationMatrix correlationMatrix = LoadCorrelationMatrix(programArgs);

            double startP = 0,
                endP = 1,
                startV = 0,
                endV = 1;
            double step = 0.01;
            double[,] errorValues = new double[(int)((endP - startP) / step) + 1, (int)((endV - startV) / step) + 1];
            for (double i = 0, iP = startP; iP < endP; iP += step, i++)
            {
                for (double j = 0, iV = startV; iV < endV; iV += step, j++)
                {
                    SimilarityAlgorithm similarityAlgorithm
                        = new SimilarityAlgorithm(
                            correlationMatrix,
                            iP,
                            iV);
                    DocumentCategorizer categorizer = new DocumentCategorizer(similarityAlgorithm);
                    IEnumerable<DocumentCluster> resultClusters = categorizer.Cluster(documents);
                    IEnumerable<DocumentClusterErrorScore> errorScores = CalculateErrorScore(resultClusters, originalClusters);

                    double average = (from score in errorScores select score.Value).Average();
                    Console.WriteLine("Average Error: " + average);

                    errorValues[(int)i, (int)j] = Math.Abs(average);
                }
            }

            string fileName = Guid.NewGuid().ToString() + ".csv";
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.Write("0, ");
                for (double j = 0, iV = startV; iV < endV; iV += step, j++)
                {
                    sw.Write("{0}, ", iV);
                }

                sw.WriteLine();

                for (double i = 0, iP = startP; iP < endP; iP += step, i++)
                {
                    sw.Write("{0}, ", iP);
                    for (double j = 0, iV = startV; iV < endV; iV += step, j++)
                    {
                        sw.Write("{0}, ", errorValues[(int)i, (int)j]);
                    }

                    sw.WriteLine();
                }
            }

            Logger.Log("Saved experiment to file: " + fileName);
        }

        private static IEnumerable<DocumentClusterErrorScore> CategorizeLabeledNewsArticles(ProgramArguments programArgs)
        {
            IEnumerable<DocumentCluster> originalClusters = GetSimilarNewsTopicFiles();
            IEnumerable<Document> documents = Flatten(originalClusters);
            CorrelationMatrix correlationMatrix = LoadCorrelationMatrix(programArgs);

            DocumentCategorizer categorizer = new DocumentCategorizer(correlationMatrix);
            IEnumerable<DocumentCluster> resultClusters = categorizer.Cluster(documents);
            OutputClusters(resultClusters);
            IEnumerable<DocumentClusterErrorScore> errorScores = CalculateErrorScore(originalClusters, resultClusters);
            return errorScores;
        }

        private static void AddErrorValue(
            SortedDictionary<double, SortedDictionary<double, List<double>>> errorValues,
            double permissibleValue,
            double variationValue,
            double average)
        {
            SortedDictionary<double, List<double>> variationErrorValues;
            if (!errorValues.TryGetValue(permissibleValue, out variationErrorValues))
            {
                errorValues[permissibleValue] = variationErrorValues = new SortedDictionary<double, List<double>>();
            }

            List<double> errors;
            if (!variationErrorValues.TryGetValue(variationValue, out errors))
            {
                variationErrorValues[variationValue] = errors = new List<double>();
            }

            errors.Add(average);
        }

        private static IEnumerable<DocumentClusterErrorScore> CalculateErrorScore(IEnumerable<DocumentCluster> resultClusters, IEnumerable<DocumentCluster> originalClusters)
        {
            List<DocumentClusterErrorScore> result = new List<DocumentClusterErrorScore>();
            List<DocumentCluster> resultClustersList = resultClusters.ToList();
            List<DocumentCluster> originalClustersList = originalClusters.ToList();

            for (int i = 0; i < originalClustersList.Count; i++)
            {
                DocumentCluster originalCluster = originalClustersList[i];
                int? resultClusterIndex = null;
                foreach (Document originalDocument in originalCluster)
                {
                    DocumentClusterErrorScore score = new DocumentClusterErrorScore();
                    int index = FindClusterIndex(resultClustersList, originalDocument);
                    if (index == -1)
                    {
                        throw new ArgumentException("Can't find doc");
                    }
                    else if (!resultClusterIndex.HasValue)
                    {
                        resultClusterIndex = index;
                    }
                    else if (index != resultClusterIndex.Value)
                    {
                        score.Value = -1;
                    }
                    else
                    {
                        score.Value = 1;
                    }

                    result.Add(score);
                }
            }

            return result.AsEnumerable();
        }

        private static int FindClusterIndex(List<DocumentCluster> clustersList, Document document)
        {
            int i = 0;
            foreach (DocumentCluster cluster in clustersList)
            {
                foreach (Document clusterDocument in cluster)
                {
                    if (clusterDocument.Equals(document))
                    {
                        return i;
                    }
                }

                i++;
            }

            return -1;
        }

        private static void OutputClusters(IEnumerable<DocumentCluster> clusters)
        {
            string folderName = Guid.NewGuid().ToString();
            Directory.CreateDirectory(folderName);
            foreach (DocumentCluster cluster in clusters)
            {
                string topicFolderName = Guid.NewGuid().ToString();
                Directory.CreateDirectory(folderName + "\\" + topicFolderName);
                int i = 0;
                foreach (Document doc in cluster.Documents)
                {
                    using (StreamWriter sw = new StreamWriter(folderName + "\\" + topicFolderName + "\\" + (i++).ToString() + ".txt"))
                    {
                        sw.Write(doc.ToString());
                    }
                }
            }
        }

        private static void TrainUsingGoogleNews(ProgramArguments programArgs)
        {
            ITrainer trainer = new GoogleNewsTrainer();
            IEnumerable<string> documents = trainer.Filter(programArgs);
            CorrelationMatrix matrix = trainer.CalculateCorrelationMatrix(documents);
        }

        private static void OutputThresholdReport(List<Tuple<Statement, Statement>> pairs, ProgramArguments programArgs)
        {
            CorrelationMatrix correlationMatrix = LoadCorrelationMatrix(programArgs);
            SimilarityAlgorithm sim = new SimilarityAlgorithm(correlationMatrix);
            StringBuilder sb = new StringBuilder();
            foreach (Tuple<Statement, Statement> pair in pairs)
            {
                Statement s1 = StemStatement(pair.Item1);
                Statement s2 = StemStatement(pair.Item2);

                double s12 = sim.StatementSimilarityToStatement(s1, s2);
                double s21 = sim.StatementSimilarityToStatement(s2, s1);
                bool areEqual = sim.StatementEqualsToStatement(s1, s2);

                sb.AppendFormat(
                    "{0},{1},{2},{2}\r\n",
                    pair.Item1.ToString().Replace(',', '.'),
                    pair.Item2.ToString().Replace(',', '.'),
                    Math.Min(s12, s21),
                    Math.Abs(s12 - s21));
            }

            string reportName = "autoRSS_thresholdReport_" + Guid.NewGuid().ToString() + ".csv";
            using (StreamWriter sw = new StreamWriter(reportName))
            {
                sw.WriteLine(sb.ToString());
            }

            Console.WriteLine("Report: " + reportName);
        }

        private static Statement StemStatement(Statement statement)
        {
            SStemmer stemmer = new SStemmer();
            WordBreaker wb = new WordBreaker();
            StopWordRemover stopWordRemover = new StopWordRemover();
            string[] wordsString = wb.BreakParagraph(statement.ToString());
            wordsString = stopWordRemover.RemoveStopWords(wordsString);
            wordsString = stemmer.StemWords(wordsString);
            List<Word> words = new List<Word>();
            foreach (string wordString in wordsString)
            {
                words.Add(new Word(wordString));
            }

            return new Statement(words.ToArray());
        }

        private static IEnumerable<Document> GetAllNewsTopicFiles()
        {
            return Flatten(GetSimilarNewsTopicFiles());
        }

        private static IEnumerable<T> Flatten<T>(IEnumerable<IEnumerable<T>> array)
        {
            List<T> result = new List<T>();
            foreach (IEnumerable<T> subArray in array)
            {
                result.AddRange(subArray);
            }

            return result.AsReadOnly();
        }

        private static IEnumerable<DocumentCluster> GetSimilarNewsTopicFiles()
        {
            List<DocumentCluster> result = new List<DocumentCluster>();
            foreach (string topicPath in Directory.GetDirectories("News", "*", SearchOption.TopDirectoryOnly))
            {
                List<Document> documents = new List<Document>();
                foreach (string dirPath in Directory.GetDirectories(topicPath, "*", SearchOption.TopDirectoryOnly))
                {
                    string[] files = Directory.GetFiles(dirPath, "*.html", SearchOption.TopDirectoryOnly);
                    if (null != files &&
                        1 < files.Length)
                    {
                        foreach (string path in files)
                        {
                            string fileContents = "";
                            using (StreamReader sr = new StreamReader(path))
                            {
                                fileContents = sr.ReadToEnd();
                            }

                            Document document = ConstructDocument(fileContents);
                            document.Id = path;
                            documents.Add(document);
                        }
                    }
                }

                result.Add(new DocumentCluster(documents));
            }

            return result.ToArray();
        }

        private static Document[] GetRamdonNewsTopicFiles()
        {
            List<Document> documents = new List<Document>();
            foreach (string dirPath in Directory.GetDirectories("News", "*", SearchOption.AllDirectories))
            {
                string[] files = Directory.GetFiles(dirPath, "*.html", SearchOption.TopDirectoryOnly);
                if (null != files &&
                    1 < files.Length)
                {
                    foreach (string path in files)
                    {
                        string fileContents = "";
                        using (StreamReader sr = new StreamReader(path))
                        {
                            fileContents = sr.ReadToEnd();
                        }

                        Document document = ConstructDocument(fileContents);
                        documents.Add(document);
                    }
                }
            }

            return documents.ToArray();
        }

        private static Document[] ScanNewsFiles()
        {
            List<Document> documents = new List<Document>();
            foreach (string path in Directory.GetFiles("News", "*.html", SearchOption.AllDirectories))
            {
                string fileContents = "";
                using (StreamReader sr = new StreamReader(path))
                {
                    fileContents = sr.ReadToEnd();
                }

                Document document = ConstructDocument(fileContents);
                documents.Add(document);
            }

            return documents.ToArray();
        }

        private static CorrelationMatrix LoadCorrelationMatrix(ProgramArguments programArgs)
        {
            if (!File.Exists(programArgs.WikipediaDBPath))
            {
                throw new ArgumentException("Invalid Path: " + programArgs.WikipediaDBPath, "programArgs");
            }

            Logger.Log("Loading Model DB from: " + programArgs.WikipediaDBPath);

            CorrelationMatrix correlationMatrix;
            using (FileStream fs = new FileStream(programArgs.WikipediaDBPath, FileMode.Open))
            {
                //BinaryFormatter formatter = new BinaryFormatter();
                correlationMatrix = new CorrelationMatrixBinarySerializer().Deserialize(fs);
                //correlationMatrix = formatter.Deserialize(fs) as CorrelationMatrix;
            }

            Logger.Log("Loaded Model DB [" + correlationMatrix.ToString() + "]");

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
                //wordsString = stopWordRemover.RemoveStopWords(wordsString);
                //wordsString = stemmer.StemWords(wordsString);
                List<Word> words = new List<Word>();
                foreach (string wordString in wordsString)
                {
                    words.Add(new Word(wordString));
                }

                statements.Add(new Statement(words.ToArray()));
            }

            return new Document(statements.ToArray());
        }

        private static List<Tuple<Statement, Statement>> DrawBiasedSample(int sampleSize)
        {
            List<Tuple<Statement, Statement>> result = new List<Tuple<Statement, Statement>>();
            IEnumerable<IEnumerable<Document>> documents = GetSimilarNewsTopicFiles();
            int remainingSamples = sampleSize;
            Random rand = new Random();
            while (remainingSamples > 0)
            {
                int set = rand.Next(0, documents.Count() - 1);

                Document[] docSet = documents.ElementAt(set).ToArray();
                int d1 = rand.Next(0, docSet.Length - 1);
                int d2 = rand.Next(0, docSet.Length - 1);

                Statement[] d1Statements = docSet[d1].Statements;
                Statement[] d2Statements = docSet[d2].Statements;

                if (d1Statements.Length > 0 && d2Statements.Length > 0)
                {
                    int s1 = 0;// rand.Next(0, d1Statements.Length - 1);
                    int s2 = 0;// rand.Next(0, d2Statements.Length - 1);

                    Tuple<Statement, Statement> pair = new Tuple<Statement, Statement>(d1Statements[s1], d2Statements[s2]);
                    result.Add(pair);
                    remainingSamples--;
                }
            }

            return result;
        }

        private static List<Tuple<Statement, Statement>> DrawRandomSample(int sampleSize)
        {
            List<Tuple<Statement, Statement>> result = new List<Tuple<Statement, Statement>>();
            Document[] documents = ScanNewsFiles();
            int remainingSamples = sampleSize;
            Random rand = new Random();
            while (remainingSamples > 0)
            {
                int d1 = rand.Next(0, documents.Length - 1);
                int d2 = rand.Next(0, documents.Length - 1);

                Statement[] d1Statements = documents[d1].Statements;
                Statement[] d2Statements = documents[d2].Statements;

                if (d1Statements.Length > 0 && d2Statements.Length > 0)
                {
                    int s1 = rand.Next(0, d1Statements.Length - 1);
                    int s2 = rand.Next(0, d2Statements.Length - 1);

                    Tuple<Statement, Statement> pair = new Tuple<Statement, Statement>(d1Statements[s1], d2Statements[s2]);
                    result.Add(pair);
                    remainingSamples--;
                }
            }

            return result;
        }

        private static void DownloadNews()
        {
            const string newsTopicsFile = @"News\topics.csv";
            using (StreamReader sr = new StreamReader(newsTopicsFile))
            {
                string line = "";
                while (null != (line = sr.ReadLine()))
                {
                    DownloadNewsPost(line);
                }
            }
        }

        private static void DownloadNewsPost(string topic)
        {
            string newsFolder = @"News\" + topic;
            const string newsApi = "https://ajax.googleapis.com/ajax/services/search/news?v=1.0&q=";
            string api = newsApi + topic;
            if (Directory.Exists(newsFolder))
            {
                Directory.Delete(newsFolder, recursive: true);
            }

            Directory.CreateDirectory(newsFolder);

            string data = "";
            using (WebClient client = new WebClient())
            {
                data = client.DownloadString(api);
            }

            int newsId = 0;
            NewsResponse news = NewsResponseParser.Parse(data);
            foreach (NewsResult newsResult in news.ResponseData.Results)
            {
                Logger.Log("Downloading news: " + newsResult.Title);
                int storyId = 0;
                string newsResultFolder = newsFolder + @"\" + newsId++;
                Directory.CreateDirectory(newsResultFolder);

                RetryAction(() => EnsureContent(newsResult));
                using (StreamWriter sw = new StreamWriter(newsResultFolder + @"\" + storyId++ + ".html"))
                {
                    sw.WriteLine(newsResult.Title + "." + newsResult.Content);
                }

                if (null != newsResult.RelatedStories)
                {
                    foreach (NewsResult relatedStory in newsResult.RelatedStories)
                    {
                        Logger.Log("Downloading news: " + relatedStory.Title);
                        RetryAction(() => EnsureContent(relatedStory));
                        using (StreamWriter sw = new StreamWriter(newsResultFolder + @"\" + storyId++ + ".html"))
                        {
                            sw.WriteLine(relatedStory.Content);
                        }
                    }
                }
            }
        }

        private static void RetryAction(Action action, int retryCount = 3, int retryWaitMilliseconds = 300, int retryWaitFactor = 1)
        {
            int waitTime = retryWaitMilliseconds;
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Logger.Log("Exception retrying action. Retry count: " + i.ToString() + ". Exception: " + ex.ToString());
                    Thread.Sleep(waitTime);
                    waitTime *= retryWaitFactor;
                }
            }
        }

        private static void EnsureContent(NewsResult newsResult)
        {
            const string readabilityAPI = "http://www.readability.com/api/content/v1/parser?url={0}&token=74d77e5e0e1adb82405c222e159cbd1b806975c1";

            using (WebClient client = new WebClient())
            {
                string readabilityResponse = client.DownloadString(string.Format(readabilityAPI, HttpUtility.UrlEncode(newsResult.UnescapedUrl)));
                ReadabilityResponse response = ReadabilityResponseParser.Parse(readabilityResponse);
                newsResult.Content = RemoveHtmlTags(response.Content);
            }
        }

        private static string RemoveHtmlTags(string htmlDocument)
        {
            Regex regexHtmlTag = new Regex("</?\\w+((\\s+\\w+(\\s*=\\s*(?:\".*?\\\"|.*?|[^\">\\s]+))?)+\\s*|\\s*)/?>");
            Regex regexEmptyLine = new Regex("\r|\n|\t");
            return regexEmptyLine.Replace(regexHtmlTag.Replace(htmlDocument, ""), "");
        }
    }
}
