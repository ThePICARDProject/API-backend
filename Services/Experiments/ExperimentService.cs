using API_backend.Models;
using API_backend.Services.FileProcessing;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xml;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace API_backend.Services.Experiments
{
    /// <summary>
    /// Service for intiating experiments using Docker-Swarm.
    /// </summary>
    /// /// <remarks>
    /// Implemented based off of bash scripts provided in the docker-swarm repository.
    /// </remarks>
    /// <seealso href="https://github.com/ThePICARDProject/docker-swarm/"/>
    /// <seealso href="https://hadoop.apache.org/docs/r2.4.1/hadoop-project-dist/hadoop-hdfs/hdfs-default.xml"/>
    public class ExperimentService
    {
        public ExperimentService(ExperimentOptions options, string scriptPath) {}

        /// <summary>
        /// Adds containers to Docker-Swarm and configures Hadoop.
        ///
        /// Executes an experiment consisting of the number of trials defined in SubmitExperiment
        /// for each nodeCount in the list.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Error returned from the process</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public async Task<string> SubmitExperiment(ExperimentParameters data)
        {
            // Create submit process
            string error;
            using (Process submit = new Process())
            {
                // Setup Process
                submit.StartInfo.FileName = "./scripts/submit-experiment.sh";

                Collection<string> arguments = new Collection<string>();

                arguments.Add(data.DatasetBasePath);
                arguments.Add(data.DatasetName);

                // Add Node Counts
                arguments.Add(data.NodeCount.ToString());

                // Add Spark arguments
                arguments.Add(data.DriverMemory);
                arguments.Add(data.DriverCores);
                arguments.Add(data.ExecutorNumber);
                arguments.Add(data.ExecuterCores.ToString());
                arguments.Add(data.ExecutorMemory);
                arguments.Add(data.MemoryOverhead.ToString());

                arguments.Add(data.ClassName);
                arguments.Add(data.JarName);
               
                // Add output paths
                arguments.Add(data.HdfsOutputDirectory.ToString());
                arguments.Add(data.LocalOutputDirectory.ToString());
                arguments.Add(data.OutputFileName);
             
                submit.StartInfo.CreateNoWindow = true;

                // Start and wait for error
                submit.Start();
                error = await submit.StandardError.ReadToEndAsync();
                await submit.WaitForExitAsync();
            }
            return error;
        }
    
    }
}
