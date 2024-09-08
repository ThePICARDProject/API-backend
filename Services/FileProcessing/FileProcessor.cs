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
    /// Need to perform similar functionality to the bash script in results-out.sh.
    /// Instead of outputting to the hardcoded path, generate a path for the database and save there.
    /// </remarks>
    /// <seealso href="https://github.com/ThePICARDProject/docker-swarm/blob/main/docker-swarm/results-out.sh"/>
    public class FileProcessor
    {
        private readonly string _databaseFileSystemBasePath;
        private readonly string _executablePath;

        public FileProcessor(FileProcessorOptions fileProcessorOptions)
        {
            // Get the path for the executable for running bash scripts
            _executablePath = fileProcessorOptions.ExecutablePath;
            if (!File.Exists(_executablePath))
                throw new FileNotFoundException($"The executable file at the path \"{_executablePath}\" could not be found or does not exist.");

            // Initialize the base path for the database filesystem and verify it exists
            _databaseFileSystemBasePath = fileProcessorOptions.DatabaseFileSystemBasePath;
            if (string.IsNullOrEmpty(_databaseFileSystemBasePath))
                throw new ArgumentNullException(nameof(fileProcessorOptions.DatabaseFileSystemBasePath));
            if (!Directory.Exists(_databaseFileSystemBasePath))
                throw new DirectoryNotFoundException($"The output directory \"{_databaseFileSystemBasePath}\" could not be found or does not exist.");
        }

        /// <summary>
        /// Aggregates all data within the Hdfs into a single output
        /// text file within the database filesystem.
        /// 
        /// Database filesystem directory structure is as follows: **CHECK ON THIS**
        ///     "{DatabaseFileSystemBasePath}/{UserId}/{AlgorithmName}/{SureveyNumber}.txt"
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="algorithmName"></param>
        /// <param name="experimentNumber"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<string> AggregateData(string userId, string algorithmName, string survey)
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
            
            // Currently an idea for using bash based on current implementation. Doesn't seem to be a better option
            using(Process resultsOut = new Process())
            {
                resultsOut.StartInfo.FileName = _executablePath;

                // Add arguments
                Collection<string> argumentsList = resultsOut.StartInfo.ArgumentList;
                argumentsList.Add("/results_out.sh");
                argumentsList.Add(aggregateFilePath);
                resultsOut.StartInfo.CreateNoWindow = false;

                // Start the process and wait to exit
                resultsOut.Start();
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
            throw new NotImplementedException();
        }
    }
}
