using API_Backend.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using System.Text;

namespace API_Backend.Services.DataVisualization
{
    public class DataVisualization
    {
        private readonly IWebHostEnvironment _env;

        public DataVisualization(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Method <c>GraphInput</c> creates graph.py executable file and sets user submitted values as parameters
        /// </summary>
        /// <param name="parameters">Users submitted parameters for python script</param>
        /// <returns>boolean representing successfully passed parameters</returns>
        public bool GraphInput(VisualizationRequest parameters)
        {
            try
            {
                // Get the base directory of the application
                var baseDirectory = _env.ContentRootPath;

                var csvFilePath = parameters.CSVFilePath;

                if (csvFilePath == null || csvFilePath.Length == 0)
                {
                    throw new ArgumentException("No input file provided or file is empty");
                }

                if (!Path.GetExtension(csvFilePath).ToLower().Equals(".csv"))
                {
                    throw new ArgumentException("Invalid file format. Only .csv files are allowed.");
                }


                string input = FormatInputString(parameters);


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
        public string FormatInputString(VisualizationRequest parameters)
        {

            if (string.IsNullOrEmpty(parameters.XAxis) || string.IsNullOrEmpty(parameters.YAxis) || string.IsNullOrEmpty(parameters.GraphType) || string.IsNullOrEmpty(parameters.OutputFileName)) {
                throw new ArgumentException("Missing graph parameters: X axis, Y axis, graph type, or output filename.");
            }
            
            var baseDirectory = _env.ContentRootPath;

            // Build the path to the Python script
            string pythonScript = Path.Combine(baseDirectory, "GUI-Team", "graph.py");
            string quotedPythonScript = $"\"{pythonScript}\"";

            StringBuilder sb = new StringBuilder(quotedPythonScript);

            sb.Append(" -i " + $"\"{parameters.CSVFilePath}\"");
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
    }
}
