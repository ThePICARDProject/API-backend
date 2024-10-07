using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace API_backend.Models
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
        public string ClassName { get; set; }
        public string RelativeJarPath { get; set; }

        public string DatasetBasePath { get; set; }
        
        // Docker Arguments
        public List<int> NodeCount { get; set; }

        // Spark Arguments
        public string DriverMemory { get; set; }
        public string DriverCores { get; set; }
        public string ExecutorNumber { get; set; }
        public string ExecutorMemory { get; set; }
        public int ExecuterCores { get; set; }
        public int MemoryOverhead { get; set; }

        // Required Algorithm Arguments
        Dictionary<int, object> arguments = new Dictionary<int, object>(); // Map of arguments, Position and value

        // File Paths
        public string HdfsOutputDirectory { get; set; } // Location in Hdfs where results are output to
        public string LocalOutputDirectory { get; set; } // Location in local file system where results should be stored
    }
}
