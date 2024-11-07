using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Backend.Models
{
    /// <summary>
    /// Links visualization requests to experiments.
    /// </summary>
    public class VisualizationExperiment
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int VisualizationRequestID { get; set; } // FK to DataVisualization

        [Required]
        public Guid ExperimentID { get; set; } // FK to ExperimentRequest

        // Navigation properties
        [ForeignKey("VisualizationRequestID")]
        public DataVisualizationModel DataVisualization { get; set; }

        [ForeignKey("ExperimentID")]
        public ExperimentRequest ExperimentRequest { get; set; }
    }
}