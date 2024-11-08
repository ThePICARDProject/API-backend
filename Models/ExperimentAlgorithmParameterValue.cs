using API_backend.Models;
using Microsoft.EntityFrameworkCore;
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

        [ForeignKey("ExperimentRequest")]
        public Guid ExperimentID { get; set; } // FK to ExperimentRequest

        public int ParameterID { get; set; } // FK to AlgorithmParameter

        public string Value { get; set; }

        public AlgorithmParameter AlgorithmParameter { get; set; }
    }
}