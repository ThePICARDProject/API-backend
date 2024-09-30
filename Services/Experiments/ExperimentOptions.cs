namespace API_backend.Services.Experiments
{
    public class ExperimentOptions
    {
        // Base path for .jar files
        public string JarFileBasePath { get; set; }

        // Base path for docker-swarm
        public string RepositoryBasePath { get; set; }
    }
}
