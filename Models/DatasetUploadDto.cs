// File: Models/DataSetUploadDto.cs
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace API_Backend.Models
{
    /// <summary>
    /// Data Transfer Object for uploading a dataset.
    /// </summary>
    public class DataSetUploadDto
    {
        /// <summary>
        /// The CSV file to upload.
        /// </summary>
        [Required]
        public IFormFile File { get; set; }

        /// <summary>
        /// The name of the dataset.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// A description of the dataset.
        /// </summary>
        public string Description { get; set; }
    }
}