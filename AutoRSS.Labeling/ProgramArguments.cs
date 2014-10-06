using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Labeling
{
    internal class ProgramArguments
    {
        private static Dictionary<string, Action<string>> _arguments;

        public ToolMode Mode { get; private set; }
        public int LabelSampleSize { get; private set; }
        public string WikipediaDBPath { get; private set; }
        public int WikipediaStartArticle { get; private set; }
        public int WikipediaEndArticle { get; private set; }

        private Dictionary<string, Action<string>> ArgumentPredicates
        {
            get
            {
                if (null == _arguments)
                {
                    _arguments = new Dictionary<string, Action<string>> (new CaseInsinsitiveStringEqualityComparer())
                        {
                            {"mode", SetMode},
                            {"samplesize", SetSampleSize},
                            {"wikipediadbpath", SetWikipediaDBPath},
                            {"wikipediastartatricle", SetWikipediaStartArticle},
                            {"wikipediaendatricle", SetWikipediaEndArticle},
                        };
                }

                return _arguments;
            }
        }

        private ProgramArguments()
        {
        }

        private void SetNoThrow(string argName, string argValue)
        {
            Action<string> predicate;
            if (ArgumentPredicates.TryGetValue(argName, out predicate))
            {
                predicate(argValue);
            }
        }

        private void SetWikipediaStartArticle(string value)
        {
            int startArticle;
            if (int.TryParse(value, out startArticle))
            {
                WikipediaStartArticle = startArticle;
            }
        }

        private void SetWikipediaEndArticle(string value)
        {
            int endArticle;
            if (int.TryParse(value, out endArticle))
            {
                WikipediaEndArticle = endArticle;
            }
        }

        private void SetWikipediaDBPath(string value)
        {
            const string wikipediaBasePath = @"C:\Users\haabu\Documents\Visual Studio 2012\Projects\AutoRSS\Build\Debug";
            WikipediaDBPath = Path.Combine(wikipediaBasePath, value);
        }

        private void SetSampleSize(string value)
        {
            int sampleSize;
            if (int.TryParse(value, out sampleSize))
            {
                LabelSampleSize = sampleSize;
            }
        }

        private void SetMode(string value)
        {
            ToolMode mode;
            if (Enum.TryParse<ToolMode>(value, out mode))
            {
                Mode = mode;
            }
        }

        public static ProgramArguments Create(string[] args)
        {
            ProgramArguments result = new ProgramArguments();
            foreach (string arg in args)
            {
                string lowerArg = arg.ToLowerInvariant();
                lowerArg = lowerArg.TrimStart('-');
                string argName = lowerArg.Substring(0, lowerArg.IndexOf(':'));
                string argValue = arg.Substring(lowerArg.IndexOf(':') + 2);
                result.SetNoThrow(argName, argValue);
            }

            return result;
        }

        internal bool HasMode(ToolMode toolMode)
        {
            return (toolMode & Mode) == toolMode;
        }

        internal class CaseInsinsitiveStringEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return x.Equals(y, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                return obj.ToLowerInvariant().GetHashCode();
            }
        }
    }

    [Flags]
    public enum ToolMode
    {
        None = 0,
        DownloadGoogleNews = 1,
        TrainUsingGoogleNews = 2,
        TrainUsingWikipediaDB = 4,
        Label = 8,
        Categorize = 16,
        Optimize = 32,
        OutputAvgErrors = 64,
    }
}
