using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Library
{
    public static class Logger
    {
        public static void Log(string message)
        {
            Console.WriteLine(message);
        }

        public static void Log(string message, TraceLevel traceLevel)
        {
            if (traceLevel > TraceLevel.Verbose)
            {
                Log(message);
            }
        }
    }

    public enum TraceLevel
    {
        Verbose,
        Medium
    }
}
