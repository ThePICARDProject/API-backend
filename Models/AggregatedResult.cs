using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Backend.Models
{
    public class AggregatedResult
    {

        [Key]
        public int AggregatedResultID { get; set; }

        // Foreign Key to Users table
        [Required]
        public string UserID { get; set; }

        public string? AggregatedResultName { get; set; }

        public string? AggregatedResultDescription { get; set; }

        [Required]
        public string AggregatedResultFilePath { get; set; }

        public ICollection<ExperimentRequest> ExperimentRequests { get; } = new List<ExperimentRequest>();

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User User { get; set; } 

    }
}
