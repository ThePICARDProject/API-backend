using Microsoft.Extensions.Primitives;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;

namespace API_backend.Services.FileProcessing
{
    /// <summary>
    /// Service for aggregating experiment data and parsing the data into a .csv file.
    /// </summary>
    /// <remarks>
    /// Implemented based off of bash scripts provided in the docker-swarm repository.
    /// </remarks>
    /// <seealso href="https://github.com/ThePICARDProject/docker-swarm/"/>
    public class FileProcessor
    {
        private readonly string _databaseFileSystemBasePath;
        private readonly string _jarBasePath;

        public FileProcessor(FileProcessorOptions fileProcessorOptions)
        {
            // Initialize the base path for the database filesystem and verify it exists
            _databaseFileSystemBasePath = fileProcessorOptions.DatabaseFileSystemBasePath;
            if (string.IsNullOrEmpty(_databaseFileSystemBasePath))
                throw new ArgumentNullException(nameof(fileProcessorOptions.DatabaseFileSystemBasePath));
            if (!Directory.Exists(_databaseFileSystemBasePath))
                throw new DirectoryNotFoundException($"The output directory \"{_databaseFileSystemBasePath}\" could not be found or does not exist.");

            // Initialize the base path for the .jar file storage and verify it exists
            _jarBasePath = fileProcessorOptions.JarFileBasePath;
            if(string.IsNullOrEmpty(_jarBasePath))
                throw new ArgumentNullException(nameof(fileProcessorOptions.JarFileBasePath));
            if (!Directory.Exists(_jarBasePath))
                throw new DirectoryNotFoundException($"The directory \"{fileProcessorOptions.JarFileBasePath}\" could not be found or does not exist.");
        }

        /// <summary>
        /// Submits a single experiment to the Docker-Swarm.
        /// 
        /// NOTES:
        ///     Must tie a running experiment to a user 
        ///     make a new folder as the experimentId and put the results
        ///     See if we can run the HDFS in this folder
        /// </summary>
        /// <param name="className">The name of the main class in the algorithm .jar file.</param>
        /// <param name="jarName">The name of the .jar file stored at the path "/opt/jars/{file_name}"</param>
        /// <param name="args">Additional arguments specific for each algorithm</param>
        /// <exception cref="NotImplementedException"></exception>
        private void Submit(string className, string jarName, List<string> args)
        {
            // Verify Args
            if(string.IsNullOrEmpty(className))
                throw new ArgumentNullException(nameof(className));
            if(string.IsNullOrEmpty(jarName))
                throw new ArgumentNullException(nameof(jarName));
            if(!File.Exists(Path.Combine(_jarBasePath, jarName)))
                throw new FileNotFoundException($".jar file with the name \"{jarName}\" does not exist in the specified folder.");

            // Create submit process
            using(Process submit = new Process())
            {
                // Setup Process
                submit.StartInfo.FileName = "submit.sh"; // THIS WILL NOT WORK UNLESS SCRIPT IS IN LOCAL DIRECTORY

                Collection<string> arguments = new Collection<string>();
                arguments.Add(className);
                arguments.Add(jarName);
                foreach(string arg in args)
                    arguments.Add(arg);

                submit.StartInfo.CreateNoWindow = true;
            
                submit.Start();
                submit.WaitForExit();
            }
        }

        /// <summary>
        /// Aggregates all data within the Hdfs into a single output text file within the database filesystem.
        /// 
        /// Database filesystem directory structure is as follows: 
        ///     "{DatabaseFileSystemBasePath}/{UserId}/{AlgorithmName}/{Timestamp}_{surveyNumber}.txt"
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="algorithmName"></param>
        /// <param name="experimentNumber"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<string> AggregateData(string userId, string algorithmName, string surveyNumber, string timestamp)
        {
            // Verify Args
            if(string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));
            if(string.IsNullOrEmpty(algorithmName))
                throw new ArgumentNullException(nameof(algorithmName));
            if(string.IsNullOrEmpty(surveyNumber))
                throw new ArgumentNullException(nameof(surveyNumber));
            if(string.IsNullOrEmpty(timestamp))
                throw new ArgumentNullException(nameof(timestamp));

            // Generate the aggregate file path
            string aggregateFilePath = Path.Combine(new string[] 
            { 
                _databaseFileSystemBasePath, 
                userId, 
                algorithmName,
                $"{timestamp}_{surveyNumber}.txt",
            });
            
            // Currently an idea for using bash based on current implementation. Doesn't seem to be a better option
            using(Process resultsOut = new Process())
            {
                // THIS WILL NOT WORK UNLESS SCRIPT IS IN LOCAL DIRECTORY
                resultsOut.StartInfo.FileName = "results-out.sh";

                // Add arguments
                Collection<string> argumentsList = resultsOut.StartInfo.ArgumentList;
                argumentsList.Add(aggregateFilePath);
                resultsOut.StartInfo.CreateNoWindow = false;

                // Start the process and wait to exit
                if (!resultsOut.Start())
                    throw new Exception("Failed to start the new process");
                await resultsOut.WaitForExitAsync();
            }

            // If we exit the process and the file still does not exist, throw an exception
            if (!File.Exists(aggregateFilePath))
                throw new FileNotFoundException($"Failed to aggregate data: output file does not exist at the path \"{aggregateFilePath}\".");
                       
            // Return the path of the saved file
            return aggregateFilePath;   
        }

        /// <summary>
        /// Generates a CSV file containing formatted results.
        /// NOTE: AggregateData must be called before this method.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public string GetCsv()
        {
            throw new NotImplementedException("TODO");
        }
    }
}
