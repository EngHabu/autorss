using AutoRSS.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AutoRSS.Labeling.Training
{
    internal class WikipediaTrainer : TrainerBase
    {
        public override IEnumerable<string> Filter(ProgramArguments programArgs)
        {
            string wikipediaPath = @"C:\Users\haabu\Downloads\enwiki-latest-pages-articles.xml\enwiki-latest-pages-articles.xml";
            int totalArticlesToRead = programArgs.WikipediaEndArticle - programArgs.WikipediaStartArticle;
            using (XmlReader sr = XmlReader.Create(new FileStream(wikipediaPath, FileMode.Open)))
            {
                using (MonitoredScope scope = new MonitoredScope("Skipping wikipedia articles"))
                {
                    for (int i = 0; i < programArgs.WikipediaStartArticle; i++)
                    {
                        bool elementFound = sr.ReadToFollowing("text");
                        if (!elementFound)
                        {
                            break;
                        }
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

                        Logger.Log("Read article " + (i - programArgs.WikipediaStartArticle + 1).ToString() + "/" + totalArticlesToRead.ToString());

                        yield return pageContents;
                    }
                }
            }
        }
    }
}
