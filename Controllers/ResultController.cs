/*
 * Need GET request to receive all Results (ids & names) for current logged in user
 * Model after getDataSetByID getrequest
 */
using System.Diagnostics;
using API_Backend.Data;
using API_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API_backend.Services.FileProcessing;
using Microsoft.EntityFrameworkCore;

namespace API_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/results")]
    public class ResultController(
        ApplicationDbContext dbContext,
        //IWebHostEnvironment environment,
        //IDatasetService ResultService,
        ILogger<ResultController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Retrieves a Results set by its Users ID.
        /// </summary>
        /// <param name="id">The ID of the Result.</param>
        /// <returns>Returns the Result IDs and Names for given User.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetResultById(int id)
        {
            // Check current user 
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            logger.LogInformation("User {userID} Retrieving result with ID {ResultID}", userId, id);

            // Fetch the ResultSet from the database
            var ResultSet = await dbContext.Results
                                              .Where(a => a.UserID == userId)  // Filter by user ID
                                              .ToListAsync();  // Fetch all matching records as a list


            // Check if the ResultSet is empty, return 404
            if (ResultSet == null)
            {
                logger.LogWarning("Results for user with ID: {ID} not found for user's request.", id);
                return NotFound(new { message = "Results not found." });
            }

            //iterate through each received Result in list and verify its assigned userID
            foreach (var result in ResultSet)
            {
                if (result.UserID != userId)
                {
                    // Log a warning and return 403 Forbidden for unauthorized access
                    logger.LogWarning("User {userID} is not authorized to access this Result {ResultID}", userId, result.ResultID);
                    return Forbid();
                }
            }

            var listOfResultIDsAndNames = ResultSet
                .Select(a => new { a.ResultID, a.ResultName })  // Pair ResultID with Name
                .ToList();

            // Convert the list of Result IDs and names to a formatted string
            var ResultIDNameString = string.Join(", ", listOfResultIDsAndNames.Select(a => $"{a.ResultID} ({a.ResultName})"));

            //now log authorization of user to access all Results in returned set, with created a string 
            logger.LogInformation("User {UserID} is authorized to access all Results in returned set: {resultIDNameString}", userId, ResultIDNameString);

            return Ok(listOfResultIDsAndNames);
        }
    }
}
