using CsvHelper;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
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
        private readonly IWebHostEnvironment _env;

        public FileProcessor(IWebHostEnvironment env)
        {
            _env = env;
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
        public void AggregateData(string survey, string algorithmName, string timestamp, string resultsDirectory)
        {
            // Check if out resultsDirectory exists
            if (!Directory.Exists(resultsDirectory))
                throw new DirectoryNotFoundException($"The directory \"{resultsDirectory}\" was not found");

            // Generate an output file path and get all results files
            string outputPath = Path.Combine(resultsDirectory, $"{survey}_{algorithmName}_{timestamp}.txt");
            var resultsFiles = Directory.EnumerateFiles(resultsDirectory);
            
            // For each results file, append the results to the aggregate file
            foreach (var filePath in resultsFiles)
            {
                List<string> lines = File.ReadLines(Path.Combine(resultsDirectory, filePath)).ToList();
                lines.Insert(0, $"---------------------------------------------------------------------------\nOutput Results for { Path.GetFileName(filePath)}\n---------------------------------------------------------------------------\n");
                File.AppendAllLines(outputPath, lines);
                File.Delete(Path.Combine(resultsDirectory, filePath));
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

            using (StreamReader output = new StreamReader(Path.Combine(resultsPath)))
            {

                // Will need to be changed to be dynamic
                // Print header
                string header = "Survey,Classifier,Multiclass,Executors,Trees,Labeled,Recall,Precision,FPR,F1,F4,Time.Split,Time.Train,Time.Test,Repitition,SupervisedTrees,Semi-SupervisedTrees,Ratio.S-SSL\n";
                
                string splittingTimeId = "SplittingTime";
                double splittingTime;

                string trainingTimeId = "TrainingTime";
                double trainingTime;

                string testingTimeId = "TestingTime";
                double testingTime;

                string recallId = "Recall(1.0)";
                double recall;

                string precisionId = "Precision(1.0)";
                double precision;

                while(!output.EndOfStream)
                {
                    string line = output.ReadLine();
                    if (line.ToUpper().Substring(0, splittingTimeId.Length) == splittingTimeId.ToUpper())
                    {
                        splittingTime = Double.Parse(line.Split('=')[1].Trim());
                        Console.Write(splittingTime);
                    }
                    else if (line.ToUpper().Substring(0, trainingTimeId.Length) == splittingTimeId.ToUpper())
                    {
                        trainingTime = Double.Parse(line.Split('=')[1].Trim());
                        Console.Write(trainingTime);

                    }
                    else if (line.ToUpper().Substring(0, testingTimeId.Length) == testingTimeId.ToUpper())
                    {
                        testingTime = Double.Parse(line.Split('=')[1].Trim());
                        Console.Write(testingTime);

                    }
                    else if (line.ToUpper().Substring(0, precisionId.Length) == precisionId.ToUpper())
                    {
                        precision = Double.Parse(line.Split("=")[1].Trim());
                        Console.Write(precision);

                    }
                    else if (line.ToUpper().Substring(0, recallId.Length) == recallId.ToUpper())
                    {
                        recall = Double.Parse(line.Split("=")[1].Trim());
                        Console.Write(recall);

                    }
                }
                
            }
            return outputPath;
        }

        // TODO: sqlQuery function will take a set of parameters and form an SQL query to search db
        public string sqlQuery(List<string> queryParams)
        {

            return "";
        }



        public void GetCsvTest(List<string> desiredMetrics, string aggregatedDataFile)
        {

            // TODO: add SQL query, store list of .txt files, create loop appending csv file with values of each file
            // Simulate successful SQL query by storing all three example results in a list and looping

            // TODO: change to accomodate aggregate data


            // Get the base directory of the application
            var baseDirectory = _env.ContentRootPath;
            string inputAppendPath = "\\Services\\FileProcessing\\Test Files\\";
            string inputFilePath = baseDirectory + inputAppendPath + aggregatedDataFile;

            string outputFile = "output1.csv"; // TODO: replace with output file from function parameter

            string outputDirectoryPath = baseDirectory + "\\Services\\FileProcessing\\OutputCSV\\";
            string outputFilePath = outputDirectoryPath + outputFile;
            Console.WriteLine("base directory: " + baseDirectory);


            using (StreamReader output = new StreamReader(Path.Combine(inputFilePath)))
            {
                // string header = "Survey,Classifier,Multiclass,Executors,Trees,Labeled,Recall,Precision,FPR,F1,F4,Time.Split,Time.Train,Time.Test,Repitition,SupervisedTrees,Semi-SupervisedTrees,Ratio.S-SSL\n";


                // Dictory of key value pairs representing metric to obtain from .txt file and corresponding value (initially set to null)
                var metrics = desiredMetrics.ToDictionary(metric => metric, metric => (double?)null);


                while (!output.EndOfStream)
                {
                    string? line = output.ReadLine();

                    // parse .txt doc for metric and store corresponding value in dictionary
                    foreach (var metricKey in metrics.Keys.ToList())
                    {
                        if (line != null && line.StartsWith(metricKey, StringComparison.OrdinalIgnoreCase))
                        {
                            var value = Double.Parse(line.Split('=')[1].Trim());
                            metrics[metricKey] = value;
                        }
                    }
                }

                // Check if the csv output directory exists
                if (!Directory.Exists(outputDirectoryPath))
                {
                    Directory.CreateDirectory(outputDirectoryPath);
                }


                // get list of headers, store in comma separated string, append new line
                var headerList = metrics.Keys.ToList();
                string csvHeaders = System.String.Join(",", headerList.ToArray()) + "\n";

                // get list of values, store in comma separated string, append new line
                var valueList = metrics.Values.ToList();
                string csvValues = System.String.Join(",", valueList.ToArray()) + "\n";



                // If file exists, append new values -- otherwise, create new file and add headers and values
                if (File.Exists(outputFilePath))
                {
                    // TODO: add try catch block
                    File.AppendAllText(outputFilePath, csvValues);

                } else
                {

                    // combine headers and values into csv formatted string
                    string csv = csvHeaders + csvValues;


                    // TODO: add try catch block
                    File.WriteAllText(outputFilePath, csv);

                }
            }
            return;
        }
    }
}

// "SplittingTime",
// "TrainingTime",
// "TestingTime"
// "Recall(1.0)"
// "Precision(1.0)"