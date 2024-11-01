using API_backend.Models;
using API_backend.Services.Docker_Swarm;
using API_backend.Services.FileProcessing;
using API_Backend.Models;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq.Expressions;
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
        // File Paths in the applications local directory
        private readonly string _rootDirectory;
        private readonly string _experimentOutputBasePath = "./results";
        private readonly string _dockerImagesBasePath = "./docker-images";
        private readonly string _hadoopOutputBasePath = "hdfs://master:8020";

        public DockerSwarm(string rootDirectory) : this(rootDirectory, "-1", "-1") {}

        public DockerSwarm(string rootDirectory, string advertiseIP, string advertisePort)
        {
            _rootDirectory = rootDirectory;
            Console.WriteLine(_rootDirectory);
            Console.WriteLine(Path.Combine(rootDirectory, "docker-compose.yml"));

            // Verify program files
            if (!File.Exists(Path.Combine(rootDirectory, "docker-compose.yml")))
                throw new Exception("Docker Compose file not found in the project root directory.");
            if (!Directory.Exists(Path.Combine(rootDirectory, "docker-images")))
                throw new Exception("Docker images directory not found in the project root directory.");
            if (!Directory.Exists(Path.Combine(rootDirectory, "scripts")))
                throw new Exception("Scripts directory not found in the project root directory.");

            // Run DockerSwarm_Init scripts
            string error = "";
            int errorCode;
            using (Process dockerSwarmInit = new Process())
            {
                dockerSwarmInit.StartInfo.RedirectStandardError = true;
                dockerSwarmInit.StartInfo.UseShellExecute = false;

                // Add Arguments
                //dockerSwarmInit.StartInfo.WorkingDirectory = _rootDirectory;
                dockerSwarmInit.StartInfo.FileName = Path.Combine(_rootDirectory, "scripts", "dockerswarm-init.sh");
                dockerSwarmInit.StartInfo.ArgumentList.Add(Environment.UserName);
                dockerSwarmInit.StartInfo.ArgumentList.Add(advertiseIP);
                dockerSwarmInit.StartInfo.ArgumentList.Add(advertisePort);
                dockerSwarmInit.StartInfo.ArgumentList.Add(_dockerImagesBasePath);
                dockerSwarmInit.StartInfo.ArgumentList.Add(_experimentOutputBasePath);

                // Start process and read stderror
                dockerSwarmInit.ErrorDataReceived += (sender, args) => error += args.Data ?? "";
                dockerSwarmInit.Start();
                dockerSwarmInit.BeginErrorReadLine();
                dockerSwarmInit.WaitForExit();
                errorCode = dockerSwarmInit.ExitCode;
            }

            // If an error occurs, throw an exception
            if (errorCode != 0)
                throw new Exception(error);
        }

        /// <summary>
        /// Submits an experiment to Docker-Swarm based on the request data.
        /// </summary>
        /// <param name="requestData"></param>
        /// <returns></returns>
        public async Task<ExperimentResponse> SubmitExperiment(ExperimentRequest requestData, StoredDataSet dataset)
        {
            // Get the date and time of the submit request
            DateTime dateTime = DateTime.Now;
            string submissionDateTime = $"{dateTime.Year.ToString()}-{dateTime.Month.ToString()}-{dateTime.Day.ToString()}" +
                $"_{dateTime.Hour.ToString()}-{dateTime.Minute.ToString()}-{dateTime.Second.ToString()}";

            // Generate user/experiment specific directories
            string datasetPath = dataset.FilePath;
            string outputPath = Path.Combine(_experimentOutputBasePath, requestData.UserID, requestData.ExperimentID);
            string outputName = $"{requestData.ExperimentID}_{submissionDateTime}.txt";

            // Update Docker images
            this.UpdateDockerfile(requestData.UserID);

            // Create submit process
            int? exitCode = null;
            string error = "";
            using (Process submit = new Process())
            {
                // Setup Process
                submit.StartInfo.FileName = Path.Combine(_rootDirectory, "scripts", "submit-experiment.sh");
                
                submit.StartInfo.CreateNoWindow = false;

                submit.StartInfo.RedirectStandardError = true;
                submit.StartInfo.UseShellExecute = false;

                submit.StartInfo.ArgumentList.Add(Environment.UserName);
                submit.StartInfo.ArgumentList.Add(Path.Combine(outputPath, $"{requestData.ExperimentID}_log.txt"));
                submit.StartInfo.ArgumentList.Add($"./{datasetPath}");

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

                // Get algorithm parameters
                List<(int, string)> parameters = new List<(int, string)>();
                foreach (ExperimentAlgorithmParameterValue item in requestData.AlgorithmParameters.ParameterValues)
                    parameters.Add((item.AlgorithmParameter.DriverIndex, item.Value.ToString()));
                
                // Sort according to each arguments DriverIndex
                parameters.Sort(delegate((int, string) item1, (int, string) item2)
                {
                    if (item1.Item1 > item2.Item1)
                        return 1;
                    if (item1.Item1 < item2.Item1)
                        return -1;
                    return 0;
                });

                // Add algorithm arguments
                foreach ((int, string) arg in parameters)
                    submit.StartInfo.ArgumentList.Add(arg.Item2);

                // Start and read from stderror
                submit.ErrorDataReceived += (sender, args) => error += args.Data ?? "";
                submit.Start();
                submit.BeginErrorReadLine();
                await submit.WaitForExitAsync();
                exitCode = submit.ExitCode;
            }

            // Generate and return an experiment response
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
