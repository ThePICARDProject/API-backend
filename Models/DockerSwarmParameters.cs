using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Backend.Models
{
    /// <summary>
    /// Represents Docker Swarm parameters for an experiment.
    /// </summary>
    public class DockerSwarmParameters
    {
        [Key, ForeignKey("ExperimentRequest")]
        public string ExperimentID { get; set; } // FK and PK to ExperimentRequest

        public string DriverMemory { get; set; }
        public string ExecutorMemory { get; set; }
        public int? Cores { get; set; }
        public int? Nodes { get; set; }
        public string MemoryOverhead { get; set; }

        // Navigation property
        public ExperimentRequest ExperimentRequest { get; set; }
    }
}