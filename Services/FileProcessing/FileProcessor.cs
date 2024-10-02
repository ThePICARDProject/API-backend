using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        private readonly string _dockerPath;

        public FileProcessor(FileProcessorOptions fileProcessorOptions)
        {
            // Check our Docker-Swarm path
            _dockerPath = fileProcessorOptions.RepositoryBasePath;
            if (string.IsNullOrEmpty(_dockerPath))
                throw new ArgumentNullException(nameof(fileProcessorOptions.RepositoryBasePath));
            if (!Directory.Exists(_dockerPath))
                throw new DirectoryNotFoundException($"The directory \"{fileProcessorOptions.RepositoryBasePath}\" could not be found or does not exist.");

            // Initialize the base path for the database filesystem and verify it exists
            _databaseFileSystemBasePath = fileProcessorOptions.OutputBasePath;
            if (string.IsNullOrEmpty(_databaseFileSystemBasePath))
                throw new ArgumentNullException(nameof(fileProcessorOptions.OutputBasePath));
            if (!Directory.Exists(_databaseFileSystemBasePath))
                throw new DirectoryNotFoundException($"The output directory \"{_databaseFileSystemBasePath}\" could not be found or does not exist.");
        }

        /// <summary>
        /// Aggregates all data within the Hdfs into a single output text file within the database filesystem.
        /// Removes all Docker containers on completion
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
            string error;
            using(Process resultsOut = new Process())
            {
                resultsOut.StartInfo.FileName = Path.Combine(_dockerPath, "results-out.sh");

                // Add arguments
                Collection<string> argumentsList = resultsOut.StartInfo.ArgumentList;
                argumentsList.Add(aggregateFilePath);
                resultsOut.StartInfo.CreateNoWindow = false;

                // Start the process and wait to exit
                if (!resultsOut.Start())
                    throw new Exception("Failed to start the new process");
                
                // Start and wait for error
                resultsOut.Start();
                error = await resultsOut.StandardError.ReadToEndAsync();
                await resultsOut.WaitForExitAsync();
                if (resultsOut.ExitCode != 0)
                    throw new Exception($"AggregateData failed with error code {resultsOut.ExitCode} and message: \"{error}\"");
            }

            // If we exit the process and the file still does not exist, throw an exception
            if (!File.Exists(aggregateFilePath))
                throw new FileNotFoundException($"Failed to aggregate data: output file does not exist at the path \"{aggregateFilePath}\".");
                       
            // Return the path of the saved file
            return aggregateFilePath;   
        }

        /// <summary>
        /// Generates a CSV file containing formatted results.
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public string GetCsv(string outputPath, string resultsPath, string survey, string classifier, int executors)
        {
            if (!Directory.Exists(outputPath))
                throw new DirectoryNotFoundException("Output directory not found.");

            using (StreamReader output = new StreamReader(Path.Combine(resultsPath)))
            {
                // Print header
                string header = "Survey,Classifier,Multiclass,Executors,Trees,Labeled,Recall,Precision,FPR,F1,F4,Time.Split,Time.Train,Time.Test,Repitition,SupervisedTrees,Semi-SupervisedTrees,Ratio.S-SSL\n";
                
                string splittingTimeId = "SplittingTime";
                double splittingTime;

                string trainingTimeId = "TrainingTime";
                double trainingTime;

                string testingTimeId = "TestingTime";
                double testingTime;

                while(!output.EndOfStream)
                {
                    string line = output.ReadLine();
                    if(line.ToUpper().Substring(0, splittingTimeId.Length) == splittingTimeId.ToUpper())
                    {
                        splittingTime = Double.Parse(line.Split('=')[1].Trim());
                    } 
                    else if(line.ToUpper().Substring(0, trainingTimeId.Length) == splittingTimeId.ToUpper())
                    {
                        trainingTime = Double.Parse(line.Split('=')[1].Trim());
                    } 
                    else if(line.ToUpper().Substring(0, testingTimeId.Length) == testingTimeId.ToUpper())
                    {
                        testingTime = Double.Parse(line.Split('=')[1].Trim());
                    }
                }
                
            }
            return outputPath;
        }
    }
}
