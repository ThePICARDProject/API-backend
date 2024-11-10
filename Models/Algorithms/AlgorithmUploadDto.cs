using API_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Policy;

namespace API_backend.Models
{
    public class AlgorithmUploadDto
    {
        public string AlgorithmName { get; set; }
        public string MainClassName { get; set; }
        public AlgorithmType AlgorithmType { get; set; }
        public IFormFile JarFile { get; set; }
        public string Parameters { get; set; } // JSON serialized parameters
    }

    public class AlgorithmParameterUploadDto
    {
        public string ParameterName { get; set; }
        public int DriverIndex { get; set; }
        public string DataType { get; set; }
    }
}
