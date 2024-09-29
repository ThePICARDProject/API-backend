using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace API_Backend.Models
{
    /// <summary>
    /// DTO for uploading a dataset chunk.
    /// </summary>
    public class ChunkedUploadDto
    {
        /// <summary>
        /// Unique identifier for the upload session.
        /// </summary>
        [Required]
        public Guid UploadId { get; set; }

        /// <summary>
        /// Sequence number of the current chunk.
        /// </summary>
        [Required]
        public int ChunkNumber { get; set; }

        /// <summary>
        /// Total number of chunks.
        /// </summary>
        [Required]
        public int TotalChunks { get; set; }

        /// <summary>
        /// Original file name.
        /// </summary>
        [Required]
        public string FileName { get; set; }

        /// <summary>
        /// The chunk data.
        /// </summary>
        [Required]
        public IFormFile File { get; set; }
    }
}