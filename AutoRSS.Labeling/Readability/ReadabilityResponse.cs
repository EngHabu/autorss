using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Labeling.Readability
{
    [DataContract]
    public class ReadabilityResponse
    {
        [DataMember(Name = "content")]
        public string Content { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "excerpt")]
        public string Excerpt { get; set; }
    }

    public static class ReadabilityResponseParser
    {
        public static ReadabilityResponse Parse(string response)
        {
            ReadabilityResponse result = null;
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ReadabilityResponse));
            using (MemoryStream mem = new MemoryStream(Encoding.UTF8.GetBytes(response)))
            {
                result = serializer.ReadObject(mem) as ReadabilityResponse;
            }

            return result;
        }
    }
}
