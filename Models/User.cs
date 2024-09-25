using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace API_Backend.Models
{
    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    public class User
    {
        [Key]
        public string UserID { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<ExperimentRequest> ExperimentRequests { get; set; }
        public ICollection<Algorithm> Algorithms { get; set; }
        public ICollection<StoredDataSet> StoredDataSets { get; set; }
    }
}