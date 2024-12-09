using Org.BouncyCastle.Asn1.Esf;

namespace API_backend.Models
{
    public class QueryExperiment
    {
        public List<QueryClusterParameters> ClusterParameters { get; set; }
        public List<QueryAlgorithmParameters> AlgorithmParameters { get; set; }
    }

    public class QueryClusterParameters
    {
        public string ClusterParameterName { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }
    
    public class QueryAlgorithmParameters
    {
        public int AlgorithmParameterId { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }
}
