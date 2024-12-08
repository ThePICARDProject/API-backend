namespace API_Backend.Models
{
    public class QueryExperiment
    {
        public List<string> ClusterParameters { get; set; }

        public List<QueryAlgorithmParameter> AlgorithmParameters { get; set; }
    }

    public class QueryAlgorithmParameter
    {
        public string AlgorithmParameterId { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }
}
