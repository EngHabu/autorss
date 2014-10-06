using AutoRSS.Library;
using AutoRSS.Library.Correlation;
using AutoRSS.Library.WordProcessor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using SStemmer = AutoRSS.Library.WordProcessor.PorterStemmer;

namespace AutoRSS.Labeling.Training
{
    internal class GoogleNewsTrainer : TrainerBase
    {
        public override IEnumerable<string> Filter(ProgramArguments programArgs)
        {
            List<Document[]> result = new List<Document[]>();
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

                            yield return fileContents;
                        }
                    }
                }
            }
        }
    }
}
