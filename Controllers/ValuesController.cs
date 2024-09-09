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

        [HttpGet]
        public IActionResult GetValues()
        {
            Console.WriteLine("Reached GetTest function");
            string parameters = "- i \"testdata.csv\" - d1 \"Ratio.S-SSL\" - d2 \"Recall\" - g \"line\" - o \"output.pdf\"";
            _dataVisualization.GraphInput(parameters);

            return Ok();
        }
    }
}
