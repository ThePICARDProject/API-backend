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
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Error returned from the process</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public async Task<string> SubmitExperiment(ExperimentParameters data)
        {
            // Generate experiment directory and create it if it does not exist
            string outputPath = Path.Combine(_experimentOutputBasePath, data.UserId, data.ExperimentId);
            string dataBasePath = Path.Combine(_dataBasePath, data.UserId);
            DateTime dateTime = DateTime.Now;
            string submissionDateTime = $"{dateTime.Year.ToString()}-{dateTime.Month.ToString()}-{dateTime.Day.ToString()}" +
                $"_{dateTime.Hour.ToString()}-{dateTime.Minute.ToString()}-{dateTime.Second.ToString()}";

            this.UpdateJarPath(data.UserId); // Update the Dockerfile

            // Create submit process
            string error;
            using (Process submit = new Process())
            {
                // Setup Process
                submit.StartInfo.FileName = "./scripts/submit-experiment.sh";
                submit.StartInfo.CreateNoWindow = false;

                submit.StartInfo.ArgumentList.Add($"{_dataBasePath}/{data.UserId}");
                submit.StartInfo.ArgumentList.Add(data.DatasetName);

                // Add Node Counts
                submit.StartInfo.ArgumentList.Add(data.NodeCount.ToString());

                // Add Spark arguments
                submit.StartInfo.ArgumentList.Add(data.DriverMemory);
                submit.StartInfo.ArgumentList.Add(data.DriverCores.ToString());
                submit.StartInfo.ArgumentList.Add(data.ExecutorNumber.ToString());
                submit.StartInfo.ArgumentList.Add(data.ExecuterCores.ToString());
                submit.StartInfo.ArgumentList.Add(data.ExecutorMemory);
                submit.StartInfo.ArgumentList.Add(data.MemoryOverhead.ToString());

                submit.StartInfo.ArgumentList.Add(data.ClassName);
                submit.StartInfo.ArgumentList.Add(data.JarName);
               
                // Add output paths
                submit.StartInfo.ArgumentList.Add(data.HdfsOutputDirectory.ToString());
                submit.StartInfo.ArgumentList.Add(outputPath);
                submit.StartInfo.ArgumentList.Add($"{data.ExperimentId}_{submissionDateTime}.txt");

                // Add algorithm parameters
                foreach(string item in data.Arguments)
                    submit.StartInfo.ArgumentList.Add(item);

                // Start and wait for error
                submit.Start();
                await submit.WaitForExitAsync();
            }
            return null;
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
