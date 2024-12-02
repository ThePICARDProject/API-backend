using API_Backend.Models;
using API_Backend.Services.DataVisualization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API_Backend.Controllers
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
            bool result = _dataVisualization.GraphInput(visRequest);
            if (result)
            {
                return Ok("Graph successfully generated");
            }
            else
            {
                return StatusCode(500); // internal service error
            }
        }
    }
}
