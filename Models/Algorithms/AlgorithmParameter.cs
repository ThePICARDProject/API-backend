using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Backend.Models
{
    /// <summary>
    /// Represents a parameter for an algorithm.
    /// </summary>
    public class AlgorithmParameter
    {
        [Key]
        public int ParameterID { get; set; }

        [Required]
        public int AlgorithmID { get; set; } // FK to Algorithm

        [Required]
        public string ParameterName { get; set; }

        [Required]
        public int DriverIndex { get; set; }

        [Required]
        public string DataType { get; set; } // e.g., "int", "string", "bool"

        // Navigation property
        public Algorithm Algorithm { get; set; }

        public ICollection<ExperimentAlgorithmParameterValue> AlgorithmParameterValues { get; set; }
    }
}