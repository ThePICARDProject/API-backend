

using API_Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Asn1.Cms;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
        public DbSet<AggregatedResult> AggregatedResults { get; init; }
        public DbSet<CsvResult> CsvResults { get; init; }


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
                    },
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
                    },
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
                    },
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
                    },

                };

                context.ExperimentRequests.AddRange(experimentRequests);
                context.SaveChanges();
            }

            // Seed Experiment Results
            if (!context.ExperimentResults.Any())
            {
                var experimentResults = new List<ExperimentResult>();

                int i = 1;

                foreach (var experimentRequest in context.ExperimentRequests.ToList())
                {
                    experimentResults.Add(
                        new ExperimentResult
                        {
                            ExperimentID = experimentRequest.ExperimentID,
                            ResultFilePath = "C:\\Users\\jacom\\Documents\\Fall 2024\\CSEE 481\\API-backend\\Services\\FileProcessing\\Test Files\\",
                            ResultFileName = $"PICARD Example {i} Results.txt",
                            MetaDataFilePath = "C:\\Users\\jacom\\Documents\\Fall 2024\\CSEE 481\\API-backend\\Services\\FileProcessing\\Test Files\\Meta",
                            CreatedAt = DateTime.UtcNow
                        }
                    );

                    i++;
                }


                context.ExperimentResults.AddRange(experimentResults);
                context.SaveChanges();
            }

            // Seed Algorithm Parameters
            if (!context.AlgorithmParameters.Any()) {
            
                var algorithmParameters = new List<AlgorithmParameter>
                {
                    new AlgorithmParameter
                    {
                        ParameterID = 1,
                        AlgorithmID = context.Algorithms.First().AlgorithmID,
                        ParameterName = "Trees",
                        DriverIndex = 1,
                        DataType = "int"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 2,
                        AlgorithmID = context.Algorithms.First().AlgorithmID,
                        ParameterName = "Supervised",
                        DriverIndex = 1,
                        DataType = "int"

                    },                    
                    new AlgorithmParameter
                    {
                        ParameterID = 3,
                        AlgorithmID = context.Algorithms.First().AlgorithmID,
                        ParameterName = "Semi-Supervised",
                        DriverIndex = 1,
                        DataType = "int"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 4,
                        AlgorithmID = context.Algorithms.First().AlgorithmID,
                        ParameterName = "Survey",
                        DriverIndex = 1,
                        DataType = "string"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 5,
                        AlgorithmID = context.Algorithms.First().AlgorithmID,
                        ParameterName = "Classifier",
                        DriverIndex = 1,
                        DataType = "string"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 6,
                        AlgorithmID = context.Algorithms.First().AlgorithmID,
                        ParameterName = "Trees",
                        DriverIndex = 1,
                        DataType = "int"

                    },

                };


            }

            // TODO: Seed AlgorithmParameterValues


            // TODO: Seed AlgorithmRequestParameters

            // TODO: Seed ExperimentAlgorithmParameterValues


            // Seed Cluster Parameters
            if (!context.ClusterParameters.Any())
            {
                var clusterParams = new List<ClusterParameters>();

                int i = 2;

                foreach (var experimentRequest in context.ExperimentRequests.ToList())
                {
                    clusterParams.Add(
                    new ClusterParameters
                    {
                        ExperimentID = experimentRequest.ExperimentID,
                        NodeCount = i,
                        DriverMemory = "2 GB",
                        DriverCores = 2,
                        ExecutorNumber = 3,
                        ExecutorCores = 2,
                        ExecutorMemory = "4 GB",
                        MemoryOverhead = 512
                    });

                    i = i + 2;
                }

                context.ClusterParameters.AddRange(clusterParams);
                context.SaveChanges();
            }
        }
    }
}