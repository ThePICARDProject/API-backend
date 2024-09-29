using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace API_Backend.Models
{
    /// <summary>
    /// Represents a data visualization request.
    /// </summary>
    public class DataVisualization
    {
        [Key]
        public int VisualizationRequestID { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public string VisualizationDataFilePath { get; set; }

        // Navigation property
        public ICollection<VisualizationExperiment> VisualizationExperiments { get; set; }
    }
}