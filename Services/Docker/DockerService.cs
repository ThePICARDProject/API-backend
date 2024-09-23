using API_backend.Services.FileProcessing;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace API_backend.Services.Docker
{
    /// <summary>
    /// Service for intiating experiments using Docker-Swarm
    /// 
    /// NOTE: CHECK WHAT IS CONSTANTLY RUNNING AND WHAT WE NEED TO DO TO SPIN UP AN EXPERIMENT
    /// </summary>
    /// /// <remarks>
    /// Implemented based off of bash scripts provided in the docker-swarm repository.
    /// </remarks>
    /// <seealso href="https://github.com/ThePICARDProject/docker-swarm/"/>
    /// <seealso href="https://hadoop.apache.org/docs/r2.4.1/hadoop-project-dist/hadoop-hdfs/hdfs-default.xml"/>
    public class DockerService
    {
        private string _jarBasePath;
        private string _dockerPath;

        public DockerService(DockerOptions options) 
        {
            // Check our Docker-Swarm path
            _dockerPath = options.DockerSwarmPath;
            if (string.IsNullOrEmpty(_dockerPath))
                throw new ArgumentNullException(nameof(options.DockerSwarmPath));
            if (!Directory.Exists(_dockerPath))
                throw new DirectoryNotFoundException($"The directory \"{options.DockerSwarmPath}\" could not be found or does not exist.");

            // Initialize the base path for the .jar file storage and verify it exists
            _jarBasePath = options.JarFileBasePath;
            if (string.IsNullOrEmpty(_jarBasePath))
                throw new ArgumentNullException(nameof(options.JarFileBasePath));
            if (!Directory.Exists(_jarBasePath))
                throw new DirectoryNotFoundException($"The directory \"{options.JarFileBasePath}\" could not be found or does not exist.");
        }

        /// <summary>
        /// Submits a single experiment to Docker-Swarm.
        /// 
        /// Note: HDFS data node local path is defined in hdfs-site.xml
        /// Note: May have to update the configuration each time we run? Construct a folder to place the hdfs data node.
        /// Note: NameNode path is also created in the DockerFile. Must be aware of this if we change path.
        /// </summary>
        /// <param name="userId">The Id of the user submitting an experiment</param>
        /// <param name="className">The main class name in the algorithm .jar.</param>
        /// <param name="relativeJarPath">Relative path for the algorithm .jar file.</param>
        /// <param name="args">Optional arguments for a given algorithm.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public void Submit(string userId, string className, string relativeJarPath, List<string> args)
        {
            // Construct paths
            string sparkHadoopConfig = Path.Combine(new string[] { _dockerPath, "docker-images", "spark-hadoop", "config", "hdfs-site.xml" });
            string submitPath = Path.Combine(_dockerPath, "submit.sh");

            // Verify Args
            if (string.IsNullOrEmpty(className))
                throw new ArgumentNullException(nameof(className));
            if (string.IsNullOrEmpty(relativeJarPath))
                throw new ArgumentNullException(nameof(relativeJarPath));
            if (!File.Exists(sparkHadoopConfig))
                throw new FileNotFoundException($"Hadoop config file with the path \"{sparkHadoopConfig}\" does not exist.");
            if (!File.Exists(submitPath))
                throw new FileNotFoundException($"Submit file with the path \"{submitPath}\" does not exist.");
            if (!File.Exists(Path.Combine(_jarBasePath, relativeJarPath)))
                throw new FileNotFoundException($".jar file with the name \"{relativeJarPath}\" does not exist in the specified folder.");

            // Update Spark-Hadoop configuration (dfs.datanode.data.dir)
            // Add a folder with the userId to create data nodes
            // TODO

            // Create submit process
            using (Process submit = new Process())
            {
                // Setup Process
                submit.StartInfo.FileName = submitPath;

                Collection<string> arguments = new Collection<string>();
                arguments.Add(className);
                arguments.Add(relativeJarPath);
                foreach (string arg in args)
                    arguments.Add(arg);

                submit.StartInfo.CreateNoWindow = true;

                submit.Start();
                submit.WaitForExit();
            }
        }
    

    }
}
