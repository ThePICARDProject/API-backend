
﻿using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
﻿using System.Diagnostics;


namespace API_Backend.Services.FileProcessing
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
        private readonly ILogger<FileProcessor> _logger;

        public FileProcessor(ILogger<FileProcessor> logger)
        {
            _logger = logger;

            _logger.LogInformation("Initializing FileProcessor");
        }

        /// <summary>
        /// Aggregates all raw data files existing in a local filesystem folder into a single output file.
        /// Data from each trial results file is given a header with the filename corresponsing to the 
        /// trial that specific output came from.
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="algorithmName"></param>
        /// <param name="experimentNumber"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public string AggregateData(string userId, string algorithmName, string timestamp, string resultsDirectory)
        {
            _logger.LogInformation("Aggregating data for UserID {UserID}, Algorithm {AlgorithmName}, Survey {Survey}", userId, algorithmName);

            // Verify arguments
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Argument {Argument} is null or empty.", nameof(userId));
                throw new ArgumentNullException(nameof(userId));
            }
            if (string.IsNullOrEmpty(algorithmName))
            {
                _logger.LogError("Argument {Argument} is null or empty.", nameof(algorithmName));
                throw new ArgumentNullException(nameof(algorithmName));
            }

            // Check if out resultsDirectory exists
            if (!Directory.Exists(resultsDirectory))
                throw new DirectoryNotFoundException($"The directory \"{resultsDirectory}\" was not found");

            try
            {

                // Generate an output file path and get all results files
                string outputPath = Path.Combine(resultsDirectory, $"{algorithmName}_{timestamp}.txt");
                var resultsFiles = Directory.EnumerateFiles(resultsDirectory);

                // For each results file, append the results to the aggregate file
                foreach (var filePath in resultsFiles)
                {
                    List<string> lines = File.ReadLines(Path.Combine(resultsDirectory, filePath)).ToList();
                    lines.Insert(0, $"---------------------------------------------------------------------------\nOutput Results for {Path.GetFileName(filePath)}\n---------------------------------------------------------------------------\n");
                    File.AppendAllLines(outputPath, lines);
                    File.Delete(Path.Combine(resultsDirectory, filePath));
                }

                // Return the path of the saved file
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while aggregating data.");
                throw;
            }
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

            _logger.LogInformation("Generating CSV file from aggregated data.");

            try
            {
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

                    while (!output.EndOfStream)
                    {
                        string line = output.ReadLine();
                        if (line.ToUpper().Substring(0, splittingTimeId.Length) == splittingTimeId.ToUpper())
                        {
                            splittingTime = Double.Parse(line.Split('=')[1].Trim());
                        }
                        else if (line.ToUpper().Substring(0, trainingTimeId.Length) == splittingTimeId.ToUpper())
                        {
                            trainingTime = Double.Parse(line.Split('=')[1].Trim());
                        }
                        else if (line.ToUpper().Substring(0, testingTimeId.Length) == testingTimeId.ToUpper())
                        {
                            testingTime = Double.Parse(line.Split('=')[1].Trim());
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while generating CSV file.");
                throw;
            }
            return outputPath;
        }
    }
}