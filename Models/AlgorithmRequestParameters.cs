﻿using API_Backend.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace API_Backend.Models
{
    public class AlgorithmRequestParameters
    {
        [Key, ForeignKey("ExperimentRequest")]
        public string ExperimentID { get; set; } // FK and PK to ExperimentRequest

        public string DatasetName { get; set; }

        // Algorithm Specific parameters
        public ICollection<ExperimentAlgorithmParameterValue> ParameterValues { get; set; }

        // Navigation property
        public ExperimentRequest ExperimentRequest { get; set; }
    }
}