
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace API_backend.Controller
{
    public class HomeController
    {


        /**
         * picard notes

            
            get parameters from ui 
            send to database ( dylan) 
            look at shell scripts for parameter details, ui should be the same

            make list of params from shell scripts? 
            shouldn't have to do login functionality ( dylan doing this )

            outline each post request in kanban for devine
        **/


        //post request template
        [Microsoft.AspNetCore.Mvc.HttpPost]
        public async Task<ActionResult> SendExperiment([FromBody] string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return BadRequest();
            }

            //do something with value
                //pass value to service method for handling in database


            return Ok();
        }


    }
}
