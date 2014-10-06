using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Library.Serialization
{
    internal class MonitoredScopeStatisticsBinarySerializer : ISerializer<ConcurrentDictionary<string, MonitoredScopeMetadata>>
    {
        public ConcurrentDictionary<string, MonitoredScopeMetadata> Deserialize(System.IO.Stream inputStream)
        {
            ConcurrentDictionary<string, MonitoredScopeMetadata> result = new ConcurrentDictionary<string, MonitoredScopeMetadata>();
            using (BinaryReader br = new BinaryReader(inputStream))
            {
                string key = br.ReadString();
                double totalMilliseconds = br.ReadDouble();
                double hitCount = br.ReadDouble();
                result[key] = new MonitoredScopeMetadata
                {
                    TotalMilliseconds = totalMilliseconds,
                    HitCount = hitCount
                };
            }

            return result;
        }

        public void Serialize(System.IO.Stream outputStream, ConcurrentDictionary<string, MonitoredScopeMetadata> graph)
        {
            using (BinaryWriter bw = new BinaryWriter(outputStream))
            {
                foreach(KeyValuePair<string, MonitoredScopeMetadata> item in graph)
                {
                    bw.Write(item.Key);
                    bw.Write(item.Value.TotalMilliseconds);
                    bw.Write(item.Value.HitCount);
                }
            }
        }
    }
}
