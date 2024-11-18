

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
        public DbSet<AlgorithmRequestParameters> AlgorithmRequestParameters { get; init; }
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
                        AlgorithmName = "First Algorithm",
                        MainClassName = "SampleMainClass",
                        AlgorithmType = AlgorithmType.Supervised,
                        JarFilePath = "/path/to/sampleAlgorithm.jar",
                        UploadedAt = DateTime.UtcNow
                    },
                    new Algorithm
                    {
                        UserID = context.Users.First().UserID, // Assigning to the first seeded user
                        AlgorithmName = "Second Algorithm",
                        MainClassName = "SampleMainClass",
                        AlgorithmType = AlgorithmType.Unsupervised,
                        JarFilePath = "/path/to/secondAlgorithm.jar",
                        UploadedAt = DateTime.UtcNow
                    },
                    new Algorithm
                    {
                        UserID = context.Users.First().UserID, // Assigning to the first seeded user
                        AlgorithmName = "Third Algorithm",
                        MainClassName = "SampleMainClass",
                        AlgorithmType = AlgorithmType.SemiSupervised,
                        JarFilePath = "/path/to/thirdAlgorithm.jar",
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

                var algorithms = context.Algorithms.ToList();

                var algorithm1 = algorithms.ElementAt(0);

                var algorithm2 = algorithms.ElementAt(1);

                var algorithm3 = algorithms.ElementAt(2);


                var algorithmParameters = new List<AlgorithmParameter>
                {
                    new AlgorithmParameter
                    {
                        ParameterID = 1,
                        AlgorithmID = algorithm1.AlgorithmID,
                        ParameterName = "Trees",
                        DriverIndex = 1,
                        DataType = "int"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 2,
                        AlgorithmID = algorithm1.AlgorithmID,
                        ParameterName = "Supervised",
                        DriverIndex = 1,
                        DataType = "int"

                    },                    
                    new AlgorithmParameter
                    {
                        ParameterID = 3,
                        AlgorithmID = algorithm1.AlgorithmID,
                        ParameterName = "Semi-Supervised",
                        DriverIndex = 1,
                        DataType = "int"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 4,
                        AlgorithmID = algorithm1.AlgorithmID,
                        ParameterName = "Survey",
                        DriverIndex = 1,
                        DataType = "string"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 5,
                        AlgorithmID = algorithm1.AlgorithmID,
                        ParameterName = "Classifier",
                        DriverIndex = 1,
                        DataType = "string"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 6,
                        AlgorithmID = algorithm1.AlgorithmID,
                        ParameterName = "Labeled",
                        DriverIndex = 1,
                        DataType = "int"

                    },

                    new AlgorithmParameter
                    {
                        ParameterID = 7,
                        AlgorithmID = algorithm2.AlgorithmID,
                        ParameterName = "Trees",
                        DriverIndex = 1,
                        DataType = "int"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 8,
                        AlgorithmID = algorithm2.AlgorithmID,
                        ParameterName = "Supervised",
                        DriverIndex = 1,
                        DataType = "int"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 9,
                        AlgorithmID = algorithm2.AlgorithmID,
                        ParameterName = "Semi-Supervised",
                        DriverIndex = 1,
                        DataType = "int"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 10,
                        AlgorithmID = algorithm2.AlgorithmID,
                        ParameterName = "Survey",
                        DriverIndex = 1,
                        DataType = "string"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 11,
                        AlgorithmID = algorithm2.AlgorithmID,
                        ParameterName = "Classifier",
                        DriverIndex = 1,
                        DataType = "string"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 12,
                        AlgorithmID = algorithm2.AlgorithmID,
                        ParameterName = "Labeled",
                        DriverIndex = 1,
                        DataType = "int"

                    },

                    new AlgorithmParameter
                    {
                        ParameterID = 13,
                        AlgorithmID = algorithm3.AlgorithmID,
                        ParameterName = "Trees",
                        DriverIndex = 1,
                        DataType = "int"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 14,
                        AlgorithmID = algorithm3.AlgorithmID,
                        ParameterName = "Supervised",
                        DriverIndex = 1,
                        DataType = "int"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 15,
                        AlgorithmID = algorithm3.AlgorithmID,
                        ParameterName = "Semi-Supervised",
                        DriverIndex = 1,
                        DataType = "int"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 16,
                        AlgorithmID = algorithm3.AlgorithmID,
                        ParameterName = "Survey",
                        DriverIndex = 1,
                        DataType = "string"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 17,
                        AlgorithmID = algorithm3.AlgorithmID,
                        ParameterName = "Classifier",
                        DriverIndex = 1,
                        DataType = "string"

                    },
                    new AlgorithmParameter
                    {
                        ParameterID = 18,
                        AlgorithmID = algorithm3.AlgorithmID,
                        ParameterName = "Labeled",
                        DriverIndex = 1,
                        DataType = "int"

                    },

                };

                context.AlgorithmParameters.AddRange(algorithmParameters);
                context.SaveChanges();


            }

            // Seed AlgorithmParameterValues
            if (!context.ExperimentAlgorithmParameterValues.Any())
            {

                var experimentLists = context.ExperimentRequests.ToList();

                var experiment1 = context.ExperimentRequests.ElementAt(0);

                var experiment2 = context.ExperimentRequests.ElementAt(1);

                var experiment3 = context.ExperimentRequests.ElementAt(2);

                var experimentvalues = new List<ExperimentAlgorithmParameterValue>
                {
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 1,
                        ExperimentID = experiment1.ExperimentID,
                        ParameterID = 1,
                        Value = "10",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 2,
                        ExperimentID = experiment1.ExperimentID,
                        ParameterID = 2,
                        Value = "33",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 3,
                        ExperimentID = experiment1.ExperimentID,
                        ParameterID = 3,
                        Value = "44",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 4,
                        ExperimentID = experiment1.ExperimentID,
                        ParameterID = 4,
                        Value = "gbt350drift",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 5,
                        ExperimentID = experiment1.ExperimentID,
                        ParameterID = 5,
                        Value = "CoDRIFt",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 6,
                        ExperimentID = experiment1.ExperimentID,
                        ParameterID = 6,
                        Value = "1",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 7,
                        ExperimentID = experiment2.ExperimentID,
                        ParameterID = 7,
                        Value = "15",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 8,
                        ExperimentID = experiment2.ExperimentID,
                        ParameterID = 8,
                        Value = "39",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 9,
                        ExperimentID = experiment2.ExperimentID,
                        ParameterID = 9,
                        Value = "37",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 10,
                        ExperimentID = experiment2.ExperimentID,
                        ParameterID = 10,
                        Value = "palfa",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 11,
                        ExperimentID = experiment2.ExperimentID,
                        ParameterID = 11,
                        Value = "CoDRIFt",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 12,
                        ExperimentID = experiment2.ExperimentID,
                        ParameterID = 12,
                        Value = "10",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 13,
                        ExperimentID = experiment3.ExperimentID,
                        ParameterID = 13,
                        Value = "10",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 14,
                        ExperimentID = experiment3.ExperimentID,
                        ParameterID = 14,
                        Value = "36",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 15,
                        ExperimentID = experiment3.ExperimentID,
                        ParameterID = 15,
                        Value = "42",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 16,
                        ExperimentID = experiment3.ExperimentID,
                        ParameterID = 16,
                        Value = "gbt350drift",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 17,
                        ExperimentID = experiment3.ExperimentID,
                        ParameterID = 17,
                        Value = "CoDRIFt",
                    },
                    new ExperimentAlgorithmParameterValue
                    {
                        ID = 18,
                        ExperimentID = experiment3.ExperimentID,
                        ParameterID = 18,
                        Value = "15",
                    },

                };

                context.ExperimentAlgorithmParameterValues.AddRange(experimentvalues);
                context.SaveChanges();

            }

            // Seed AlgorithmRequestParameters
            if (!context.AlgorithmRequestParameters.Any()) {
                var algorithmRequestParameters = new List<AlgorithmRequestParameters>();


                var experimentRequests = context.ExperimentRequests.ToList();

                foreach (var experimentRequest in experimentRequests)
                {
                    algorithmRequestParameters.Add(
                        new AlgorithmRequestParameters
                        {
                            ExperimentID = experimentRequest.ExperimentID,
                            DatasetName = "Sample Dataset"
                        });
                }

                context.AlgorithmRequestParameters.AddRange(algorithmRequestParameters);
                context.SaveChanges();
            }


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