using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Nodes;
using API_backend.Services.FileProcessing;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace API_backend.Controller
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

        [HttpPost("createCsv")]
        public IActionResult csvCreate([FromBody] List<string> desiredMetrics, string inputFile)
        {
            _fileProcessor.GetCsvTest(desiredMetrics, inputFile);
            
            return Ok();
        }
    }
}
