

namespace API_Backend.Services.FileProcessing
{
    /// <summary>
    /// Interface for dataset-related file operations.
    /// </summary>
    public interface IDatasetService
    {
        Task SaveChunkAsync(IFormFile file, string chunkFilePath);
        Task CombineChunksAsync(string sessionFolder, string finalFilePath, string extension, int totalChunks);
    }

    /// <summary>
    /// Service for handling dataset file operations.
    /// </summary>
    public class DatasetService : IDatasetService
    {
        private readonly ILogger<DatasetService> _logger;

        public DatasetService(ILogger<DatasetService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Saves a single chunk of the uploaded file.
        /// </summary>
        public async Task SaveChunkAsync(IFormFile file, string chunkFilePath)
        {
            _logger.LogInformation("Saving chunk to {ChunkFilePath}", chunkFilePath);

            try
            {
                using (var stream = new FileStream(chunkFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("Chunk saved successfully to {ChunkFilePath}", chunkFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving chunk to {ChunkFilePath}", chunkFilePath);
                throw;
            }
        }

        /// <summary>
        /// Combines all chunks into the final file.
        /// </summary>
        public async Task CombineChunksAsync(string sessionFolder, string finalFilePath, string extension, int totalChunks)
        {
            _logger.LogInformation("Combining {TotalChunks} chunks from {SessionFolder} into {FinalFilePath}", totalChunks, sessionFolder, finalFilePath);

            try
            {
                // Ensure the final directory exists
                string? finalDirectory = Path.GetDirectoryName(finalFilePath);
                if (finalDirectory != null) Directory.CreateDirectory(finalDirectory);

                // Create or overwrite the final file
                using (var finalStream = new FileStream(finalFilePath, FileMode.Create))
                {
                    for (int i = 1; i <= totalChunks; i++)
                    {
                        string currentChunkPath = Path.Combine(sessionFolder, $"chunk_{i}{extension}");

                        if (!File.Exists(currentChunkPath))
                        {
                            _logger.LogError("Missing chunk file: {ChunkFilePath}", currentChunkPath);
                            throw new FileNotFoundException($"Missing chunk file: {currentChunkPath}");
                        }

                        using (var chunkStream = new FileStream(currentChunkPath, FileMode.Open))
                        {
                            await chunkStream.CopyToAsync(finalStream);
                        }

                        _logger.LogInformation("Chunk {ChunkNumber} appended to final file.", i);
                    }
                }

                // Optionally, delete the session folder after combining
                Directory.Delete(sessionFolder, true);

                _logger.LogInformation("Chunks combined successfully into {FinalFilePath}", finalFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while combining chunks into {FinalFilePath}", finalFilePath);
                throw;
            }
        }
    }
}