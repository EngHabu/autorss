using edu.stanford.nlp.dcoref;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.pipeline;
using edu.stanford.nlp.semgraph;
using edu.stanford.nlp.trees;
using edu.stanford.nlp.util;
using java.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Library.WordProcessor
{
    public class SentenceBreaker
    {
        private StanfordCoreNLP _pipline;
        private WordBreaker _wordBreaker;
        private static SentenceBreaker _instance;
        private static readonly object s_SyncObject = new object();
        private SentenceBreaker()
        {
            // creates a StanfordCoreNLP object, with POS tagging, lemmatization, NER, parsing, and coreference resolution 
            Properties props = new Properties();
            //props.put("annotators", "tokenize, ssplit, pos, lemma, ner, parse, dcoref");
            props.put("annotators", "tokenize, ssplit, pos, lemma");
            //System.Environment.CurrentDirectory = @"C:\Users\haabu\Documents\Visual Studio 2012\Projects\AutoRSS\AutoRSS.Library\bin\Debug";
            _pipline = new StanfordCoreNLP(props);

            _wordBreaker = new WordBreaker(new char[] { '.' });
        }

        public static SentenceBreaker Instance
        {
            get
            {
                if (null == _instance)
                {
                    lock(s_SyncObject)
                    {
                        if (null == _instance)
                        {
                            _instance = new SentenceBreaker();
                        }
                    }
                }

                return _instance;
            }
        }

        public string[] BreakIntoWords(string paragraph)
        {
            // create an empty Annotation just with the given text
            Annotation document = new Annotation(paragraph);

            // run all Annotators on this text
            _pipline.annotate(document);

            // these are all the sentences in this document
            // a CoreMap is essentially a Map that uses class objects as keys and has values with custom types
            object obj = document.get(new CoreAnnotations.SentencesAnnotation().getClass());
            ArrayList sentences = obj as ArrayList;
            List<string> words = new List<string>(sentences.size() * 10); // Guess how many words per statement
            int i = 0;
            foreach (CoreMap sentence in sentences)
            {
                words.AddRange(_wordBreaker.BreakParagraph(sentence.ToString()));
            }

            return words.ToArray();
        }

        public string[] BreakIntoSentences(string paragraph)
        {
            // create an empty Annotation just with the given text
            Annotation document = new Annotation(paragraph);

            // run all Annotators on this text
            _pipline.annotate(document);

            // these are all the sentences in this document
            // a CoreMap is essentially a Map that uses class objects as keys and has values with custom types
            ArrayList sentences = document.get(new CoreAnnotations.SentencesAnnotation().getClass()) as ArrayList;
            string[] statements = new string[sentences.size()];
            int i = 0;
            foreach (CoreMap sentence in sentences)
            {
                statements[i++] = sentence.ToString();
            }

            return statements;

            //foreach (CoreMap sentence in sentences)
            //{
            //    // traversing the words in the current sentence
            //    // a CoreLabel is a CoreMap with additional token-specific methods
            //    foreach (CoreLabel token in sentence.get(new CoreAnnotations.TokensAnnotation().getClass()) as System.Collections.IEnumerable)
            //    {
            //        // this is the text of the token
            //        String word = token.get(new CoreAnnotations.TextAnnotation().getClass()) as string;
            //        // this is the POS tag of the token
            //        String pos = token.get(new CoreAnnotations.PartOfSpeechAnnotation().getClass()) as string;
            //        // this is the NER label of the token
            //        String ne = token.get(new CoreAnnotations.NamedEntityTagAnnotation().getClass()) as string;
            //    }

            //    // this is the parse tree of the current sentence
            //    Tree tree = sentence.get(new TreeCoreAnnotations.TreeAnnotation().getClass()) as Tree;

            //    // this is the Stanford dependency graph of the current sentence
            //    SemanticGraph dependencies = sentence.get(new SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation().getClass()) as SemanticGraph;
            //}

            //// This is the coreference link graph
            //// Each chain stores a set of mentions that link to each other,
            //// along with a method for getting the most representative mention
            //// Both sentence and token offsets start at 1!
            //Dictionary<int, CorefChain> graph =
            //  document.get(new CorefCoreAnnotations.CorefChainAnnotation().getClass()) as Dictionary<int, CorefChain>;

            //return graph;
        }
    }
}
