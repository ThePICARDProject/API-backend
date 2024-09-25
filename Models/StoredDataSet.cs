// File: Models/StoredDataSet.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Backend.Models
{
    public class StoredDataSet
    {
        [Key]
        public int DataSetID { get; set; }

        [Required]
        public string UserID { get; set; } // Changed from int to string

        [ForeignKey("UserID")]
        public User User { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public string FilePath { get; set; }

        [Required]
        public DateTime UploadedAt { get; set; }
    }
}