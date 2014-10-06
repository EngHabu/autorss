using AutoRSS.Library;
using AutoRSS.Library.Correlation;
using AutoRSS.Library.Serialization;
using AutoRSS.Library.WordProcessor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SStemmer = AutoRSS.Library.WordProcessor.PorterStemmer;

namespace AutoRSS.Labeling.Training
{
    internal abstract class TrainerBase : ITrainer
    {
        public abstract IEnumerable<string> Filter(ProgramArguments programArgs);
        
        public CorrelationMatrix CalculateCorrelationMatrix(IEnumerable<string> documents)
        {
            CorrelationMatrix correlationMatrix = new CorrelationMatrix();
            return UpdateCorrelationMatrix(correlationMatrix, documents);
        }

        public CorrelationMatrix UpdateCorrelationMatrix(CorrelationMatrix existingMatrix, IEnumerable<string> documents)
        {
            WordBreaker wordBreaker = new WordBreaker();
            StopWordRemover stopwordRemover = new StopWordRemover();
            SentenceBreaker sb = SentenceBreaker.Instance;

            int i = 1;
            try
            {
                Parallel.ForEach(documents, (documentContents, loopState) => //string documentContents in documents)
                {
                    int documentNumber = Interlocked.Increment(ref i);
                    using (new MonitoredScope("Learning from a document No. " + documentNumber.ToString()))
                    {
                        SStemmer stemmer = new SStemmer();
                        string[] words;
                        //using (MonitoredScope scope = new MonitoredScope("Break Paragraph", TraceLevel.Medium))
                        {
                            words = sb.BreakIntoWords(documentContents);
                        }

                        //using (MonitoredScope scope = new MonitoredScope("Stem Words", TraceLevel.Medium))
                        {
                            words = stemmer.StemWords(words);
                        }

                        //using (MonitoredScope scope = new MonitoredScope("Remove Stop Words", TraceLevel.Medium))
                        {
                            words = stopwordRemover.RemoveStopWords(words);
                        }

                        //using (MonitoredScope scope = new MonitoredScope("Calculate correlation", TraceLevel.Medium))
                        {
                            existingMatrix.Add(words);
                        }
                    }

                    Logger.Log("Finished document number: " + documentNumber.ToString());
                    if (existingMatrix.Words.Count > 100000)
                    {
                        loopState.Break();
                    }
                    //Logger.Log("Finished document number: " + (i++).ToString() + " unique words: " + correlationMatrix.Words.Count + ", pairs: " + correlationMatrix.Matrix.Count);
                });
            }
            finally
            {
                Logger.Log("Unique words: " + existingMatrix.WordsMetadata.Count + ", Pairs: " + existingMatrix.Matrix.Count);
                string filename = "autorss_" + Guid.NewGuid().ToString();
                using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                {
                    new CorrelationMatrixBinarySerializer().Serialize(fs, existingMatrix);
                }

                Logger.Log("Correlation Matrix saved to file: " + filename);

                filename = "autorss_Scopes_" + Guid.NewGuid().ToString();
                using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                {
                    MonitoredScope.SerializeStatistics(fs);
                }

                Logger.Log("MonitoredScopes saved to file: " + filename);
            }

            return existingMatrix;
        }
    }
}
