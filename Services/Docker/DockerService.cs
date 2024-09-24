using API_backend.Models;
using API_backend.Services.FileProcessing;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xml;

namespace API_backend.Services.Docker
{
    /// <summary>
    /// Service for intiating experiments using Docker-Swarm.
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
        private string _hdfsPath;

        public DockerService(DockerOptions options) 
        {
            // Check our Docker-Swarm path
            _dockerPath = options.DockerSwarmPath;
            if (string.IsNullOrEmpty(_dockerPath))
                throw new ArgumentNullException(nameof(options.DockerSwarmPath));
            if (!Directory.Exists(_dockerPath))
                throw new DirectoryNotFoundException($"The directory \"{options.DockerSwarmPath}\" could not be found or does not exist.");

            // Check the Hadoop Path
            _hdfsPath = options.HdfsPath;
            if(string.IsNullOrEmpty(_hdfsPath))
                throw new ArgumentNullException(_hdfsPath);

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
        /// Spins up an instance of Docker-Swarm and Hadoop, and runs experiments in
        /// a folder with the hadoop path and the userId of the user who submitted it.
        ///
        /// Executes an experiment consisting of the number of trials defined in SubmitExperiment
        /// for each nodeCount in the list.
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
        public void SubmitExperiment(SubmitExperiment data)
        {
            // Construct paths
            string sparkHadoopConfig = Path.Combine(new string[] { _dockerPath, "docker-images", "spark-hadoop", "config", "hdfs-site.xml" });
            string submitPath = Path.Combine(_dockerPath, "submit-experiment.sh");

            // Verify Args
            if (string.IsNullOrEmpty(data.ClassName))
                throw new ArgumentNullException(nameof(data.ClassName));
            if (string.IsNullOrEmpty(data.RelativeJarPath))
                throw new ArgumentNullException(nameof(data.RelativeJarPath));
            if (!File.Exists(sparkHadoopConfig))
                throw new FileNotFoundException($"Hadoop config file with the path \"{sparkHadoopConfig}\" does not exist.");
            if (!File.Exists(submitPath))
                throw new FileNotFoundException($"Submit file with the path \"{submitPath}\" does not exist.");
            if (!File.Exists(Path.Combine(_jarBasePath, data.RelativeJarPath)))
                throw new FileNotFoundException($".jar file with the name \"{data.RelativeJarPath}\" does not exist in the specified folder.");

            // Update Spark-Hadoop configuration (dfs.datanode.data.dir)
            // Add a folder with the userId to create data nodes
            // CHECK SCRIPTS SHOULD AUTO CREATE ON STARTUP
            string userFolder = Path.Combine(_hdfsPath, data.UserId);
            XmlDocument hdfsConfig = new XmlDocument();
            hdfsConfig.Load(sparkHadoopConfig);
            XmlNodeList properties = hdfsConfig.GetElementsByTagName("property");
            foreach(XmlNode property in properties)
            {
                if (property.ChildNodes.Item(0).Value == "dfs.namenode.name.dir")
                    property.ChildNodes.Item(1).Value = Path.Combine(userFolder, "nameNode");
                else if (property.ChildNodes.Item(0).Value == "dfs.datanode.data.dir")
                    property.ChildNodes.Item(1).Value = Path.Combine(userFolder, "dataNode");
            }
            hdfsConfig.Save(sparkHadoopConfig);

            // Create submit process
            using (Process submit = new Process())
            {
                // Setup Process
                submit.StartInfo.FileName = submitPath;

                Collection<string> arguments = new Collection<string>();
                
                // Add docker-swarm path and dataset
                arguments.Add(_dockerPath);
                arguments.Add(data.Dataset);

                // Add trials
                arguments.Add(data.Trials.ToString());

                // TODO

                // Add Node Counts
                arguments.Add(data.NodeCounts.Count.ToString());
                foreach(int node in data.NodeCounts)
                {
                    arguments.Add(node.ToString());
                }

                // Add Trials
                arguments.Add(data.Trials.ToString());
                
                
                //arguments.Add(className);
                //arguments.Add(relativeJarPath);
                //foreach (string arg in args)
                    //arguments.Add(arg);

                submit.StartInfo.CreateNoWindow = true;

                submit.Start();
                submit.WaitForExit();
            }
        }
    

    }
}
