
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Nodes;

namespace API_backend.Controller
{
    [ApiController]
    [Route("[controller]")]
    public class ExperimentController : ControllerBase
    {

        //need experiment object for holding params of experiment?


        /**
         * STEPS FOR HANDLING POST REQUEST
         * 
         *  1.	Authenticate the User
         *  2. 	Parse the Multipart Request:
         *          //service layer??
         * 	3.	Save Experiment Details:
         * 	        //service layer??
            4.	Save the Dataset:
            5.	Handle Experiment Status:
            6.	Trigger Docker Swarm Execution:
         **/
        

        //joe said json Object is easier to use for passing info to this endpoint?
        [HttpPost("sendExperiment")]
        public async Task<ActionResult> passExperiment(JsonObject Experiment)
        {
           /*
            * GUARDS NEEDED?
            */

            //NEED: convert jsonObject to csv file ?
                 //csv experimentCsv = convertToCsv(ExperimentData);

            //call service method for handling of
            //      1. parsing,
            //      2. passing to model
            //      3. saving to db,
            //      4. sending to docker.
            service.receiveExperiment(experimentCsv);

            int success = service.sendExperiment(experimentCsv);

            if (success)
            {
                return Ok();
            }
            else
            {
                return BadRequestObjectResult("Error sending experiment");
            }
            
        }


    }
}
