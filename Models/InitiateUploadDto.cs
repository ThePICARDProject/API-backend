using System.ComponentModel.DataAnnotations;

namespace API_Backend.Models
{
    /// <summary>
    /// DTO for initiating a dataset upload.
    /// </summary>
    public class InitiateUploadDto
    {
        /// <summary>
        /// The original file name.
        /// </summary>
        [Required]
        public string FileName { get; set; }

        /// <summary>
        /// The total number of chunks.
        /// </summary>
        [Required]
        public int TotalChunks { get; set; }

        /// <summary>
        /// (Optional) Description of the dataset.
        /// </summary>
        public string Description { get; set; }
    }
}