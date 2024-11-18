using System.ComponentModel.DataAnnotations;

namespace API_Backend.Models
{
    public class AggregatedResult
    {

        [Key]
        public int AggregatedResultID { get; set; }

        public string AggregatedResultName { get; set; }

        public string AggregatedResultDescription { get; set; }

        public string AggregatedResultFilePath { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

    }
}
