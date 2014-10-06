using System;
using System.IO;
using System.Xml;

namespace AutoRSS.Mapper
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlReader reader = null;
            if (args.Length > 0)
            {
                reader = XmlReader.Create(args[0]);
            }

            //string line;
            //string[] words;

            bool elementFound = false;
            while (elementFound = reader.ReadToFollowing("text"))
            {
                string pageContents;
                reader.ReadStartElement();
                pageContents = reader.ReadContentAsString();
                Console.WriteLine(pageContents.ToLowerInvariant());
            }
            //while ((line = Console.ReadLine()) != null)
            //{
            //    words = line.Split(' ');

            //    foreach (string word in words)
            //        Console.WriteLine(word.ToLower());
            //}
        }
    }
}
