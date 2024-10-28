

using API_Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace API_Backend.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        // Constructor accepting DbContextOptions

        // DbSets for your entities
        public DbSet<User> Users { get; init; }
        public DbSet<ExperimentRequest> ExperimentRequests { get; init; }
        public DbSet<ClusterParameters> ClusterParameters { get; init; }
        public DbSet<Algorithm> Algorithms { get; init; }
        public DbSet<AlgorithmParameter> AlgorithmParameters { get; init; }
        public DbSet<UploadSession> UploadSessions { get; init; }
        public DbSet<ExperimentAlgorithmParameterValue> ExperimentAlgorithmParameterValues { get; init; }
        public DbSet<ExperimentResult> ExperimentResults { get; init; }
        public DbSet<DataVisualizationModel> DataVisualizations { get; init; }
        public DbSet<VisualizationExperiment> VisualizationExperiments { get; init; }
        public DbSet<StoredDataSet> StoredDataSets { get; init; }

        // Override OnModelCreating to configure relationships
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserID);
                entity.Property(e => e.UserID)
                      .IsRequired()
                      .HasMaxLength(36); // For GUIDs
            });

            // Configure StoredDataSet
            modelBuilder.Entity<StoredDataSet>(entity =>
            {
                entity.HasKey(e => e.DataSetID);
                entity.HasOne(e => e.User)
                      .WithMany(u => u.StoredDataSets)
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure UploadSession
            modelBuilder.Entity<UploadSession>(entity =>
            {
                entity.HasKey(e => e.UploadId);
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure other entities as needed...

            base.OnModelCreating(modelBuilder);
        }

        // Seed Users
        public static void Seed(ApplicationDbContext context)
        {
            
            if (!context.Users.Any())
            {
                var users = new List<User>
                {
                    new User
                    {
                        UserID = Guid.NewGuid().ToString(),
                        Email = "jac0057@mix.wvu.edu",
                        FirstName = "Jacob",
                        LastName = "Comer",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                context.Users.AddRange(users);
                context.SaveChanges();
            }

            // Seed Algorithms
            if (!context.Algorithms.Any())
            {
                var algorithms = new List<Algorithm>
                {
                    new Algorithm
                    {
                        UserID = context.Users.First().UserID, // Assigning to the first seeded user
                        AlgorithmName = "Sample Algorithm",
                        MainClassName = "SampleMainClass",
                        AlgorithmType = AlgorithmType.Supervised,
                        JarFilePath = "/path/to/sampleAlgorithm.jar",
                        UploadedAt = DateTime.UtcNow
                    }
                };

                context.Algorithms.AddRange(algorithms);
                context.SaveChanges();
            }

            // Seed Experiment Requests
            if (!context.ExperimentRequests.Any())
            {
                var experimentRequests = new List<ExperimentRequest>
                {
                    new ExperimentRequest
                    {
                        ExperimentID = Guid.NewGuid().ToString(),
                        UserID = context.Users.First().UserID, // Assigning to Jacob
                        AlgorithmID = context.Algorithms.First().AlgorithmID,
                        CreatedAt = DateTime.UtcNow,
                        Status = ExperimentStatus.Finished,
                        Parameters = "{\"param1\": \"value1\", \"param2\": \"value2\"}",
                        StartTime = DateTime.UtcNow,
                        EndTime = DateTime.UtcNow,
                    }
                };

                context.ExperimentRequests.AddRange(experimentRequests);
                context.SaveChanges();
            }

            // Seed Experiment Results
            if (!context.ExperimentResults.Any())
            {
                var experimentResults = new List<ExperimentResult>
                {
                    new ExperimentResult
                    {
                        ExperimentID = context.ExperimentRequests.First().ExperimentID,
                        CSVFilePath = "C:\\Users\\jacom\\Documents\\Fall 2024\\CSEE 481\\API-backend\\Services\\FileProcessing\\Test Files\\",
                        CSVFileName = "PICARD Example 1 Results.txt",
                        MetaDataFilePath = "C:\\Users\\jacom\\Documents\\Fall 2024\\CSEE 481\\API-backend\\Services\\FileProcessing\\Test Files\\Meta",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.ExperimentResults.AddRange(experimentResults);
                context.SaveChanges();
            }

            // Seed Cluster Parameters
            if (!context.ClusterParameters.Any())
            {
                var clusterParams = new List<ClusterParameters>
                {
                    new ClusterParameters
                    {
                        ExperimentID = context.ExperimentRequests.First().ExperimentID,
                        NodeCount = 5,
                        DriverMemory = "2 GB",
                        DriverCores = 2,
                        ExecutorNumber = 3,
                        ExecutorCores = 2,
                        ExecutorMemory = "4 GB",
                        MemoryOverhead = 512
                    }
                };

                context.ClusterParameters.AddRange(clusterParams);
                context.SaveChanges();
            }
        }
    }
}