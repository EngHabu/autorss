using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Labeling.GoogleNews
{
    [DataContract]
    public class NewsResponse
    {
        [DataMember(Name = "responseData")]
        public ResponseData ResponseData { get; set; }
    }

    [DataContract]
    public class ResponseData
    {
        [DataMember(Name = "results")]
        public NewsResult[] Results { get; set; }
    }

    [DataContract]
    public class NewsResult
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }
        [DataMember(Name = "content")]
        public string Content { get; set; }
        [DataMember(Name = "unescapedUrl")]
        public string UnescapedUrl { get; set; }
        [DataMember(Name = "relatedStories")]
        public NewsResult[] RelatedStories { get; set; }
    }

    public static class NewsResponseParser
    {
        public static NewsResponse Parse(string newsResponse)
        {
            NewsResponse result = null;
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(NewsResponse));
            using (MemoryStream mem = new MemoryStream(Encoding.UTF8.GetBytes(newsResponse)))
            {
                result = serializer.ReadObject(mem) as NewsResponse;
            }

            return result;
        }
    }
}
