using API_Backend.Models;
using API_Backend.Services.DataVisualization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        public async Task<IActionResult> GetValues([FromForm] VisualizationRequest visRequest)
        {
            // Get userId from logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var aggregatedResultId = visRequest.AggregatedResultID;

            bool result = await _dataVisualization.GraphInput(visRequest, userId);
            if (result)
            {
                string filePath = await _dataVisualization.GetFilePath(aggregatedResultId, userId);

                if (filePath != null)
                {

                    return this.Content(filePath);
                }
                else
                {
                    return StatusCode(500, new { message = "Error returning file path to data visualization grpah." });
                }
            }
            else
            {
                return StatusCode(500); // internal service error
            }
        }
    }
}
