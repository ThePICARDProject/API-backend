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
using API_Backend.Models;
using static Mysqlx.Error.Types;
using System;
using System.Linq.Dynamic.Core;


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
                this.tempCreateDirectories(exportPath); // change back to not be temp
            }

            // For each results file, append the results to the aggregate file
            exportPath = Path.Combine(exportPath, $"{requestId}.txt");
            foreach (var filePath in filePaths)
            {
                List<string> lines = File.ReadLines(filePath).ToList();
                lines.Insert(0, $"nr-----\nOutput Results for {Path.GetFileName(filePath)}\n-----\n");
                File.AppendAllLines(exportPath, lines);
            }

            // Return the path of the saved file
            return exportPath;
        }


        public async Task<List<string>> sqlQuery(QueryExperiment queryParams)
        {

            /**
             * Example sql query:
             *      - SELECT resultFilePath, resultFileName
             *        FROM experimentResults eres
             *        JOIN experimentRequests ereq on ereq.ExperimentID = eres.ExperimentID
             *        JOIN dockerSwarmParameters dsp on ereq.ExperimentID = dsp.ExperimentID
             *        WHERE queryParams[0];
             * 
             */

            var clusterParams = queryParams.ClusterParams;

            var algorithmParams = queryParams.AlgorithmParams;

            StringBuilder dynamicQuery = new StringBuilder("x => ");

            foreach (var clusterParam in clusterParams)
            {
                if (clusterParam != clusterParams.Last())
                {
                    dynamicQuery.Append("x.Clusters.");
                    dynamicQuery.Append(clusterParam);
                    dynamicQuery.Append(" && ");
                } else
                {
                    dynamicQuery.Append("x.Clusters.");
                    dynamicQuery.Append(clusterParam);
                }
                
            }


            // TODO: not correct, need to join all algorithm related tables / values before hand
            foreach (var algorithmParam in algorithmParams)
            {
                if (algorithmParam != algorithmParams.Last())
                {
                    dynamicQuery.Append("x.Algorithms.");
                    dynamicQuery.Append(algorithmParam);
                    dynamicQuery.Append(" && ");
                }
                else
                {
                    dynamicQuery.Append("x.Algoritms.");
                    dynamicQuery.Append(algorithmParam);
                }

            }



            var data = await _dbContext.ExperimentResults
            .Join(
                _dbContext.ClusterParameters,
                resultsList => resultsList.ExperimentID,
                clusterList => clusterList.ExperimentID,
                (resultsList, clusterList) => new { Results = resultsList, Clusters = clusterList }
            )
            .Where(dynamicQuery.ToString())
            .ToListAsync();

            List<string> filePaths = new List<string>();


            foreach (var item in data)
            {
                Console.WriteLine($"Result File Path: {item.Results.ResultFilePath}, Result File Name: {item.Results.ResultFileName}, " +
                                  $"Driver Memory: {item.Clusters.DriverMemory}, Driver Cores: {item.Clusters.DriverCores}");

                filePaths.Add(item.Results.ResultFilePath);
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

            return filePaths;
        }

        public List<string> generateCSV(QueryExperiment request)
        {
            var clusterParams = request.ClusterParams;

            var algorithmParams = request.AlgorithmParams;



            return null;
        }



        public string GetCsv(List<string> desiredMetrics, string aggregateFilePath)
        {
            if (!Path.Exists(aggregateFilePath))
            {
                throw new FileNotFoundException($"The file at path '{aggregateFilePath}' was not found.");
            }

            string aggregateParent = Path.GetDirectoryName(aggregateFilePath);
            var outputFilePath = Path.Combine(aggregateParent, $"{Path.GetFileName(aggregateParent)}.csv");



            using (StreamReader output = new StreamReader(aggregateFilePath))
            {

                // Dictory of key value pairs representing metric to obtain from .txt file and corresponding value (initially set to null)
                var metrics = desiredMetrics.ToDictionary(metric => metric, metric => (double?)null);

                var headerList = metrics.Keys.ToList();
                string csvHeaders = System.String.Join(",", headerList.ToArray()) + "\n";

                File.AppendAllText(outputFilePath, csvHeaders);



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


                    if (line != null && line.StartsWith("nr-----") || output.EndOfStream)
                    {
                        var valueList = metrics.Values.ToList();
                        if (valueList.Any(value => value.HasValue))
                        {
                            string csvValues = System.String.Join(",", valueList.ToArray()) + "\n";
                            File.AppendAllText(outputFilePath, csvValues);

                        }
                        // Clear metric values
                        foreach (var key in metrics.Keys.ToList())
                        {
                            metrics[key] = null;
                        }

                    }


                }



            }
            return outputFilePath;
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

        private void tempCreateDirectories(string filePath)
        {
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
        }
    }
}

// "SplittingTime",
// "TrainingTime",
// "TestingTime"
// "Recall(1.0)"
// "Precision(1.0)"
    
