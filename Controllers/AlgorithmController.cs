using API_backend.Models;
using API_Backend.Data;
using API_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace API_backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/algorithms")]
    public class AlgorithmController(ApplicationDbContext dbContext, ILogger<AlgorithmController> logger) : ControllerBase
    {

        private readonly IWebHostEnvironment _environment;

        public AlgorithmController(ApplicationDbContext dbContext, ILogger<AlgorithmController> logger, IWebHostEnvironment environment) : this(dbContext, logger)
        {
            _environment = environment;
        }

        [HttpPost]
        [Route("upload")]
        public async Task<IActionResult> UploadAlgorithm([FromForm] AlgorithmUploadDto dto)
        {
            // Get paramater data
            List<AlgorithmParameterUploadDto> parameters = JsonSerializer.Deserialize<List<AlgorithmParameterUploadDto>>(dto.Parameters);
            
            return await HandleUpload(dto, parameters);
        }

        /// <summary>
        /// Handles the upload of a JAR file in the database and filesystem.
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<IActionResult> HandleUpload(AlgorithmUploadDto dto, List<AlgorithmParameterUploadDto> parameters)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            logger.LogInformation($"Handling algorithm upload for {dto} from user {userId}");

            if(dto.JarFile.Length == 0)
            {
                logger.LogWarning("No file uploaded.");
                return BadRequest(new { message = "No file uploaded."});
            }
            
            /* Further validation here */
            
            try
            {
                // Check that the docker-images path exists
                string algorithmFolder = Path.Combine(_environment.ContentRootPath, "docker-images", "spark-hadoop");
                if (!Directory.Exists(algorithmFolder))
                    throw new Exception("docker-images folder does not exist in the applications root directory");

                // Create the user folder
                algorithmFolder = Path.Combine(algorithmFolder, "jars", userId);
                if (!Directory.Exists(algorithmFolder))
                    Directory.CreateDirectory(algorithmFolder);

                // Get the full filepath
                string filePath = Path.Combine(algorithmFolder, Path.GetFileName(dto.JarFile.FileName));
                if (System.IO.File.Exists(filePath))
                    throw new Exception($"Jar file with the name {dto.JarFile.FileName} already exists");

                // Save the file
                await using(var filestream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.JarFile.CopyToAsync(filestream);
                }
                string relativeFilePath = Path.Combine("docker-images", "spark-hadoop", "jars", userId, Path.GetFileName(dto.JarFile.FileName));

                // Save Algorithm in DB
                var Algorithm = new Algorithm
                {
                    UserID = userId,
                    AlgorithmName = dto.AlgorithmName,
                    MainClassName = dto.MainClassName,
                    AlgorithmType = dto.AlgorithmType,
                    JarFilePath = relativeFilePath,
                    UploadedAt = DateTime.Now,
                    Parameters = new List<AlgorithmParameter>(),
                    ExperimentRequests = new List<ExperimentRequest>()
                };
                dbContext.Algorithms.Add(Algorithm);

                // Save Algorithm parameters
                foreach (AlgorithmParameterUploadDto paramDto in parameters)
                {
                    ValidateAlgorithmParameterData(paramDto);
                    var algorithmParameter = new AlgorithmParameter
                    {
                        AlgorithmID = Algorithm.AlgorithmID,
                        ParameterName = paramDto.ParameterName,
                        DriverIndex = paramDto.DriverIndex,
                        DataType = paramDto.DataType,
                        Algorithm = Algorithm,
                    };
                    Algorithm.Parameters.Add(algorithmParameter);
                    dbContext.Add(algorithmParameter);
                }

                // Add algorithm and save changes
                await dbContext.SaveChangesAsync();

                // Return OK
                return Ok(new
                {
                    message = "Dataset uploaded successfully.",
                    algorithmId = Algorithm.AlgorithmID
                });

            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occured while uploading algorithm {dto.AlgorithmName}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while uploading the algorithm." });
            }
        }
    
        private void ValidateAlgorithmParameterData(AlgorithmParameterUploadDto param)
        {
            switch(param.DataType)
            {
                case "int": break;
                case "string": break;
                case "boolean": break;
                default:
                    throw new FormatException("DataType must have the pattern \"int\", \"string\", \"boolean\"");
            }
            if (param.DriverIndex < 0)
                throw new ArgumentOutOfRangeException("Driver index must be greater than or equal to 0.");
        }
    

        /// <summary>
        /// Retrieves a Algorithm set by its Users ID.
        /// </summary>
        /// <param name="id">The ID of the Algorithm.</param>
        /// <returns>Returns the Algorithm IDs and Names for given User.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAlgorithmById(int id)
        {
            // Check current user 
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            logger.LogInformation("User {userID} Retrieving algorithm with ID {AlgorithmID}", userId, id);


            // Fetch the AlgorithmSet from the database
            var AlgorithmSet = await dbContext.Algorithms
                                              .Where(a => a.UserID == userId)  // Filter by user ID
                                              .ToListAsync();  // Fetch all matching records as a list


            // Check if the AlgorithmSet is empty, return 404
            if (AlgorithmSet == null)
            {
                logger.LogWarning("Algorithms for user with ID: {ID} not found for user's request.", id);
                return NotFound(new { message = "Algorithms not found." });
            }

            //iterate through each received algo in list and verify its assigned userID
            foreach (var algorithm in AlgorithmSet)
            {
                if (algorithm.UserID != userId)
                {
                    // Log a warning and return 403 Forbidden for unauthorized access
                    logger.LogWarning("User {userID} is not authorized to access this algorithm {AlgorithmID}", userId, algorithm.AlgorithmID);
                    return Forbid();
                }
            }

            var listOfAlgorithmIDsAndNames = AlgorithmSet
                .Select(a => new { a.AlgorithmID, a.AlgorithmName })  // Pair AlgorithmID with Name
                .ToList();

            // Convert the list of algorithm IDs and names to a formatted string
            var algorithmIDNameString = string.Join(", ", listOfAlgorithmIDsAndNames.Select(a => $"{a.AlgorithmID} ({a.AlgorithmName})"));

            //now log authorization of user to access all algorithms in returned set, with created a string 
            logger.LogInformation("User {UserID} is authorized to access all algorithms in returned set: {algorithmIDNameString}", userId, algorithmIDNameString);


            return Ok(listOfAlgorithmIDsAndNames);
        }
    }
}
