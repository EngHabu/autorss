using AutoRSS.Library.WordProcessor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateDictionary
{
    class Program
    {
        static void Main(string[] args)
        {
            string dictionariesPath = @"WordProcessor\Dictionary";
            foreach (string filePath in Directory.GetFiles(dictionariesPath))
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        StemmedDictionary.Instance.Add(line);
                    }
                }
            }

            StemmedDictionary.Instance.Serialize();
        }
    }
}
