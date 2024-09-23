namespace API_backend.Models
{
    public class SubmitExperiment
    {
        public string UserId { get; set; }
        public string Dataset { get; set; }
        public string ClassName { get; set; }
        public string RelativeJarPath { get; set; }
        public int Trials { get; set; }
        public List<int> NodeCounts { get; set; }
        public List<string> args { get; set; }
    }
}
