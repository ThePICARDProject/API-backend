/*
 * Need GET request to receive all algortithms (ids & names) for current logged in user

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
    [Route("api/algorithms")]
    public class AlgorithmController(
        ApplicationDbContext dbContext,
        //IWebHostEnvironment environment,
        //IDatasetService algorithmService,
        ILogger<AlgorithmController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Retrieves a Algorithm set by its Users ID.
        /// </summary>
        /// <param name="id">The ID of the Algorithm.</param>
        /// <returns>Returns the Algorithm IDs and Names for given User.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAlgorithmById(int id)
        {
            // Check current user 
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            logger.LogInformation("User {userID} Retrieving algorithm with ID {AlgorithmID}", userId, id);


            // Fetch the AlgorithmSet from the database
            var AlgorithmSet = await dbContext.Algorithms
                                              .Where(a => a.UserID == userId)  // Filter by user ID
                                              .ToListAsync();  // Fetch all matching records as a list


            // Check if the AlgorithmSet is empty, return 404
            if (AlgorithmSet == null)
            {
                logger.LogWarning("Algorithms for user with ID: {ID} not found for user's request.", id);
                return NotFound(new { message = "Algorithms not found." });
            }

            //iterate through each received algo in list and verify its assigned userID
            foreach (var algorithm in AlgorithmSet)
            {
                if (algorithm.UserID != userId)
                {
                    // Log a warning and return 403 Forbidden for unauthorized access
                    logger.LogWarning("User {userID} is not authorized to access this algorithm {AlgorithmID}", userId, algorithm.AlgorithmID);
                    return Forbid();
                }
            }

            var listOfAlgorithmIDsAndNames = AlgorithmSet
                .Select(a => new { a.AlgorithmID, a.AlgorithmName })  // Pair AlgorithmID with Name
                .ToList();

            // Convert the list of algorithm IDs and names to a formatted string
            var algorithmIDNameString = string.Join(", ", listOfAlgorithmIDsAndNames.Select(a => $"{a.AlgorithmID} ({a.AlgorithmName})"));

            //now log authorization of user to access all algorithms in returned set, with created a string 
            logger.LogInformation("User {UserID} is authorized to access all algorithms in returned set: {algorithmIDNameString}", userId, algorithmIDNameString);


            return Ok(listOfAlgorithmIDsAndNames);
        }
    }
}