namespace API_backend.Models
{
    public class VisualizationRequest
    {
        public IFormFile InputFile { get; set; } = null!;
        public string XAxis { get; set; } = null!;
        public string YAxis { get; set;} = null!;
        public string? ColorDimension { get; set; }
        public string? FacetDimension { get; set; }
        public string GraphType { get; set; } = null!;
        public string? OutputFileName { get; set; }


    }
}
