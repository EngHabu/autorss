using AutoRSS.Library.Correlation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Library.Serialization
{
    public class CorrelationMatrixBinarySerializer : ISerializer<CorrelationMatrix>
    {
        public CorrelationMatrix Deserialize(System.IO.Stream serializationStream)
        {
            CorrelationMatrix result = new CorrelationMatrix();
            using (BinaryReader bw = new BinaryReader(serializationStream))
            {
                string stringKey;
                int intValue;
                int intValue2;
                double doubleValue;
                double doubleKey;
                intValue = bw.ReadInt32();
                intValue2 = bw.ReadInt32();
                try
                {   //checking for 1GB of memory availability  
                    using (MemoryFailPoint memoryFailPoint = new MemoryFailPoint(4000))
                    {
                        result = new CorrelationMatrix(intValue, intValue2);
                        //perform memory consuming operation  
                    }
                }
                catch (InsufficientMemoryException exception)
                {
                    Logger.Log("Not sufficient memory. ex:" + exception);
                    //This happens when there is not enough memory  
                }  

                while ((stringKey = bw.ReadString()) != "0000")
                {
                    intValue = bw.ReadInt32();
                    intValue2 = bw.ReadInt32();
                    result.WordsMetadata[stringKey] = new WordMetadata { Index = intValue, Occurances = intValue2 };
                }

                while ((doubleKey = bw.ReadDouble()) != -1)
                {
                    doubleValue = bw.ReadDouble();
                    result.Matrix[doubleKey] = doubleValue;
                }
            }

            return result;
        }

        public void Serialize(System.IO.Stream serializationStream, CorrelationMatrix graph)
        {
            using (BinaryWriter bw = new BinaryWriter(serializationStream))
            {
                bw.Write(graph.WordsMetadata.Count);
                bw.Write(graph.Matrix.Count);
                foreach (KeyValuePair<string, WordMetadata> wordIndex in graph.WordsMetadata)
                {
                    bw.Write(wordIndex.Key);
                    bw.Write(wordIndex.Value.Index);
                    bw.Write(wordIndex.Value.Occurances);
                }

                bw.Write("0000");

                foreach (KeyValuePair<double, double> correlation in graph.Matrix)
                {
                    bw.Write(correlation.Key);
                    bw.Write(correlation.Value);
                }

                bw.Write((double)-1);
            }
        }
    }
}
