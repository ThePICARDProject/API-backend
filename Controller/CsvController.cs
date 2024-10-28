using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Nodes;
using API_Backend.Services.FileProcessing;
using Microsoft.AspNetCore.Mvc.Rendering;
using API_Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using API_Backend.Controllers;
using API_Backend.Data;

namespace API_Backend.Controller
{
    [ApiController]
    [Route("[controller]")]
    public class CsvController : ControllerBase
    {

        private readonly FileProcessor _fileProcessor;
        private readonly ILogger<CsvController> _logger;
        private readonly ApplicationDbContext _dbContext;



        public CsvController (FileProcessor fileProcessor, ILogger<CsvController> logger, ApplicationDbContext dbContext)
        {
            _fileProcessor = fileProcessor;
            _logger = logger;
            _dbContext = dbContext;
        }


        // TODO: update to handle other status codes
        [HttpPost("createCsv")]
        public IActionResult csvCreate([FromBody] List<string> desiredMetrics, string inputFile, string outputFilePath)
        {
            _fileProcessor.GetCsvTest(desiredMetrics, inputFile, outputFilePath);
            
            return Ok();
        }


        // TODO: update to handle other status codes
        [HttpPost("sqlQuery")]
        public async Task<IActionResult> sqlQuery([FromBody] QueryExperiment request)
        {
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
