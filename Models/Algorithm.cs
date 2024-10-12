using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using API_backend.Models;

namespace API_Backend.Models
{
    /// <summary>
    /// Represents an algorithm uploaded by a user or provided by admin.
    /// </summary>
    public class Algorithm
    {
        [Key]
        public int AlgorithmID { get; set; }

        [ForeignKey("UserID")]
        public string? UserID { get; set; } // Null if provided by admin

        [Required]
        public string AlgorithmName { get; set; }

        [Required]
        public string MainClassName { get; set; }

        [Required]
        public AlgorithmType AlgorithmType { get; set; }

        [Required]
        public string JarFilePath { get; set; }

        [Required]
        public DateTime UploadedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public User User { get; set; }
        public ICollection<AlgorithmParameter> Parameters { get; set; }
        public ICollection<ExperimentRequest> ExperimentRequests { get; set; }
    }

    public enum AlgorithmType
    {
        Supervised,
        Unsupervised,
        SemiSupervised
    }
}