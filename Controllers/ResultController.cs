using API_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API_Backend.Services.FileProcessing;
using API_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Security;
using API_backend.Models;
using System.IO.Compression;
using System.Diagnostics;

namespace API_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/result")]
    public class ResultController: ControllerBase
    {
        private readonly FileProcessor _fileProcessor;
        private readonly ILogger<ResultController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly ExperimentService _experimentService;

        public ResultController(/*ExperimentService experimentService,*/ FileProcessor fileProcessor, ILogger<ResultController> logger, ApplicationDbContext dbContext)
        {
            _fileProcessor = fileProcessor;
            _logger = logger;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Gets the processed results of an experiment.
        /// </summary>
        [HttpGet("getProcessedResults/{aggregateDataId}")]
        public async Task<IActionResult> GetProcessedResults(int aggregateDataId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User {UserID} is requesting results for AggregateDataId {aggregateDataId}", userId, aggregateDataId);

            // Get AggregatedDataResult
            AggregatedResult result = await _dbContext.AggregatedResults.FirstOrDefaultAsync(x => x.AggregatedResultID == aggregateDataId);
            if (result is null)
                return NotFound(new { message = $"Aggregate data with the id \"{aggregateDataId}\" was not found." });

            // Ensure the requesting user is the owner of the experiment
            if (result.UserID != userId)
            {
                _logger.LogWarning("User {UserID} is not authorized to access results for AggregateDataId {AggregateDataId}", userId, aggregateDataId);
                return Forbid();
            }

            // Get Csv results
            _logger.LogInformation("Getting .csv results for AggregateDataId {aggregateDataId}", aggregateDataId);
            CsvResult csvResult = await _dbContext.CsvResults.FirstOrDefaultAsync(x => x.AggregatedResultID == result.AggregatedResultID);
            if (csvResult is null)
                _logger.LogInformation("Csv result not found for AggregateDataId {aggregateDataId}", aggregateDataId);

            // Get zip data
            _logger.LogInformation("Zipping results files.");
            string zipPath = _fileProcessor.GetZippedResults(result);
            string fileName = Path.GetFileName(zipPath);
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(zipPath);

            // Cleanup zip file
            Directory.Delete(Path.GetDirectoryName(zipPath), true);

            _logger.LogInformation("Returning results for AggregateDataId {aggregateDataId}", aggregateDataId);
            return File(fileBytes, "application/octet-stream", fileName);
        }

        /// <summary>
        /// Controller handling returning an aggregate result file based off a user specified list of db queries
        /// </summary>
        /// <param name="queryParams"> List of docker swarm and algorithm parameters to query db </param>
        /// <returns> Aggregated result file path </returns>
        /// <exception cref="SecurityException"></exception>
        [HttpPost ("aggregateData")]
        public async Task<IActionResult> AggregateData(QueryExperiment queryParams)
        {
            try
            {
                // Get userId from logged in user
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userId == null)
                    throw new SecurityException("User not logged in");

                _logger.LogInformation("User {UserID} is aggregating experiment results", userId);

                // List of experiment request IDs that match passed query params
                HashSet<string> requestIds = await _fileProcessor.QueryExperiments(userId, queryParams);

                // File path of aggregate data file composed of concatenated results file associated with passed experiment request IDs
                int aggregateDataId = await _fileProcessor.AggregateData(userId, requestIds.ToList());

                return Ok(new { AggregateDataId = aggregateDataId });
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, new { message = "File not found.", details = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Returns a CSV file path.  CSV is generated by parsing an aggregated data file for user specified data.  The CSV file is primarily used for data visualization.
        /// </summary>
        /// <param name="desiredMetrics"> User specified metrics to be parsed from the aggregated data file </param>
        /// <param name="aggregateDataId"> Path to the aggregated data file </param>
        /// <returns> Path to the CSV file </returns>
        [HttpPost("createCsv")]
        public IActionResult CreateCsv([FromBody] CreateCsvRequest request)
        {
            try
            {
                int csvId = _fileProcessor.GetCsv(request.MetricsIdentifiers, request.AggregateDataId);
                return Ok(new { CsvResultId = csvId });
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("Aggregated data file not found: " + ex.Message);
                return NotFound("Aggregated data file not found");
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);

                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
            }
        }

        [HttpGet("DockerSwarmParams")]
        public async Task<IActionResult> GetAllDockerSwarmParams()
        {
            try
            {
                var dockerSwarmParams = await _dbContext.ClusterParameters.ToListAsync();
                return Ok(dockerSwarmParams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MySQL error: {Message} - Inner Exception: {InnerException}", ex.Message, ex.InnerException?.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving docker swarm params." });
            }
        }
    }
}