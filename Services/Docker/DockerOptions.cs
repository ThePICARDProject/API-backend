namespace API_backend.Services.Docker
{
    public class DockerOptions
    {
        // Base path for .jar files
        public string JarFileBasePath { get; set; }

        // Base path for docker-swarm
        public string DockerSwarmPath { get; set; }
    }
}
