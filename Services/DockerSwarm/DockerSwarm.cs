using API_backend.Models;
using API_backend.Services.Docker_Swarm;
using API_backend.Services.FileProcessing;
using API_Backend.Models;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace API_backend.Services.Docker_Swarm
{
    /// <summary>
    /// Service for working with experiments and Docker-Swarm.
    /// </summary>
    /// <remarks>
    /// Implemented based off of scripts provided in the docker-swarm repository.
    /// Modified for enhanced modularity and use within the API.
    /// </remarks>
    /// <seealso href="https://github.com/ThePICARDProject/docker-swarm/"/>
    public class DockerSwarm
    {
        private readonly string _dataBasePath = "./data";
        private readonly string _experimentOutputBasePath = "./results";
        private readonly string _hadoopOutputBasePath = "hdfs://master:8020";

        public string DataBasePath { get { return _dataBasePath; } }
        public string ExperimentOutputBasePath { get  { return _experimentOutputBasePath; } }
        public string HadoopOutputBasePath {  get { return _hadoopOutputBasePath; } }

        public DockerSwarm() 
        {
            // Verify the data path exists
            if (!Directory.Exists(_dataBasePath))
                throw new Exception($"Path {_dataBasePath} does not exist.");
            
            // If we do not have a results folder, create it
            if (!Directory.Exists(_experimentOutputBasePath))
                Directory.CreateDirectory(_experimentOutputBasePath);
        }

        /// <summary>
        /// Submits an experiment to Docker-Swarm based on the request data.
        /// </summary>
        /// <param name="requestData"></param>
        /// <returns></returns>
        public async Task<ExperimentResponse> SubmitExperiment(ExperimentRequest requestData)
        {
            // Get the date and time of the submit request
            DateTime dateTime = DateTime.Now;
            string submissionDateTime = $"{dateTime.Year.ToString()}-{dateTime.Month.ToString()}-{dateTime.Day.ToString()}" +
                $"_{dateTime.Hour.ToString()}-{dateTime.Minute.ToString()}-{dateTime.Second.ToString()}";

            // Generate user/experiment specific directories
            string outputPath = Path.Combine(_experimentOutputBasePath, requestData.UserID, requestData.ExperimentID);
            string dataBasePath = Path.Combine(_dataBasePath, requestData.UserID);
            string outputName = $"{requestData.ExperimentID}_{submissionDateTime}.txt";

            // Update Docker images
            this.UpdateDockerfile(requestData.UserID);

            // Create submit process
            int? exitCode = null;
            string error = null;
            using (Process submit = new Process())
            {
                // Setup Process
                submit.StartInfo.FileName = "./scripts/submit-experiment.sh";
                submit.StartInfo.CreateNoWindow = false;

                submit.StartInfo.ArgumentList.Add(Environment.UserName);
                submit.StartInfo.ArgumentList.Add($"{_dataBasePath}/{requestData.UserID}");
                submit.StartInfo.ArgumentList.Add(requestData.AlgorithmParameters.DatasetName);

                // Add Node Counts
                submit.StartInfo.ArgumentList.Add(requestData.ClusterParameters.NodeCount.ToString());

                // Add Spark arguments
                submit.StartInfo.ArgumentList.Add(requestData.ClusterParameters.DriverMemory);
                submit.StartInfo.ArgumentList.Add(requestData.ClusterParameters.DriverCores.ToString());
                submit.StartInfo.ArgumentList.Add(requestData.ClusterParameters.ExecutorNumber.ToString());
                submit.StartInfo.ArgumentList.Add(requestData.ClusterParameters.ExecutorCores.ToString());
                submit.StartInfo.ArgumentList.Add(requestData.ClusterParameters.ExecutorMemory);
                submit.StartInfo.ArgumentList.Add(requestData.ClusterParameters.MemoryOverhead.ToString());

                submit.StartInfo.ArgumentList.Add(requestData.Algorithm.MainClassName);
                submit.StartInfo.ArgumentList.Add(Path.GetFileName(requestData.Algorithm.JarFilePath));
                
                // Add output paths
                submit.StartInfo.ArgumentList.Add($"{_hadoopOutputBasePath}");
                submit.StartInfo.ArgumentList.Add(outputPath);
                submit.StartInfo.ArgumentList.Add($"data/{requestData.UserID}/{requestData.ExperimentID}/{outputName}");

                // Get algorithm Parameters
                List<(int, string)> parameters = new List<(int, string)>();
                foreach (ExperimentAlgorithmParameterValue item in requestData.AlgorithmParameters.ParameterValues)
                    parameters.Add((item.AlgorithmParameter.DriverIndex, item.Value.ToString()));
                
                // Sort according to each values DriverIndex
                parameters.Sort(delegate((int, string) item1, (int, string) item2)
                {
                    if (item1.Item1 > item2.Item1)
                        return 1;
                    if (item1.Item1 < item2.Item1)
                        return -1;
                    return 0;
                });

                // Add algorithm parameters
                foreach ((int, string) arg in parameters)
                    submit.StartInfo.ArgumentList.Add(arg.Item2);

                // Start and wait for error
                submit.Start();
                await submit.WaitForExitAsync();
                exitCode = submit.ExitCode;
            }
            return new ExperimentResponse() 
            { 
                ErrorCode = exitCode,
                ErrorMessage = error,
                OutputPath = Path.Combine(outputPath, outputName)
            };
        }

        /// <summary>
        /// Updates the docker images with user specific data.
        /// </summary>
        /// <param name="userId"></param>
        /// <exception cref="FileNotFoundException"></exception>
        private void UpdateDockerfile(string userId)
        {
            // Generate the path and the jar line regex pattern
            string dockerfilePath = "./docker-images/spark-hadoop/Dockerfile";
            if (!File.Exists(dockerfilePath))
                throw new FileNotFoundException("Dockerfile was not found");
            Regex linePattern = new Regex("COPY \\.\\/jars\\/(?:[a-zA-Z0-9]+\\/\\*|\\*) \\/opt\\/jars");

            // Copy each line from the old dockerfile to a string
            string newFileContent = "";
            using(StreamReader dockerfile = new StreamReader(dockerfilePath))
            {
                while(!dockerfile.EndOfStream)
                {
                    string line = dockerfile.ReadLine();
                    Match match = linePattern.Match(line);
                    if (line.Length != 0 && match.Value.Length == line.Length) // If the pattern matches the line, update the line
                            line = $"COPY ./jars/{userId}/* /opt/jars";
                    
                    newFileContent = $"{newFileContent}{line}\n";
                }
            }

            // Write all bytes in the new file content to a new Dockerfile
            File.WriteAllBytes(dockerfilePath, Encoding.UTF8.GetBytes(newFileContent));
        }
    }
}
