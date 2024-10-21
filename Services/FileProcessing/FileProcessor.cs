
﻿using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
﻿using System.Diagnostics;
using Org.BouncyCastle.Bcpg.Sig;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.AspNetCore.Routing.Constraints;


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
        private readonly string _outputBaseDirectory = "exports";

        public FileProcessor()
        {

            // Create Exports directory
            if(!Directory.Exists(_outputBaseDirectory))
                Directory.CreateDirectory(_outputBaseDirectory);
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
                Directory.CreateDirectory(exportPath);
                this.SetFilePermissions(exportPath);
            }

                // For each results file, append the results to the aggregate file
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
    
        private void SetFilePermissions(string filePath)
        {
            using(Process permissions = new Process())
            {
                permissions.StartInfo.FileName = "/bin/bash";
                permissions.StartInfo.Arguments = $"chmod +rwx ./{filePath}";
            }
        }
    }
}