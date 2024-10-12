
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Nodes;
using API_backend.Services.FileProcessing;
using API_Backend.Services.FileProcessing;

namespace API_backend.Controller
{
    [ApiController]
    [Route("[controller]")]
    public class ExperimentController : ControllerBase
    {

        // FileProcessor class
        private readonly FileProcessor _fileProcessor;

        // Constructor to inject the FileProcessor 
        public ExperimentController(FileProcessor fileProcessor)
        {
            _fileProcessor = fileProcessor;
        }

        /// <summary>
        /// Endpoint to accept experiment data from ui and send to file processing service
        /// </summary>
        /// <returns>Returns the path where the experiment data is stored</returns>
        [HttpPost("create")]
        public IActionResult createExperiment([FromBody] String userId, String algorithmName, String survey)
        {

            /*
             *  Validates the experiment details, calls aggregateData, checks return value
             *  @return Ok() if successful
             *  @return BadRequest() if 
             *      - bad parameter from frontend
             *      - aggregateData() returns null
         
             */
            try
            {
                // Verify Args
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentNullException(nameof(userId));
                if (string.IsNullOrEmpty(algorithmName))
                    throw new ArgumentNullException(nameof(algorithmName));
                if (string.IsNullOrEmpty(survey))
                    throw new ArgumentNullException(nameof(survey));

                // Initiate Experiment from Docker service

                // call to FileProcessor service method to aggregate data
                // COMMENTED OUT TO ALLOW FOR BUILD
                //String filePath = _fileProcessor.AggregateData(userId, algorithmName, survey);

                // If we have a valid filePath from the file processor, getCsv()

                //if aggregateData() returns a valid path, success
                //if (!string.IsNullOrEmpty(filePath))
                    // return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return BadRequest();
            }

            //fall through
            return BadRequest();
        }


    }

}
