using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace API_backend.Models
{
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
        public string DriverCores { get; set; }
        
        public string ExecutorNumber { get; set; }
        public string ExecutorMemory { get; set; }
        public string ExecuterCores { get; set; }
        public string MemoryOverhead { get; set; }

        // Required Algorithm Arguments
        public int NumberOfClasses { get; set; }
        public int NumberOfTrees { get; set; }
        public string Impurity {  get; set; }
        public int MaxDepth { get; set; }
        public int MaxBins { get; set; }
        public string DatasetName { get; set; }
        public string OutputName { get; set; }
        public int PercentLabeled { get; set; }

        // Optional Algorithm Arguments
        public List<string> args { get; set; }
    }
}
