using API_backend.Models;
using API_Backend.Data;
using API_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using System.IO;

namespace API_backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AlgorithmController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AlgorithmController> _logger;
        private readonly IWebHostEnvironment _environment;

        public AlgorithmController(ApplicationDbContext dbContext, ILogger<AlgorithmController> logger, IWebHostEnvironment environment)
        {
            _dbContext = dbContext;
            _environment = environment;
            _logger = logger;
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
            _logger.LogInformation($"Handling algorithm upload for {dto} from user {userId}");

            if(dto.JarFile.Length == 0)
            {
                _logger.LogWarning("No file uploaded.");
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

                // Save Algorithm parameters
                foreach(AlgorithmParameterUploadDto paramDto in parameters)
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
                    _dbContext.Add(algorithmParameter);
                }

                // Add algorithm and save changes
                _dbContext.Algorithms.Add(Algorithm);
                await _dbContext.SaveChangesAsync();

                // Return OK
                return Ok(new
                {
                    message = "Dataset uploaded successfully.",
                    algorithmId = Algorithm.AlgorithmID
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occured while uploading algorithm {dto.AlgorithmName}");
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
    }
}
