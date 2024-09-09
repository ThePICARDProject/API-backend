using System;
using System.Diagnostics;

namespace API_backend.Services.DataVisualization
{
    public class DataVisualization
    {
        private readonly IWebHostEnvironment _env;

        public DataVisualization(IWebHostEnvironment env)
        {
            _env = env;
        }

        public bool GraphInput(string parameters)
        {
            // TODO: Input validation, likely already handled on front end

            // Get the base directory of the application
            var baseDirectory = _env.ContentRootPath;


            Console.WriteLine($"Base directory is: {baseDirectory}");

            // Build the path to the Python script
            string pythonScript = Path.Combine(baseDirectory, "GUI-Team", "graph.py");
            string quotedPythonScript = $"\"{pythonScript}\"";

            string inputScript = Path.Combine(baseDirectory, "GUI-Team", "testdata.csv");
            string quotedInputScript = $"\"{inputScript}\"";



            string tempParams = $"-i {quotedInputScript} -d1 \"Ratio.S-SSL\" -d2 \"Recall\" -g \"line\" -o \"output.pdf\"";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "python.exe",
                Arguments = $"{quotedPythonScript} {tempParams}",
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
            return false;
        }
    }
}
