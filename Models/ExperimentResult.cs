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
        public string ExperimentID { get; set; } // FK to ExperimentRequest

        public string ResultFilePath { get; set; } // TODO: Change to ResultFilePath
        public string ResultFileName { get; set; } // TODO: Change to ResultFileName
        public string MetaDataFilePath { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation property
        [ForeignKey("ExperimentID")]
        public ExperimentRequest ExperimentRequest { get; set; }
    }
}