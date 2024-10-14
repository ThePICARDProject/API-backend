

namespace API_backend.Services.FileProcessing
{
    /// <summary>
    /// Configuration options for the FileProcessorService
    /// </summary>
    public class FileProcessorOptions(string databaseFileSystemBasePath)
    {
        // Base path for the Database file system
        public string DatabaseFileSystemBasePath { get; set; } = databaseFileSystemBasePath;
    }
}
