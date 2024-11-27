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

        public string AggregateData(string userId, List<string> requestIds)
        {

            // retrieve file paths
            //List<string> filePaths = new List<string>();


            //foreach (var requestId in requestIds)
            //{
            //    var filePath = _dbContext.ExperimentResults
            //        .Where(e => e.ExperimentID == requestId)
            //        .Select(e => e.ResultFilePath)
            //        .FirstOrDefault();

            //    if (filePath != null)
            //    {
            //        filePaths.Add(filePath);
            //    }
            //}

            // Store in db and use ID for file path
            string tempID = "agg1";


            // Verify arguments
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            // Construct output path
            string exportPath = Path.Combine(_outputBaseDirectory, userId, "AggregateData");


            Console.WriteLine("Export Path: " + exportPath);

            if (!Directory.Exists(exportPath))
            {
                this.tempCreateDirectories(exportPath); // change back to not be temp
            }

            Console.WriteLine("After export path if statement");

            // For each results file, append the results to the aggregate file
            exportPath = Path.Combine(exportPath, $"{tempID}.txt");

            Console.WriteLine("After tempID added to path");

            foreach (var requestId in requestIds)
            {

                var filePath = _dbContext.ExperimentResults
                    .Where(e => e.ExperimentID == requestId)
                    .Select(e => e.ResultFilePath)
                    .FirstOrDefault();

                var fileName = _dbContext.ExperimentResults
                    .Where(e => e.ExperimentID == requestId)
                    .Select(e => e.ResultFileName)
                    .FirstOrDefault();

                Console.WriteLine(filePath);

                var completeFilePath = filePath + fileName;


                if (filePath != null)
                {
                    



                    // TODO: add in all algorithm parameters and values in format "Trees = 10"
                    var queryResult = (from ereq in _dbContext.ExperimentRequests
                                       join users in _dbContext.Users on ereq.UserID equals users.UserID
                                       join alg in _dbContext.Algorithms on ereq.AlgorithmID equals alg.AlgorithmID
                                       join param in _dbContext.AlgorithmParameters on alg.AlgorithmID equals param.AlgorithmID
                                       join values in _dbContext.ExperimentAlgorithmParameterValues on param.ParameterID equals values.ParameterID
                                       where users.UserID == userId && ereq.ExperimentID == requestId
                                       select new
                                       {
                                           ereq.ExperimentID,
                                           alg.AlgorithmID,
                                           param.ParameterName,
                                           values.Value
                                       }).ToList();

                    StringBuilder sb = new StringBuilder("Algorithm Information:\n");


                    foreach (var item in queryResult)
                    {

                        StringBuilder insideSB = new StringBuilder();

                        insideSB.Append(item.ParameterName + " = ");
                        insideSB.AppendLine(item.Value);


                        sb.AppendLine(insideSB.ToString());

                        Console.WriteLine($"ExperimentID: {item.ExperimentID}");
                        Console.WriteLine($"AlgorithmID: {item.AlgorithmID}");
                        Console.WriteLine($"ParameterName: {item.ParameterName}");
                        Console.WriteLine($"Value: {item.Value}");

                    }

                    List<string> lines = File.ReadLines(completeFilePath).ToList();
                    lines.Insert(0, $"nr-----\nRequestID: {requestId}\nOutput Results for {Path.GetFileName(completeFilePath)}\n-----\n{sb.ToString()}\n");
                    File.AppendAllLines(exportPath, lines);

                }


            }


            // TODO: Add aggregate to DB

            // Return the path of the saved file
            return exportPath;
        }


        public async Task<List<string>> QueryExperiments(string userId, QueryExperiment queryParams)
        {


            var clusterParams = queryParams.ClusterParams;

            var algorithmParams = queryParams.AlgorithmParams;



            StringBuilder dynamicQuery = new StringBuilder("x => ");

            foreach (var clusterParam in clusterParams)
            {
                if (clusterParam != clusterParams.Last())
                {
                    dynamicQuery.Append("x.");
                    dynamicQuery.Append(clusterParam);
                    dynamicQuery.Append(" && ");
                } else
                {
                    dynamicQuery.Append("x.");
                    dynamicQuery.Append(clusterParam);
                }
                
            }


            List<AlgorithmQueryModel> algorithmQueryModels = new List<AlgorithmQueryModel>();

            foreach (var algorithmParam in algorithmParams)
            {
                var queryComponents = algorithmParam.Split(' ');

                var algorithmQueryModel = new AlgorithmQueryModel();

                algorithmQueryModel.ParamName = queryComponents[0];
                algorithmQueryModel.ParamOperator = queryComponents[1];
                algorithmQueryModel.ParamValue = queryComponents[2];

                algorithmQueryModels.Add(algorithmQueryModel);
            }


            var queryResult = (from ereq in _dbContext.ExperimentRequests
                               join users in _dbContext.Users on ereq.UserID equals users.UserID
                               join alg in _dbContext.Algorithms on ereq.AlgorithmID equals alg.AlgorithmID
                               join param in _dbContext.AlgorithmParameters on alg.AlgorithmID equals param.AlgorithmID
                               join values in _dbContext.ExperimentAlgorithmParameterValues on param.ParameterID equals values.ParameterID
                               join cluster in _dbContext.ClusterParameters on ereq.ExperimentID equals cluster.ExperimentID
                               where users.UserID == userId
                               select new
                               {
                                   ereq.ExperimentID,
                                   alg.AlgorithmID,
                                   param.ParameterName,
                                   values.Value,
                                   cluster.NodeCount,
                                   cluster.DriverMemory,
                                   cluster.DriverCores,
                                   cluster.ExecutorNumber,
                                   cluster.ExecutorCores,
                                   cluster.ExecutorMemory,
                                   cluster.MemoryOverhead
                               }).ToList();

            foreach (var item in queryResult)
            {
                Console.WriteLine($"ExperimentID: {item.ExperimentID}");
                Console.WriteLine($"AlgorithmID: {item.AlgorithmID}");
                Console.WriteLine($"ParameterName: {item.ParameterName}");
                Console.WriteLine($"Value: {item.Value}");
                Console.WriteLine($"NodeCount: {item.NodeCount}");
                Console.WriteLine($"DriverMemory: {item.DriverMemory}");
                Console.WriteLine($"DriverCores: {item.DriverCores}");
                Console.WriteLine($"ExecutorNumber: {item.ExecutorNumber}");
                Console.WriteLine($"ExecutorCores: {item.ExecutorCores}");
                Console.WriteLine($"ExecutorMemory: {item.ExecutorMemory}");
                Console.WriteLine($"MemoryOverhead: {item.MemoryOverhead}");
                Console.WriteLine("---------------");
            }


            var filteredAlgorithms = queryResult;

            foreach (var algorithmQueryModel in algorithmQueryModels)
            {
                filteredAlgorithms = filteredAlgorithms
                    .Where(r => r.ParameterName == algorithmQueryModel.ParamName && r.Value.Equals(algorithmQueryModel.ParamValue))
                    .ToList();

            }


            Console.WriteLine("Before final filter reached");

            var finalResult = queryResult
                .Where(r => filteredAlgorithms
                    .Any(f => f.AlgorithmID == r.AlgorithmID && f.ParameterName == r.ParameterName && f.Value.Equals(r.Value)))
                .AsQueryable()
                .Where(dynamicQuery.ToString())
                .ToList();

            // Returning the ExperimentIDs from the final result
            List<string> requestIds = finalResult
                .Select(r => r.ExperimentID)
                .ToList();

            foreach (var requestId in requestIds)
            {
                Console.WriteLine(requestId);
            }


            Console.WriteLine("End of queryExperiment reached!");

            return requestIds;
        }



        public Dictionary<string, object> insertAlgorithmData(string requestId)
        {

            return null;
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
                var metrics = desiredMetrics.ToDictionary(metric => metric, metric => (string?)null);

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
                            
                            var value = line.Split('=')[1].Trim();
                            metrics[metricKey] = value;
                        }
                    }


                    if (line != null && line.StartsWith("nr-----") || output.EndOfStream)
                    {
                        var valueList = metrics.Values.ToList();
                        if (valueList.Any(value => value != null && !string.IsNullOrEmpty(value.ToString())))
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
    
