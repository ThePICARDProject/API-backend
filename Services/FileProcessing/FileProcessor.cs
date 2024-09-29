using System.Diagnostics;


namespace API_Backend.Services.FileProcessing
{
    /// <summary>
    /// Service for aggregating experiment data and parsing the data into a .csv file.
    /// </summary>
    public class FileProcessor
    {
        private readonly string _databaseFileSystemBasePath;
        private readonly ILogger<FileProcessor> _logger;

        public FileProcessor(FileProcessorOptions fileProcessorOptions, ILogger<FileProcessor> logger)
        {
            _logger = logger;

            _logger.LogInformation("Initializing FileProcessor with base path: {BasePath}", fileProcessorOptions.DatabaseFileSystemBasePath);

            // Initialize the base path for the database filesystem and verify it exists
            _databaseFileSystemBasePath = fileProcessorOptions.DatabaseFileSystemBasePath;
            if (string.IsNullOrEmpty(_databaseFileSystemBasePath))
            {
                _logger.LogError("DatabaseFileSystemBasePath is null or empty.");
                throw new ArgumentNullException(nameof(fileProcessorOptions.DatabaseFileSystemBasePath));
            }
            if (!Directory.Exists(_databaseFileSystemBasePath))
            {
                _logger.LogError("The output directory \"{OutputDir}\" could not be found or does not exist.", _databaseFileSystemBasePath);
                throw new DirectoryNotFoundException($"The output directory \"{_databaseFileSystemBasePath}\" could not be found or does not exist");
            }
        }

        /// <summary>
        /// Aggregates all data within the HDFS into a single output text file within the database filesystem.
        /// </summary>
        public string AggregateData(string userId, string algorithmName, string survey)
        {
            _logger.LogInformation("Aggregating data for UserID {UserID}, Algorithm {AlgorithmName}, Survey {Survey}", userId, algorithmName, survey);

            // Verify arguments
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Argument {Argument} is null or empty.", nameof(userId));
                throw new ArgumentNullException(nameof(userId));
            }
            if (string.IsNullOrEmpty(algorithmName))
            {
                _logger.LogError("Argument {Argument} is null or empty.", nameof(algorithmName));
                throw new ArgumentNullException(nameof(algorithmName));
            }
            if (string.IsNullOrEmpty(survey))
            {
                _logger.LogError("Argument {Argument} is null or empty.", nameof(survey));
                throw new ArgumentNullException(nameof(survey));
            }

            try
            {
                // Generate the aggregate file path
                string aggregateFilePath = Path.Combine(_databaseFileSystemBasePath, userId, algorithmName, $"{survey}.txt");

                _logger.LogInformation("Aggregate file path: {AggregateFilePath}", aggregateFilePath);

                // Ensure the directory exists
                var directoryPath = Path.GetDirectoryName(aggregateFilePath);
                if (directoryPath != null) Directory.CreateDirectory(directoryPath);

                // Interface with HDFS to get files and output to the database filesystem base path
                using (Process resultsOut = new Process())
                {
                    resultsOut.StartInfo.FileName = "/bin/sh";
                    // This is the command run by results-out.sh, with the output path substituted with the database path
                    resultsOut.StartInfo.Arguments = $"-c \"docker run --rm --name results-extractor --network $(basename $(pwd))_cluster-network -v $(pwd)/results:/mnt/results spark-hadoop:latest hdfs dfs -getmerge /data/results/{algorithmName}/output {aggregateFilePath}\"";
                    resultsOut.StartInfo.RedirectStandardOutput = true;
                    resultsOut.StartInfo.RedirectStandardError = true;
                    resultsOut.StartInfo.UseShellExecute = false;
                    resultsOut.StartInfo.CreateNoWindow = true;

                    _logger.LogInformation("Starting process to aggregate data.");

                    resultsOut.Start();
                    
                    var error = resultsOut.StandardError.ReadToEnd();

                    resultsOut.WaitForExit();

                    if (resultsOut.ExitCode != 0)
                    {
                        _logger.LogError("Error aggregating data: {Error}", error);
                        throw new Exception($"Error aggregating data: {error}");
                    }

                    _logger.LogInformation("Data aggregation process completed successfully.");
                }

                // Return the path of the saved file
                return aggregateFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while aggregating data.");
                throw;
            }
        }

        /// <summary>
        /// Generates a CSV file containing formatted results.
        /// NOTE: AggregateData must be called before this method.
        /// </summary>
        public string GetCsv()
        {
            _logger.LogInformation("Generating CSV file from aggregated data.");

            try
            {
                // Implement CSV generation logic here
                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while generating CSV file.");
                throw;
            }
        }
    }

    /// <summary>
    /// Options for configuring the FileProcessor.
    /// </summary>
    public abstract class FileProcessorOptions(string databaseFileSystemBasePath)
    {
        public string DatabaseFileSystemBasePath { get; set; } = databaseFileSystemBasePath;
    }
}