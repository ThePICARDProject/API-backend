using API_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API_Backend.Services.FileProcessing;
using API_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;

namespace API_Backend.Controllers
{
    //[Authorize]
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
            //_experimentService = experimentService;
        }


        /// <summary>
        /// Gets the processed results of an experiment.
        /// </summary>
        [HttpGet("data/{experimentId}")]
        public async Task<IActionResult> GetProcessedResults(string experimentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User {UserID} is requesting results for ExperimentID {ExperimentID}", userId, experimentId);

            var experiment = await _experimentService.GetExperimentByIdAsync(experimentId);

            if (experiment is not { Status: ExperimentStatus.Finished })
            {
                _logger.LogWarning("Results not available for ExperimentID {ExperimentID}", experimentId);
                return NotFound(new { message = "Results not available or experiment not completed." });
            }

            // Ensure the requesting user is the owner of the experiment
            if (experiment.UserID != userId)
            {
                _logger.LogWarning("User {UserID} is not authorized to access results for ExperimentID {ExperimentID}", userId, experimentId);
                return Forbid();
            }

            // Retrieve result data
            var result = await _experimentService.GetExperimentResultAsync(experimentId);

            if (result == null)
            {
                _logger.LogWarning("Experiment results not found for ExperimentID {ExperimentID}", experimentId);
                return NotFound(new { message = "Experiment results not found." });
            }

            _logger.LogInformation("Returning results for ExperimentID {ExperimentID}", experimentId);

            // Return the result data
            return Ok(new { experimentId, result });
        }


        // TODO: update to handle other status codes and return an appropriate file path
        [HttpPost ("aggregateData")]
        public IActionResult aggregateData(string userId, string requestId, List<string> filePaths)
        {
            try
            {
                string resultFilePath = _fileProcessor.AggregateData(userId, requestId, filePaths);

                return this.Content(resultFilePath);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);



                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);

                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
            }
            

        }


        // TODO: update to handle other status codes and return an appropriate file path
        [HttpPost("createCsv")]
        public IActionResult csvCreate([FromBody] List<string> desiredMetrics, string inputFile)
        {

            try
            {
                string outputFilePath = _fileProcessor.GetCsv(desiredMetrics, inputFile);

                return this.Content(outputFilePath);
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


        /** TODO: update to handle other status codes
        Make sqlQuery a service called by getCSV controller or aggregateData controller
        Generate outputFilePath based on passed user and query for aggregateData
        Generage outputFilePath based on passed user, query, and metrics for createCSV
        Update db to store filePaths for aggregateData and CSVs, with FK to userID
        **/
        [HttpPost("sqlQuery")]
        public async Task<IActionResult> sqlQuery([FromBody] QueryExperiment request)
        {
            Console.WriteLine("inside sqlQuery");

            await _fileProcessor.sqlQuery(request.DesiredMetrics, request.QueryParams);


            return Ok();
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