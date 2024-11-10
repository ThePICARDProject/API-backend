
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Backend.Models
{
    /// <summary>
    /// Represents an upload session for chunked uploads.
    /// </summary>
    public class UploadSession
    {
        [Key]
        public Guid UploadId { get; set; }

        [Required]
        public string UserID { get; set; }

        [ForeignKey("UserID")]
        public User User { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public int TotalChunks { get; set; }

        [Required]
        public int UploadedChunks { get; set; } = 0;

        public string Description { get; set; } // Optional description

        [Required]
        public DateTime UploadedAt { get; set; }

        [Required]
        public bool Completed { get; set; } = false;
    }
}