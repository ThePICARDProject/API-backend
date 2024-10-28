using System.Diagnostics;
using API_Backend.Data;
using API_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API_backend.Services.FileProcessing;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace API_Backend.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/dataset")]
    public class DataSetController : ControllerBase
    {
        private readonly IDatasetService _datasetService;
        private readonly ILogger<DataSetController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _environment;

        // Define allowed file extensions
        private readonly string[] _permittedExtensions = { ".csv" };

        public DataSetController(
            ApplicationDbContext dbContext,
            IWebHostEnvironment environment,
            IDatasetService datasetService,
            ILogger<DataSetController> logger)
        {
            _dbContext = dbContext;
            _environment = environment;
            _datasetService = datasetService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all datasets associated with the authenticated user.
        /// </summary>
        /// <returns>Returns a list of datasets with name, description, and download links.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllDatasets()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User {UserID} is retrieving all datasets.", userId);

            try
            {
                var datasets = await _dbContext.StoredDataSets
                    .Where(ds => ds.UserID == userId)
                    .Select(ds => new
                    {
                        ds.DataSetID,
                        ds.Name,
                        ds.Description,
                        ds.FilePath,
                        ds.UploadedAt,
                        DownloadUrl = Url.Action("DownloadDataset", "DataSet", new { id = ds.DataSetID }, Request.Scheme)
                    })
                    .ToListAsync();

                _logger.LogInformation("User {UserID} retrieved {Count} datasets.", userId, datasets.Count);

                return Ok(datasets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving datasets for user {UserID}.", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving datasets." });
            }
        }

        /// <summary>
        /// Retrieves a specific dataset by its ID.
        /// </summary>
        /// <param name="id">The ID of the dataset.</param>
        /// <returns>Returns the dataset details.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDataSetById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User {UserID} is retrieving dataset with ID {DataSetID}.", userId, id);

            try
            {
                var dataSet = await _dbContext.StoredDataSets.FindAsync(id);

                if (dataSet == null)
                {
                    _logger.LogWarning("Dataset with ID {DataSetID} not found for user {UserID}.", id, userId);
                    return NotFound(new { message = "Dataset not found." });
                }

                if (dataSet.UserID != userId)
                {
                    _logger.LogWarning("User {UserID} is not authorized to access dataset {DataSetID}.", userId, id);
                    return Forbid();
                }

                _logger.LogInformation("User {UserID} successfully retrieved dataset {DataSetID}.", userId, id);

                return Ok(new
                {
                    dataSet.DataSetID,
                    dataSet.Name,
                    dataSet.Description,
                    dataSet.FilePath,
                    dataSet.UploadedAt,
                    DownloadUrl = Url.Action("DownloadDataset", "DataSet", new { id = dataSet.DataSetID }, Request.Scheme)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dataset {DataSetID} for user {UserID}.", id, userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving the dataset." });
            }
        }

        /// <summary>
        /// Downloads the specified dataset.
        /// </summary>
        /// <param name="id">The ID of the dataset to download.</param>
        /// <returns>Returns the dataset file for download.</returns>
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadDataset(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User {UserID} is attempting to download dataset {DataSetID}.", userId, id);

            try
            {
                var dataSet = await _dbContext.StoredDataSets.FindAsync(id);

                if (dataSet == null)
                {
                    _logger.LogWarning("Dataset with ID {DataSetID} not found for download by user {UserID}.", id, userId);
                    return NotFound(new { message = "Dataset not found." });
                }

                if (dataSet.UserID != userId)
                {
                    _logger.LogWarning("User {UserID} is not authorized to download dataset {DataSetID}.", userId, id);
                    return Forbid();
                }

                var filePath = Path.Combine(_environment.ContentRootPath, dataSet.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("File {FilePath} does not exist for dataset {DataSetID}.", filePath, id);
                    return NotFound(new { message = "File not found." });
                }

                var fileName = Path.GetFileName(filePath);
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                _logger.LogInformation("User {UserID} successfully downloaded dataset {DataSetID}.", userId, id);

                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading dataset {DataSetID} for user {UserID}.", id, userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while downloading the dataset." });
            }
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
            _logger.LogInformation("Handling regular dataset upload for {FileName} from user {UserID}.", dto.File.FileName, userId);

            // Validate input parameters
            if (dto.File.Length == 0)
            {
                _logger.LogWarning("No file uploaded.");
                return BadRequest(new { message = "No file uploaded." });
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                _logger.LogWarning("Dataset name is required.");
                return BadRequest(new { message = "Dataset name is required." });
            }

            // Validate file extension
            var extension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !_permittedExtensions.Contains(extension))
            {
                _logger.LogWarning("Unsupported file type: {Extension}.", extension);
                return BadRequest(new { message = "Unsupported file type. Only CSV files are allowed." });
            }

            try
            {
                // Define uploads folder
                string uploadsFolder = Path.Combine(_environment.ContentRootPath, "Datasets");
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
                    UserID = userId,
                    Name = dto.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                    FilePath = relativeFilePath,
                    UploadedAt = DateTime.UtcNow,
                };

                _dbContext.StoredDataSets.Add(dataSet);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Dataset {DatasetName} uploaded successfully by user {UserID}.", dataSet.Name, userId);

                return Ok(new
                {
                    message = "Dataset uploaded successfully.",
                    dataSetID = dataSet.DataSetID
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while uploading dataset {DatasetName}.", dto.Name);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while uploading the dataset." });
            }
        }

        /// <summary>
        /// Handles a chunked file upload.
        /// </summary>
        private async Task<IActionResult> HandleChunkedUpload(DataSetUploadDto dto, string uploadId, string chunkNumberStr, string totalChunksStr)
        {
            _logger.LogInformation("Handling chunked upload: UploadId={UploadId}, ChunkNumber={ChunkNumber}, TotalChunks={TotalChunks}.", uploadId, chunkNumberStr, totalChunksStr);

            // Parse chunk numbers
            if (!int.TryParse(chunkNumberStr, out int chunkNumber) ||
                !int.TryParse(totalChunksStr, out int totalChunks))
            {
                _logger.LogWarning("Invalid chunk number or total chunks.");
                return BadRequest(new { message = "Invalid chunk number or total chunks." });
            }

            // Validate input parameters
            if (dto.File.Length == 0)
            {
                _logger.LogWarning("No file uploaded in chunk {ChunkNumber}.", chunkNumber);
                return BadRequest(new { message = "No file uploaded." });
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                _logger.LogWarning("Dataset name is required for chunk {ChunkNumber}.", chunkNumber);
                return BadRequest(new { message = "Dataset name is required." });
            }

            // Validate file extension
            var extension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !_permittedExtensions.Contains(extension))
            {
                _logger.LogWarning("Unsupported file type in chunk {ChunkNumber}: {Extension}.", chunkNumber, extension);
                return BadRequest(new { message = "Unsupported file type. Only CSV files are allowed." });
            }

            try
            {
                // Define temporary uploads folder for the uploadId
                string uploadsFolder = Path.Combine(_environment.ContentRootPath, "TempUploads", uploadId);
                Directory.CreateDirectory(uploadsFolder);

                // Save the chunk to a temporary file
                string chunkFilePath = Path.Combine(uploadsFolder, $"chunk_{chunkNumber}");
                await using (var fileStream = new FileStream(chunkFilePath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(fileStream);
                }

                _logger.LogInformation("Chunk {ChunkNumber} saved for UploadId {UploadId}.", chunkNumber, uploadId);

                // Check if all chunks have been uploaded
                var uploadedChunks = Directory.GetFiles(uploadsFolder, "chunk_*").Length;
                if (uploadedChunks == totalChunks)
                {
                    _logger.LogInformation("All chunks uploaded for UploadId={UploadId}. Assembling file.", uploadId);

                    // All chunks have been uploaded, assemble the file
                    string datasetsFolder = Path.Combine(_environment.ContentRootPath, "Datasets");
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
                            _logger.LogInformation("Appended chunk {ChunkNumber} to final file.", i);
                        }
                    }

                    _logger.LogInformation("File assembled for UploadId {UploadId}.", uploadId);

                    // Clean up the chunks
                    Directory.Delete(uploadsFolder, true);
                    _logger.LogInformation("Temporary chunks deleted for UploadId={UploadId}.", uploadId);

                    // Store relative path
                    string relativeFilePath = Path.Combine("Datasets", uniqueFileName).Replace("\\", "/");

                    // Save dataset info to DB
                    var dataSet = new StoredDataSet
                    {
                        UserID = User.FindFirstValue(ClaimTypes.NameIdentifier),
                        Name = dto.Name.Trim(),
                        Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                        FilePath = relativeFilePath,
                        UploadedAt = DateTime.UtcNow,
                    };

                    _dbContext.StoredDataSets.Add(dataSet);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("Dataset {DatasetName} uploaded successfully by user {UserID} via chunked upload.", dataSet.Name, dataSet.UserID);

                    return Ok(new
                    {
                        message = "Dataset uploaded successfully.",
                        dataSetID = dataSet.DataSetID
                    });
                }
                else
                {
                    // Chunk uploaded successfully, wait for more chunks
                    _logger.LogInformation("Chunk {ChunkNumber}/{TotalChunks} uploaded for UploadId={UploadId}.", chunkNumber, totalChunks, uploadId);
                    return Ok(new { message = $"Chunk {chunkNumber} uploaded successfully." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while uploading chunk {ChunkNumber} for UploadId={UploadId}.", chunkNumberStr, uploadId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while uploading the dataset." });
            }
        }
    }
}