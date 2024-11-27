using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;

namespace API_Backend.Models
{
    public class AggregatedResult
    {

        [Key]
        public int AggregatedResultID { get; set; }

        public string? AggregatedResultName { get; set; }

        public string? AggregatedResultDescription { get; set; }

        public string AggregatedResultFilePath { get; set; }

        public ICollection<ExperimentRequest> ExperimentRequests { get; } = new List<ExperimentRequest>();

        [Required]
        public DateTime CreatedAt { get; set; }

    }
}
