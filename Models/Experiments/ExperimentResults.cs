using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Backend.Models
{
    /// <summary>
    /// Represents the results of an experiment.
    /// </summary>
    public class ExperimentResult
    {
        [Key]
        public int ResultID { get; set; }

        [Required]
        public Guid ExperimentID { get; set; } // FK to ExperimentRequest

        public string CSVFilePath { get; set; }
        public string CSVFileName { get; set; }
        public string MetaDataFilePath { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation property
        [ForeignKey("ExperimentID")]
        public ExperimentRequest ExperimentRequest { get; set; }
    }
}