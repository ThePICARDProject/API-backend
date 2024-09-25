using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace API_Backend.Services
{
    /// <summary>
    /// Interface for dataset-related file operations.
    /// </summary>
    public interface IDatasetService
    {
        /// <summary>
        /// Saves the uploaded file to the specified directory with a unique name.
        /// </summary>
        /// <param name="file">The uploaded file.</param>
        /// <param name="uploadsFolder">The directory to save the file.</param>
        /// <returns>The unique file name.</returns>
        Task<string> SaveFileAsync(IFormFile file, string uploadsFolder);
    }

    /// <summary>
    /// Service for handling dataset file operations.
    /// </summary>
    public class DatasetService : IDatasetService
    {
        public async Task<string> SaveFileAsync(IFormFile file, string uploadsFolder)
        {
            // Ensure the uploads folder exists
            Directory.CreateDirectory(uploadsFolder);

            // Generate a unique file name to prevent overwriting
            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

            // Combine the folder with the unique file name
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save the file asynchronously
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return uniqueFileName; // Return the unique file name for storage
        }
    }
}