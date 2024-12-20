﻿namespace API_Backend.Models
{
    public class VisualizationRequest
    {
        public int AggregatedResultID { get; set; }
        public string XAxis { get; set; } = null!;
        public string YAxis { get; set; } = null!;
        public string? ColorDimension { get; set; }
        public string? FacetDimension { get; set; }
        public string GraphType { get; set; } = null!;
        public string OutputFileName { get; set; } = null!;
    }
}
