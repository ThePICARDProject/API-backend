using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace API_backend.Models
{
    /// <summary>
    /// All experiment parameters for submit.
    /// 
    /// Optional args must be submitted in the order specified by the driver
    /// 
    /// HDFS path must be the path of the output inside the HDFS including the output
    /// file name, even if it is defined in the driver itself.
    /// 
    /// Example: if the argument for an algorithm output path is "output" but the 
    /// actual path is constructed in the driver to be /data/results/palfa/output,
    /// the full path must be provided.
    /// 
    /// </summary>
    public class ExperimentParameters
    {
        // User Data
        public string UserId { get; set; }
        public string ClassName { get; set; }
        public string RelativeJarPath { get; set; }
        
        // Submit Arguments
        public int Trials { get; set; }

        // Docker Arguments
        public List<int> NodeCounts { get; set; }

        // Spark Arguments
        public string DriverMemory { get; set; }
        //public string DriverCores { get; set; }
        
        //public string ExecutorNumber { get; set; }
        public string ExecutorMemory { get; set; }
        public int ExecuterCores { get; set; }
        public int MemoryOverhead { get; set; }

        // Required Algorithm Arguments
        public int NumberOfClasses { get; set; }
        public int NumberOfTrees { get; set; }
        public string Impurity {  get; set; }
        public int MaxDepth { get; set; }
        public int MaxBins { get; set; }
        public string DatasetPath { get; set; }
        public int PercentLabeled { get; set; }

        public string HdfsOutputDirectory { get; set; } // Location in Hdfs where results are output to
        public string LocalOutputDirectory { get; set; } // Location in local file system where results should be stored

        // Optional Algorithm Arguments
        public List<string> args { get; set; }
    }
}
