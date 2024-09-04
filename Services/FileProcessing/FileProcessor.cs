using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Linq.Expressions;

namespace API_backend.Services.FileProcessing
{
    /// <summary>
    /// Service for aggregating experiment data and parsing the data into a .csv file.
    /// </summary>
    /// <remarks>
    /// Need to perform similar functionality to the bash script in results-out.sh.
    /// Instead of outputting to the hardcoded path, generate a path for the database and save there.
    /// </remarks>
    /// <seealso href="https://github.com/ThePICARDProject/docker-swarm/blob/main/docker-swarm/results-out.sh"/>
    public class FileProcessor
    {
        private readonly string _databaseFileSystemBasePath;

        public FileProcessor(FileProcessorOptions fileProcessorOptions)
        {                             
            // Initialize the base path for the database filesystem and verify it exists
            _databaseFileSystemBasePath = fileProcessorOptions.DatabaseFileSystemBasePath;
            if (string.IsNullOrEmpty(_databaseFileSystemBasePath))
                throw new ArgumentNullException(nameof(fileProcessorOptions.DatabaseFileSystemBasePath));
            if (!Directory.Exists(_databaseFileSystemBasePath))
                throw new DirectoryNotFoundException($"The output directory \"{_databaseFileSystemBasePath}\" could not be found or does not exist");
        }

        /// <summary>
        /// Aggregates all data within the Hdfs into a single output
        /// text file within the database filesystem.
        /// 
        /// Database filesystem directory structure is as follows: **CHECK ON THIS**
        ///     {DatabaseFileSystemBasePath}/{UserId}/{AlgorithmName}/{SureveyNumber}.txt
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="algorithmName"></param>
        /// <param name="experimentNumber"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public string AggregateData(string userId, string algorithmName, string survey)
        {
            // Verify Args
            if(string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));
            if(string.IsNullOrEmpty(algorithmName))
                throw new ArgumentNullException(nameof(algorithmName));
            if(string.IsNullOrEmpty(survey))
                throw new ArgumentNullException(nameof(survey));

            // Generate the aggregate file path
            string aggregateFilePath = Path.Combine(new string[] 
            { 
                _databaseFileSystemBasePath, 
                userId, 
                algorithmName, 
                survey 
            });

            ////////////////////////////////////
            ///  INTERFACE WITH HDFS TO GET  ///
            ///  FILES AND OUTPUT TO THE DB  ///
            ///     FILESYSTEM BASE PATH     ///
            ////////////////////////////////////
            
            // Currently an idea for using bash based on current implementation. Doesn't seem to be a better option
            using(Process resultsOut = new Process())
            {
                resultsOut.StartInfo.FileName = "/bin/sh";
                // This is the command ran by results-out.sh, with the output path substituted with the database path
                resultsOut.StartInfo.Arguments = $"docker run --rm --name results-extractor --network \"$(basename \"$(pwd)\")_cluster-network\" -v \"$(pwd)/results:/mnt/results\" spark-hadoop:latest hdfs dfs -getmerge /data/results/palfa/output {aggregateFilePath}";
                resultsOut.StartInfo.CreateNoWindow = false;

                System.Diagnostics.Process.Start(aggregateFilePath);
            }
                       
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
            throw new NotImplementedException();
        }
    }
}
