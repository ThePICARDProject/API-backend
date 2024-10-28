using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Backend.Models
{
    /// <summary>
    /// Represents Docker Swarm parameters for an experiment.
    /// </summary>
    public class ClusterParameters
    {

        [Key]
        public int ClusterParamID { get; set; }
        [ForeignKey("ExperimentRequest")]
        public string ExperimentID { get; set; } // FK and PK to ExperimentRequest

        // Docker Parameters
        public int NodeCount { get; set; }

        // Spark Parameters
        public string DriverMemory { get; set; }
        public int DriverCores { get; set; }
        public int ExecutorNumber { get; set; }
        public int ExecutorCores { get; set; }
        public string ExecutorMemory { get; set; }
        public int MemoryOverhead { get; set; }

        // Navigation property
        public ExperimentRequest ExperimentRequest { get; set; }
    }
}