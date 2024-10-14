using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Backend.Models
{
    /// <summary>
    /// Stores parameter values for each experiment.
    /// </summary>
    public class ExperimentAlgorithmParameterValue
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public string ExperimentID { get; set; } // FK to ExperimentRequest

        [Required]
        public int ParameterID { get; set; } // FK to AlgorithmParameter

        public string Value { get; set; }

        // Navigation properties
        [ForeignKey("ExperimentID")]
        public ExperimentRequest ExperimentRequest { get; set; }

        [ForeignKey("ParameterID")]
        public AlgorithmParameter AlgorithmParameter { get; set; }
    }
}