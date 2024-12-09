using Org.BouncyCastle.Asn1.Esf;

namespace API_backend.Models
{
    public class QueryExperiment : IEquatable<QueryExperiment>
    {
        public List<QueryClusterParameters> ClusterParameters { get; set; }
        public List<QueryAlgorithmParameters> AlgorithmParameters { get; set; }

        public bool Equals(QueryExperiment other)
        {
            if (ClusterParameters.Count != other.ClusterParameters.Count)
                return false;
            foreach(QueryClusterParameters param in ClusterParameters)
                if(!param.Equals(other))
                    return false;

            if (AlgorithmParameters.Count != other.AlgorithmParameters.Count)
                return false;
            foreach (QueryAlgorithmParameters param in AlgorithmParameters)
                if (!param.Equals(other))
                    return false;
            return true;
        }
    }

    public class QueryClusterParameters : IEquatable<QueryClusterParameters>
    {
        public string ClusterParameterName { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }

        public bool Equals(QueryClusterParameters other)
        {
            if(other is null) return false;
            return (this.ClusterParameterName == other.ClusterParameterName 
                && this.Operator == other.Operator 
                && this.Value == other.Value);
        }
    }
    
    public class QueryAlgorithmParameters : IEquatable<QueryAlgorithmParameters>
    {
        public int AlgorithmParameterId { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }

        public bool Equals(QueryAlgorithmParameters other)
        {
            if(other is null) return false;
            return (this.AlgorithmParameterId == other.AlgorithmParameterId
                && this.Operator == other.Operator
                && this.Value == other.Value);
        }
    }
}
