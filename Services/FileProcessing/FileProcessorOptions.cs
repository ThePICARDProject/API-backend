using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace API_backend.Services.FileProcessing
{
    /// <summary>
    /// Configuration options for the FileProcessorService
    /// </summary>
    public class FileProcessorOptions
    {
        // Base path for the Database file system
        public string OutputBasePath { get; set; }

        // Base path for docker-swarm
        public string RepositoryBasePath { get; set; }
    }
}
