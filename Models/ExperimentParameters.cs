using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace API_Backend.Models
{
    /// <summary>
    /// All experiment parameters for submit.
    /// 
    /// Parameter order and value will be provided by a front end configuration.
    /// 
    /// </summary>
    public class ExperimentParameters
    {
        // User Data
        public string UserId { get; set; }
        public string ExperimentId { get; set; }
        public string ClassName { get; set; }

        public string DatasetName { get; set; }
        
        // Docker Arguments
        public int NodeCount { get; set; }

        // Spark Arguments
        public string DriverMemory { get; set; }
        public int DriverCores { get; set; }
        public int ExecutorNumber { get; set; }
        public int ExecuterCores { get; set; }
        public string ExecutorMemory { get; set; }
        public int MemoryOverhead { get; set; }

        public string JarName { get; set; }

        // Required Algorithm Arguments
        public List<string> Arguments = new List<string>(); // Map of arguments, Position and value

        // File Paths
        public string HdfsOutputDirectory { get; set; } // Location in Hdfs where results are output to
    }
}
