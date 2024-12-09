using API_Backend.Data;
using API_Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MySqlX.XDevAPI.Common;
using System;
using System.Diagnostics;
using System.Text;

namespace API_Backend.Services.DataVisualization
{
    public class DataVisualization
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _dbContext;


        public DataVisualization(IWebHostEnvironment env, ApplicationDbContext dbContext)
        {
            _env = env;
            _dbContext = dbContext;
        }
        private readonly string _outputBaseDirectory = "exports";

        /// <summary>
        /// Method <c>GraphInput</c> creates graph.py executable file and sets user submitted values as parameters
        /// </summary>
        /// <param name="parameters">Users submitted parameters for python script</param>
        /// <returns>boolean representing successfully passed parameters</returns>
        public async Task<bool> GraphInput(VisualizationRequest parameters, string userId)
        {
            try
            {
                // Get the base directory of the application
                var baseDirectory = _env.ContentRootPath;

                var csvResultId = parameters.AggregatedResultID;

                var queryResult = (from csvres in _dbContext.CsvResults
                                   join aggres in _dbContext.AggregatedResults on csvres.AggregatedResultID equals aggres.AggregatedResultID
                                   where aggres.UserID == userId && csvres.CsvResultID == csvResultId
                                   select new
                                   {
                                       csvres.CsvResultFilePath
                                   }).FirstOrDefault();

                string csvFilePath = (await _dbContext.CsvResults.FirstOrDefaultAsync(x => x.AggregatedResultID == parameters.AggregatedResultID)).CsvResultFilePath;

                var csvParentDirectory = Path.GetDirectoryName(csvFilePath);

                if (csvFilePath == null || csvFilePath.Length == 0)
                {
                    throw new ArgumentException("No input file provided or file is empty");
                }

                if (!Path.GetExtension(csvFilePath).ToLower().Equals(".csv"))
                {
                    throw new ArgumentException("Invalid file format. Only .csv files are allowed.");
                }


                string input = FormatInputString(parameters, csvFilePath);


                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "python.exe",
                    Arguments = $"{input}",
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (Process pythonExe = new Process())
                {
                    pythonExe.StartInfo = startInfo;
                    pythonExe.Start();
                    pythonExe.WaitForExit();

                }

                /*
                 * example input
                 * python3 graph.py -i "testdata.csv" -d1 "Ratio.S-SSL" -d2 "Recall" -g "line" -o "output.pdf"
                */

                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                return false;
            }


        }

        /// <summary>
        /// Method <c>FormatInputStringt</c> formats graph.py parameters to pass to python executable file
        /// </summary>
        /// <param name="parameters">Users submitted parameters for python script</param>
        /// <returns>string formatted for python script</returns>
        public string FormatInputString(VisualizationRequest parameters, string CSVFilePath)
        {

            if (string.IsNullOrEmpty(parameters.XAxis) || string.IsNullOrEmpty(parameters.YAxis) || string.IsNullOrEmpty(parameters.GraphType) || string.IsNullOrEmpty(parameters.OutputFileName))
            {
                throw new ArgumentException("Missing graph parameters: X axis, Y axis, graph type, or output filename.");
            }

            var baseDirectory = _env.ContentRootPath;

            // Build the path to the Python script
            string pythonScript = Path.Combine(baseDirectory, "GUI-Team", "graph.py");
            string quotedPythonScript = $"\"{pythonScript}\"";

            StringBuilder sb = new StringBuilder(quotedPythonScript);

            sb.Append(" -i " + $"\"{CSVFilePath}\"");
            sb.Append(" -d1 " + parameters.XAxis);
            sb.Append(" -d2 " + parameters.YAxis);
            sb.Append(" -g " + parameters.GraphType);
            sb.Append(" -o ");

            if (parameters.OutputFileName != null || parameters.OutputFileName != "")
            {
                sb.Append(parameters.OutputFileName);
            }



            Console.WriteLine(sb.ToString());


            return sb.ToString();
        }

        public async Task<string> GetFilePath(int csvResultId, string userId)
        {
            AggregatedResult result = await _dbContext.AggregatedResults.FirstOrDefaultAsync(x => x.UserID == userId && x.AggregatedResultID == csvResultId);

            var csvParentDirectory = Path.GetDirectoryName(result.AggregatedResultFilePath);
            if (string.IsNullOrEmpty(csvParentDirectory))
            {
                throw new InvalidOperationException("CSV parent directory not found.");
            }

            var tempGraphDirectory = Path.Combine(_outputBaseDirectory, "tempGraph");

            Console.WriteLine($"{tempGraphDirectory}");

            if (!Directory.Exists(tempGraphDirectory))
            {
                throw new DirectoryNotFoundException("tempGraph directory not found");
            }


            var tempGraphFiles = Directory.GetFiles(tempGraphDirectory);

            if (tempGraphFiles.Length != 1)
            {
                throw new InvalidOperationException($"Expected exactly one file in the directory, but found {tempGraphFiles.Length}.");
            }

            var graphFilePath = tempGraphFiles[0];

            var destFilePath = Path.Combine(csvParentDirectory, Path.GetFileName(graphFilePath));

            // move graph to aggregateData directory (csv parent directory)
            try
            {
                File.Move(graphFilePath, destFilePath);
                Console.WriteLine($"Moved file from {graphFilePath} to {destFilePath}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to move the graph file. Error: {ex.Message}", ex);
            }

            // delete files in tempGraphDirectory
            foreach (var file in tempGraphFiles)
            {
                try
                {
                    File.Delete(file);
                    Console.WriteLine($"Deleted file: {file}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete file: {file}. Error: {ex.Message}");
                }
            }

            return destFilePath;
        }
    }

}
