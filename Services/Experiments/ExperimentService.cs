using API_backend.Models;
using API_backend.Services.FileProcessing;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Unicode;
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
        private readonly string _dataBasePath = "./data";
        private readonly string _experimentOutputBasePath = "./results";

        public ExperimentService() {}

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
            // Generate specific experiment paths and submission DateTime
            string outputPath = Path.Combine(_experimentOutputBasePath, data.UserId, data.ExperimentId);
            string dataBasePath = Path.Combine(_dataBasePath, data.UserId);
            DateTime dateTime = DateTime.Now;
            string submissionDateTime = $"{dateTime.Year.ToString()}.{dateTime.Month.ToString()}.{dateTime.Day.ToString()}" +
                $"_{dateTime.Hour.ToString()}.{dateTime.Minute.ToString()}.{dateTime.Second.ToString()}";

            this.UpdateJarPath(data.UserId); // Update the Dockerfile

            // Create submit process
            string error;
            using (Process submit = new Process())
            {
                // Setup Process
                submit.StartInfo.FileName = "./scripts/submit-experiment.sh";
                submit.StartInfo.CreateNoWindow = false;

                Collection<string> arguments = new Collection<string>();

                arguments.Add($"{_dataBasePath}/{data.UserId}");
                arguments.Add(data.DatasetName);

                // Add Node Counts
                arguments.Add(data.NodeCount.ToString());

                // Add Spark arguments
                arguments.Add(data.DriverMemory);
                arguments.Add(data.DriverCores.ToString());
                arguments.Add(data.ExecutorNumber.ToString());
                arguments.Add(data.ExecuterCores.ToString());
                arguments.Add(data.ExecutorMemory);
                arguments.Add(data.MemoryOverhead.ToString());

                arguments.Add(data.ClassName);
                arguments.Add(data.JarName);
               
                // Add output paths
                arguments.Add(data.HdfsOutputDirectory.ToString());
                arguments.Add(outputPath);
                arguments.Add($"{data.ExperimentId}_{submissionDateTime}.txt");
             
                submit.StartInfo.CreateNoWindow = true;

                // Start and wait for error
                submit.Start();
                error = await submit.StandardError.ReadToEndAsync();
                await submit.WaitForExitAsync();
            }
            return error;
        }

        /// <summary>
        /// Updates the Dockerfile to reference Jar files for a specific userId.
        /// </summary>
        /// <param name="userId"></param>
        private void UpdateJarPath(string userId)
        {
            // Generate the path and the jar line regex pattern
            string dockerfilePath = "./docker-images/spark-hadoop/Dockerfile";
            if (!File.Exists(dockerfilePath))
                throw new FileNotFoundException("Dockerfile was not found");
            Regex linePattern = new Regex("COPY ./jars/[a-zA-Z0-9]+/* opt/jars");

            // Copy each line from the old dockerfile to a string
            string newFileContent = "";
            using(StreamReader dockerfile = new StreamReader(dockerfilePath))
            {
                while(!dockerfile.EndOfStream)
                {
                    string line = dockerfile.ReadLine();
                    Match match = linePattern.Match(line);
                    if(match.Length == line.Length) // If the pattern matches the line, update the line
                        line = $"COPY ./jars/{userId}/* opt/jars";
                    newFileContent = $"{newFileContent}{line}\n";
                }
            }

            // Write all bytes in the new file content to a new Dockerfile
            File.WriteAllBytes(dockerfilePath, Encoding.UTF8.GetBytes(newFileContent));
        }
    }
}
