using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Backend.Models
{
    public class CsvResult
    {


        [Key]
        public int CsvResultID { get; set; }

        [Required]
        public int AggregatedResultID { get; set; } // FK to ExperimentRequest

        public string CsvResultName { get; set; }

        public string CsvResultDescription { get; set; }

        public string CsvResultFilePath { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation property
        [ForeignKey("AggregatedResultID")]
        public AggregatedResult AggregatedResult { get; set; }
    }
}
