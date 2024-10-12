using API_backend.Models;
using API_backend.Services.DataVisualization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VisualizationController : ControllerBase
    {

        private readonly DataVisualization _dataVisualization;

        public VisualizationController(DataVisualization dataVisualization)
        {
            _dataVisualization = dataVisualization;
        }

        [HttpPost]
        public IActionResult GetValues([FromForm] VisualizationRequest visRequest)
        {
            //TODO: remove debug line
            Console.WriteLine("Reached GetTest function");

            _dataVisualization.GraphInput(visRequest);

            return Ok();
        }
    }
}
