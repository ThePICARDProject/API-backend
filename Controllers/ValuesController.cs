using API_backend.Models;
using API_backend.Services.DataVisualization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {

        private readonly DataVisualization _dataVisualization;

        public ValuesController(DataVisualization dataVisualization)
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
