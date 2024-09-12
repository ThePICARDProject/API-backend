using API_backend.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using System.Text;

namespace API_backend.Services.DataVisualization
{
    public class DataVisualization
    {
        private readonly IWebHostEnvironment _env;

        public DataVisualization(IWebHostEnvironment env)
        {
            _env = env;
        }

        /**
         * TODO: documentation
        */
        public bool GraphInput(VisualizationRequest parameters)
        {
            // TODO: Input validation, likely already handled on front end

            // Get the base directory of the application
            var baseDirectory = _env.ContentRootPath;

            var inputFile = parameters.InputFile;

            string fileName = inputFile.FileName;

            if (Path.GetExtension(fileName) != ".csv")
            {
                fileName = Path.GetFileNameWithoutExtension(fileName) + ".csv";
            }

            var filePath = Path.Combine(baseDirectory, "InputFiles", fileName);

            // Check if the directory exists
            if (!Directory.Exists(Path.Combine(baseDirectory, "InputFiles")))
            {
                Directory.CreateDirectory(Path.Combine(baseDirectory, "InputFiles"));
            }

            // Copy input file to file path
            using (var stream = System.IO.File.Create(filePath))
            {
                inputFile.CopyToAsync(stream);
            }

            //TODO: Remove debug line
            Console.WriteLine("File path is " + filePath);

            string input = FormatInputString(parameters, filePath);


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

            //TODO: Update return value to accurately reflect result
            return false;
        }

        /**
         * @brief Formats graph.py parameters to pass to python executable file
         * @param paremeters - data visualization parameters passed to controller from input form
         * @param inputFilePath indicates filepath location
         * @return string containing Python formatted parameters
        */
        public string FormatInputString(VisualizationRequest parameters, string inputFilePath)
        {

            var baseDirectory = _env.ContentRootPath;

            // Build the path to the Python script
            string pythonScript = Path.Combine(baseDirectory, "GUI-Team", "graph.py");
            string quotedPythonScript = $"\"{pythonScript}\"";

            StringBuilder sb = new StringBuilder(quotedPythonScript);

            sb.Append(" -i " + $"\"{inputFilePath}\"");
            sb.Append(" -d1 " + parameters.XAxis);
            sb.Append(" -d2 " + parameters.YAxis);
            sb.Append(" -g " + parameters.GraphType);

            if (parameters.OutputFileName != null || parameters.OutputFileName != "")
            {
                sb.Append(" -o " + parameters.OutputFileName);
            }



            Console.WriteLine(sb.ToString());


            return sb.ToString();
        }
    }
}
