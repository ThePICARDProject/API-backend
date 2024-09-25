using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_Backend.Models;
using API_Backend.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IO;
using System;
using API_Backend.Services;
using System.Linq;

namespace API_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/dataset")]
    public class DataSetController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _environment;
        private readonly IDatasetService _datasetService;

        // Define allowed file extensions
        private readonly string[] permittedExtensions = { ".csv" };

        public DataSetController(ApplicationDbContext dbContext, IWebHostEnvironment environment, IDatasetService datasetService)
        {
            _dbContext = dbContext;
            _environment = environment;
            _datasetService = datasetService;
        }

        /// <summary>
        /// Uploads a new dataset.
        /// </summary>
        /// <param name="dto">The dataset upload information.</param>
        /// <returns>Returns a success message and the ID of the uploaded dataset.</returns>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(104857600)] // Optional: Limit file size to 100MB
        public async Task<IActionResult> UploadDataSet([FromForm] DataSetUploadDto dto)
        {
            // Validate input parameters
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Dataset name is required." });

            // Validate file extension
            var extension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || Array.IndexOf(permittedExtensions, extension) < 0)
                return BadRequest(new { message = "Unsupported file type. Only CSV files are allowed." });

            try
            {
                // Get user ID from authenticated user
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Invalid user." });

                // Define the uploads folder path (e.g., ContentRootPath/Datasets)
                string uploadsFolder = Path.Combine(_environment.ContentRootPath, "Datasets");
                Directory.CreateDirectory(uploadsFolder); // Ensure the directory exists

                // Use DatasetService to save the file
                string uniqueFileName = await _datasetService.SaveFileAsync(dto.File, uploadsFolder);

                // Store relative path
                string relativeFilePath = Path.Combine("Datasets", uniqueFileName).Replace("\\", "/");

                // Save dataset info to DB
                var dataSet = new StoredDataSet
                {
                    UserID = userIdClaim, // Use the string UserID
                    Name = dto.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                    FilePath = relativeFilePath, // Store relative path
                    UploadedAt = DateTime.UtcNow,
                };

                _dbContext.StoredDataSets.Add(dataSet);
                await _dbContext.SaveChangesAsync();

                return Ok(new 
                { 
                    message = "Dataset uploaded successfully.", 
                    dataSetID = dataSet.DataSetID 
                });
            }
            catch (Exception ex)
            {
                // Handle exception as needed
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while uploading the dataset." });
            }
        }

        /// <summary>
        /// Retrieves a dataset by its ID.
        /// </summary>
        /// <param name="id">The ID of the dataset.</param>
        /// <returns>Returns the dataset details.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDataSetById(int id)
        {
            var dataSet = await _dbContext.StoredDataSets.FindAsync(id);

            if (dataSet == null)
                return NotFound(new { message = "Dataset not found." });

            return Ok(new 
            { 
                dataSet.DataSetID,
                dataSet.UserID,
                dataSet.Name,
                dataSet.Description,
                dataSet.FilePath,
                dataSet.UploadedAt,
            });
        }
    }
}