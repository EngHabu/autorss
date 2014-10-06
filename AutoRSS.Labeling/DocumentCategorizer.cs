using AutoRSS.Library.Correlation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Labeling
{
    public class DocumentCluster : IEnumerable<Document>
    {
        public List<Document> Documents { get; private set; }

        public DocumentCluster()
        {
            Documents = new List<Document>();
        }

        public DocumentCluster(IEnumerable<Document> documents)
        {
            this.Documents = new List<Document>(documents);
        }

        public void Add(Document document)
        {
            Documents.Add(document);
        }

        public IEnumerator<Document> GetEnumerator()
        {
            return Documents.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class DocumentCategorizer
    {
        private SimilarityAlgorithm _similarity;

        public DocumentCategorizer(CorrelationMatrix matrix)
        {
            _similarity = new SimilarityAlgorithm(matrix);
        }

        public DocumentCategorizer(SimilarityAlgorithm similarityAlgorithm)
        {
            _similarity = similarityAlgorithm;
        }

        public IEnumerable<DocumentCluster> Cluster(IEnumerable<Document> documents)
        {
            List<DocumentCluster> clusters = new List<DocumentCluster>();
            DocumentCluster cluster = new DocumentCluster();
            foreach (Document document in documents)
            {
                cluster = FindCluster(document, clusters);
                if (null == cluster)
                {
                    cluster = new DocumentCluster();
                    clusters.Add(cluster);
                }

                cluster.Add(document);
            }

            return clusters;
        }

        private DocumentCluster FindCluster(Document document, IEnumerable<DocumentCluster> clusters)
        {
            foreach (DocumentCluster cluster in clusters)
            {
                if (BelongsTo(document, cluster))
                {
                    return cluster;
                }
            }

            return null;
        }

        private bool BelongsTo(Document document, DocumentCluster cluster)
        {
            foreach (Document doc in cluster.Documents)
            {
                double oddsRatio = _similarity.CalculateOddsRatio(document, doc);
                if (oddsRatio >= 2)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }
    }
}
