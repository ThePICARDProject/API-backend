

using System.Diagnostics;
using API_Backend.Data;
using API_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API_backend.Services.FileProcessing;
using System.Linq;

namespace API_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/dataset")]
    public class DataSetController(
        ApplicationDbContext dbContext,
        IWebHostEnvironment environment,
        IDatasetService datasetService,
        ILogger<DataSetController> logger)
        : ControllerBase
    {
        private readonly IDatasetService _datasetService = datasetService;

        // Define allowed file extensions
        private readonly string[] _permittedExtensions = [".csv"];

        /// <summary>
        /// Retrieves a dataset by its ID.
        /// </summary>
        /// <param name="id">The ID of the dataset.</param>
        /// <returns>Returns the dataset details.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDataSetById(int id)
        {
            // Check if the current user is authorized to access this dataset
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            logger.LogInformation("User {userID} Retrieving dataset with ID {DataSetID}", userId, id);

            var dataSet = await dbContext.StoredDataSets.FindAsync(id);

            if (dataSet == null)
            {
                logger.LogWarning("Dataset with ID {DataSetID} not found from {userID}'s request.", id, userId);
                return NotFound(new { message = "Dataset not found." });
            }

            if (dataSet.UserID != userId)
            {
                logger.LogWarning("User {userID} is not authorized to access dataset {DataSetID}", userId, id);
                return Forbid();
            }

            logger.LogInformation("{userID} Dataset {DataSetID} retrieved successfully.", userId, id);

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

        /// <summary>
        /// Uploads a new dataset or a chunk of a dataset.
        /// </summary>
        /// <param name="dto">The dataset upload information.</param>
        /// <returns>Returns a success message and the ID of the uploaded dataset.</returns>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        [RequestSizeLimit(long.MaxValue)] // Set the request size limit to the maximum allowed
        public async Task<IActionResult> UploadDataSet([FromForm] DataSetUploadDto dto)
        {
            // Extract optional chunking parameters
            var uploadId = Request.Form["UploadId"].FirstOrDefault();
            var chunkNumberStr = Request.Form["ChunkNumber"].FirstOrDefault();
            var totalChunksStr = Request.Form["TotalChunks"].FirstOrDefault();

            // Determine if this is a chunked upload
            bool isChunkedUpload = !string.IsNullOrEmpty(uploadId) &&
                                   !string.IsNullOrEmpty(chunkNumberStr) &&
                                   !string.IsNullOrEmpty(totalChunksStr);

            if (isChunkedUpload)
            {
                // Handle chunked upload
                Debug.Assert(uploadId != null, nameof(uploadId) + " != null");
                Debug.Assert(chunkNumberStr != null, nameof(chunkNumberStr) + " != null");
                Debug.Assert(totalChunksStr != null, nameof(totalChunksStr) + " != null");
                return await HandleChunkedUpload(dto, uploadId, chunkNumberStr, totalChunksStr);
            }
            else
            {
                // Handle normal (non-chunked) upload
                return await HandleRegularUpload(dto);
            }
        }

        /// <summary>
        /// Handles a regular (non-chunked) file upload.
        /// </summary>
        private async Task<IActionResult> HandleRegularUpload(DataSetUploadDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            logger.LogInformation("Handling regular dataset upload for {FileName} from user {userID} ", dto.File.FileName, userId);

            // Validate input parameters
            if (dto.File.Length == 0)
            {
                logger.LogWarning("No file uploaded.");
                return BadRequest(new { message = "No file uploaded." });
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                logger.LogWarning("Dataset name is required.");
                return BadRequest(new { message = "Dataset name is required." });
            }

            // Validate file extension
            var extension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !_permittedExtensions.Contains(extension))
            {
                logger.LogWarning("Unsupported file type: {Extension}", extension);
                return BadRequest(new { message = "Unsupported file type. Only CSV files are allowed." });
            }

            try
            {
                // Get user ID
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    logger.LogWarning("Invalid user.");
                    return Unauthorized(new { message = "Invalid user." });
                }

                // Define uploads folder
                string uploadsFolder = Path.Combine(environment.ContentRootPath, "Datasets");
                Directory.CreateDirectory(uploadsFolder); // Ensure the directory exists

                // Generate unique file name
                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(dto.File.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                await using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(fileStream);
                }

                // Store relative path
                string relativeFilePath = Path.Combine("Datasets", uniqueFileName).Replace("\\", "/");

                // Save dataset info to DB
                var dataSet = new StoredDataSet
                {
                    UserID = userIdClaim,
                    Name = dto.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                    FilePath = relativeFilePath,
                    UploadedAt = DateTime.UtcNow,
                };

                dbContext.StoredDataSets.Add(dataSet);
                await dbContext.SaveChangesAsync();

                logger.LogInformation("Dataset {DatasetName} uploaded successfully by user {UserID}", dataSet.Name, userIdClaim);

                return Ok(new
                {
                    message = "Dataset uploaded successfully.",
                    dataSetID = dataSet.DataSetID
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while uploading dataset {DatasetName}", dto.Name);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while uploading the dataset." });
            }
        }

        /// <summary>
        /// Handles a chunked file upload.
        /// </summary>
        private async Task<IActionResult> HandleChunkedUpload(DataSetUploadDto dto, string uploadId, string chunkNumberStr, string totalChunksStr)
        {
            logger.LogInformation("Handling chunked upload: UploadId={UploadId}, ChunkNumber={ChunkNumber}, TotalChunks={TotalChunks}", uploadId, chunkNumberStr, totalChunksStr);

            // Parse chunk numbers
            if (!int.TryParse(chunkNumberStr, out int chunkNumber) ||
                !int.TryParse(totalChunksStr, out int totalChunks))
            {
                logger.LogWarning("Invalid chunk number or total chunks.");
                return BadRequest(new { message = "Invalid chunk number or total chunks." });
            }

            // Validate input parameters
            if (dto.File.Length == 0)
            {
                logger.LogWarning("No file uploaded.");
                return BadRequest(new { message = "No file uploaded." });
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                logger.LogWarning("Dataset name is required.");
                return BadRequest(new { message = "Dataset name is required." });
            }

            // Validate file extension
            var extension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !_permittedExtensions.Contains(extension))
            {
                logger.LogWarning("Unsupported file type: {Extension}", extension);
                return BadRequest(new { message = "Unsupported file type. Only CSV files are allowed." });
            }

            try
            {
                // Get user ID
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    logger.LogWarning("Invalid user.");
                    return Unauthorized(new { message = "Invalid user." });
                }

                // Define temporary uploads folder for the uploadId
                string uploadsFolder = Path.Combine(environment.ContentRootPath, "TempUploads", uploadId);
                Directory.CreateDirectory(uploadsFolder);

                // Save the chunk to a temporary file
                string chunkFilePath = Path.Combine(uploadsFolder, $"chunk_{chunkNumber}");
                await using (var fileStream = new FileStream(chunkFilePath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(fileStream);
                }

                logger.LogInformation("Chunk {ChunkNumber} saved for UploadId {UploadId}", chunkNumber, uploadId);

                // Check if all chunks have been uploaded
                var uploadedChunks = Directory.GetFiles(uploadsFolder, "chunk_*").Length;
                if (uploadedChunks == totalChunks)
                {
                    logger.LogInformation("All chunks uploaded for UploadId={UploadId}. Assembling file.", uploadId);

                    // All chunks have been uploaded, assemble the file
                    string datasetsFolder = Path.Combine(environment.ContentRootPath, "Datasets");
                    Directory.CreateDirectory(datasetsFolder); // Ensure the directory exists

                    string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(dto.File.FileName)}";
                    string finalFilePath = Path.Combine(datasetsFolder, uniqueFileName);

                    using (var finalFileStream = new FileStream(finalFilePath, FileMode.Create))
                    {
                        for (int i = 1; i <= totalChunks; i++)
                        {
                            string chunkPath = Path.Combine(uploadsFolder, $"chunk_{i}");
                            using (var chunkStream = new FileStream(chunkPath, FileMode.Open))
                            {
                                await chunkStream.CopyToAsync(finalFileStream);
                            }
                        }
                    }

                    logger.LogInformation("File assembled for UploadId {UploadId}", uploadId);

                    // Clean up the chunks
                    Directory.Delete(uploadsFolder, true);

                    // Store relative path
                    string relativeFilePath = Path.Combine("Datasets", uniqueFileName).Replace("\\", "/");

                    // Save dataset info to DB
                    var dataSet = new StoredDataSet
                    {
                        UserID = userIdClaim,
                        Name = dto.Name.Trim(),
                        Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                        FilePath = relativeFilePath,
                        UploadedAt = DateTime.UtcNow,
                    };

                    dbContext.StoredDataSets.Add(dataSet);
                    await dbContext.SaveChangesAsync();

                    logger.LogInformation("Dataset {DatasetName} uploaded successfully by user {UserID}", dataSet.Name, userIdClaim);

                    return Ok(new
                    {
                        message = "Dataset uploaded successfully.",
                        dataSetID = dataSet.DataSetID
                    });
                }
                else
                {
                    // Chunk uploaded successfully, wait for more chunks
                    logger.LogInformation("Chunk {ChunkNumber}/{TotalChunks} uploaded for UploadId={UploadId}", chunkNumber, totalChunks, uploadId);
                    return Ok(new { message = $"Chunk {chunkNumber} uploaded successfully." });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while uploading chunk {ChunkNumber} for UploadId={UploadId}", chunkNumberStr, uploadId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while uploading the dataset." });
            }
        }
    }
}