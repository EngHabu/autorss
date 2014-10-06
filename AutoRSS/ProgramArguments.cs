using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS
{
    internal class ProgramArguments
    {
        private int _wikipediaStartArticle = 0;
        private int _wikipediaEndArticle = 10000;
        private string _wikipediaDBPath = "";

        private ProgramArguments()
        {
        }

        public int WikipediaStartArticle
        {
            get { return _wikipediaStartArticle; }
            set { _wikipediaStartArticle = value; }
        }

        public int WikipediaEndArticle
        {
            get { return _wikipediaEndArticle; }
            set { _wikipediaEndArticle = value; }
        }

        public string WikipediaDBPath
        {
            get { return _wikipediaDBPath; }
            set { _wikipediaDBPath = value; }
        }

        public static ProgramArguments Create(string[] args)
        {
            ProgramArguments result = new ProgramArguments();
            foreach (string arg in args)
            {
                string lowerArg = arg.ToLowerInvariant();
                if (lowerArg.StartsWith("-" + ProgramArgumentsConstants.WikipediaStartArticle))
                {
                    int value;
                    if (int.TryParse(lowerArg.Substring(("-" + ProgramArgumentsConstants.WikipediaStartArticle).Length + 1), out value))
                    {
                        result.WikipediaStartArticle = value;
                    }
                }
                else if (lowerArg.StartsWith("-" + ProgramArgumentsConstants.WikipediaEndArticle))
                {
                    int value;
                    if (int.TryParse(lowerArg.Substring(("-" + ProgramArgumentsConstants.WikipediaEndArticle).Length + 1), out value))
                    {
                        result.WikipediaEndArticle = value;
                    }
                }
                else if (lowerArg.StartsWith("-" + ProgramArgumentsConstants.WikipediaDBPath))
                {
                    string value;
                    if (File.Exists((value = lowerArg.Substring(("-" + ProgramArgumentsConstants.WikipediaDBPath).Length + 1))))
                    {
                        result.WikipediaDBPath = value;
                    }
                }
            }

            return result;
        }
    }

    internal class ProgramArgumentsConstants
    {
        public const string WikipediaStartArticle = "wikipediastartatricle";
        public const string WikipediaEndArticle = "wikipediaendatricle";
        public const string WikipediaDBPath = "wikipediadbpath";
    }
}
