using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Nodes;
using API_Backend.Services.FileProcessing;
using Microsoft.AspNetCore.Mvc.Rendering;
using API_Backend.Models;

namespace API_Backend.Controller
{
    [ApiController]
    [Route("[controller]")]
    public class CsvController : ControllerBase
    {

        private readonly FileProcessor _fileProcessor;

        
        public CsvController (FileProcessor fileProcessor)
        {
            _fileProcessor = fileProcessor;
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
        public IActionResult sqlQuery([FromBody] QueryExperiment request)
        {
            _fileProcessor.sqlQuery(request.DesiredMetrics, request.QueryParams);

            return Ok();
        }
    }
}
