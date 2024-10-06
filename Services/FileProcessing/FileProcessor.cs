using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
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



        public void GetCsvTest()
        {

            // Get the base directory of the application
                var baseDirectory = _env.ContentRootPath;

            string path = "\\Services\\FileProcessing\\Test Files\\PICARD Example 1 Results.txt";
                
            Console.WriteLine(baseDirectory);

            string fullPath = baseDirectory + path;

            Console.WriteLine(fullPath);

            using (StreamReader output = new StreamReader(Path.Combine(fullPath)))
            {
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

                while (!output.EndOfStream)
                {
                    string line = output.ReadLine();

                    Console.WriteLine(line);
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
            return;
        }
    }
}
