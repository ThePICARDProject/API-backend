﻿using API_backend.Models;
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
        private readonly string _jarBasePath;
        private readonly string _repositoryBasePath;

        public ExperimentService(IOptions<ExperimentOptions> options) 
        {
            // Check our Docker-Swarm path
            _repositoryBasePath = options.Value.RepositoryBasePath;
            if (string.IsNullOrEmpty(_repositoryBasePath))
                throw new ArgumentNullException(nameof(options.Value.RepositoryBasePath));
            if (!Directory.Exists(_repositoryBasePath))
                throw new DirectoryNotFoundException($"The directory \"{options.Value.RepositoryBasePath}\" could not be found or does not exist.");

            // Initialize the base path for the .jar file storage and verify it exists
            _jarBasePath = options.Value.JarFileBasePath;
            if (string.IsNullOrEmpty(_jarBasePath))
                throw new ArgumentNullException(nameof(options.Value.JarFileBasePath));
            if (!Directory.Exists(_jarBasePath))
                throw new DirectoryNotFoundException($"The directory \"{options.Value.JarFileBasePath}\" could not be found or does not exist.");
        }

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
            // Construct paths
            string submitPath = Path.Combine(_repositoryBasePath, "submit-experiment.sh");

            // Verify Args
            if (string.IsNullOrEmpty(data.ClassName))
                throw new ArgumentNullException(nameof(data.ClassName));
            if (string.IsNullOrEmpty(data.RelativeJarPath))
                throw new ArgumentNullException(nameof(data.RelativeJarPath));
            if (!File.Exists(submitPath))
                throw new FileNotFoundException($"Submit file with the path \"{submitPath}\" does not exist.");
            if (!File.Exists(Path.Combine(_jarBasePath, data.RelativeJarPath)))
                throw new FileNotFoundException($".jar file with the name \"{data.RelativeJarPath}\" does not exist in the specified folder.");

            // Create submit process
            string error;
            using (Process submit = new Process())
            {
                // Setup Process
                submit.StartInfo.FileName = submitPath;

                Collection<string> arguments = new Collection<string>();
                
                // Add docker-swarm path and dataset
                arguments.Add(_repositoryBasePath);
                arguments.Add(data.DatasetName);

                // Add Trials
                arguments.Add(data.Trials.ToString());

                // Add Node Counts
                arguments.Add(data.NodeCounts.Count.ToString());
                foreach(int node in data.NodeCounts)
                    arguments.Add(node.ToString());

                // Add Driver data
                arguments.Add(data.DriverMemory);
                arguments.Add(data.DriverCores);

                // Add Executer data
                arguments.Add(data.ExecutorNumber);
                arguments.Add(data.ExecutorMemory);
                arguments.Add(data.ExecuterCores);
                arguments.Add(data.MemoryOverhead);
                
                // Add required algorithm arguments
                arguments.Add(data.NumberOfClasses.ToString());
                arguments.Add(data.NumberOfTrees.ToString());
                arguments.Add(data.Impurity.ToString());
                arguments.Add(data.MaxDepth.ToString());
                arguments.Add(data.MaxBins.ToString());

                arguments.Add(data.PercentLabeled.ToString());
                
                // Add output paths
                arguments.Add(data.HdfsOutputDirectory.ToString());
                arguments.Add(data.LocalOutputDirectory.ToString());

                // Add optional arguments
                foreach(string arg in data.args)
                    arguments.Add(arg);
             
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