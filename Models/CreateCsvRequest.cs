using System.Security.Policy;

namespace API_backend.Models
{
    public class CreateCsvRequest
    {
        public int AggregateDataId { get; set; }
        public List<string> MetricsIdentifiers {  get; set; }
    }
}
