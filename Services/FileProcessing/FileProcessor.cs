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
﻿using System.Diagnostics;
using Org.BouncyCastle.Bcpg.Sig;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.AspNetCore.Routing.Constraints;
using API_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;


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
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _dbContext;

        public FileProcessor(IWebHostEnvironment env, ApplicationDbContext dbContext)
        {
            _env = env;
            _dbContext = dbContext;
        }
        private readonly string _outputBaseDirectory = "exports";

        public FileProcessor()
        {
        }

        public string AggregateData(string userId, string requestId, List<string> filePaths)
        {

            // Verify arguments
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            // Construct output path
            string exportPath = Path.Combine(_outputBaseDirectory, userId, requestId);
            if (Directory.Exists(exportPath))
                throw new Exception($"Export with the Id {requestId} already exists.");
            else
            {
                this.CreateDirectories(exportPath);
            }

            // For each results file, append the results to the aggregate file
            exportPath = Path.Combine(exportPath, $"{requestId}.txt");
            foreach (var filePath in filePaths)
            {
                List<string> lines = File.ReadLines(filePath).ToList();
                lines.Insert(0, $"-----\nOutput Results for {Path.GetFileName(filePath)}\n-----\n");
                File.AppendAllLines(exportPath, lines);
            }

            // Return the path of the saved file
            return exportPath;
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
                throw;
            }
            return outputPath;
        }

        // TODO: sqlQuery function will take a set of parameters and form an SQL query
        // to search db, getting a list of file paths and storing them to pass to getCSV and aggregate data
        public async Task sqlQuery(List<string> desiredMetrics, List<string> queryParams)
        {

            // TODO: data sanitization

            /**
             * User input breakdown:
             * desiredMetrics - will be passed to getCSV
             * queryParams: 
             *      - each query param consists of [field] (> | < | >= | <= | = | != | BETWEEN | LIKE) [value]
             *      
             * 
             */

            /**
             * Example sql query:
             *      - SELECT resultFilePath, resultFileName
             *        FROM experimentResults eres
             *        JOIN experimentRequests ereq on ereq.ExperimentID = eres.ExperimentID
             *        JOIN dockerSwarmParameters dsp on ereq.ExperimentID = dsp.ExperimentID
             *        WHERE queryParams[0];
             * 
             */

            var data = await (from resultsList in _dbContext.ExperimentResults
                        join clusterList in _dbContext.ClusterParameters
                        on resultsList.ExperimentID
                        equals clusterList.ExperimentID
                        select new
                        {
                            Results = resultsList,
                            Clusters = clusterList
                        }).
                        ToListAsync();


            foreach (var item in data)
            {
                Console.WriteLine($"Result File Path: {item.Results.CSVFilePath}, Result File Name: {item.Results.CSVFileName}, " +
                                  $"Driver Memory: {item.Clusters.DriverMemory}, Driver Cores: {item.Clusters.DriverCores}");
            }

            /** TODO: Clarify changes needed in the db
             *      - Should experimentResults have a resultFilePath rather than a csvFilePath? 
             *          - each experiment results in one raw data .txt file, multiple experiment results are then later aggregated into a csv file after a sql query
             *      - What tables need to be joined in order to query for all relevant metrics?
             *          - experimentResults will have .txt file path
             *          - dockerSwarmParameters has:
             *              - driver memory
             *              - driver cores
             *              - executor cores
             *              - executor memory
             *              - memory overhead
             *          - Not clear if algorithm parameters will need to be queried
             *              - these are dynamically stored (variable parameters)
             *              - likely difficult to safely query (front end would require text input and sanitization)
             *                  - possibly prone to errors
             *          - experimentRequests
             *              - has a field "parameters" with text data type
             *      - Insert test db values to query from
             */

            return;
        }



        public void GetCsvTest(List<string> desiredMetrics, string inputFile, string outputFilePath)
        {

            // TODO: add SQL query, store list of .txt files, create loop appending csv file with values of each file
            // Simulate successful SQL query by storing all three example results in a list and looping


            // Get the base directory of the application
            var baseDirectory = _env.ContentRootPath;
            string inputAppendPath = "\\Services\\FileProcessing\\Test Files\\";
            string inputFilePath = baseDirectory + inputAppendPath + inputFile;

            string outputFile = "output1.csv"; // TODO: replace with output file from function parameter

            string outputDirectoryPath = baseDirectory + "\\Services\\FileProcessing\\OutputCSV\\";
            string tempOutputFilePath = outputDirectoryPath + outputFile;
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
                if (File.Exists(tempOutputFilePath))
                {
                    // TODO: add try catch block
                    File.AppendAllText(tempOutputFilePath, csvValues);

                } else
                {

                    // combine headers and values into csv formatted string
                    string csv = csvHeaders + csvValues;


                    // TODO: add try catch block
                    File.WriteAllText(tempOutputFilePath, csv);

                }
            }
            return;
        }

        private void CreateDirectories(string filePath)
        {
            using (Process permissions = new Process())
            {
                permissions.StartInfo.FileName = "./scripts/create_export_directory.sh";
                permissions.StartInfo.Arguments = $"./{filePath}";

                permissions.Start();
                permissions.WaitForExit();
            }
        }
    }
}

// "SplittingTime",
// "TrainingTime",
// "TestingTime"
// "Recall(1.0)"
// "Precision(1.0)"
    
