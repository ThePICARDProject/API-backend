
using API_backend.Models;
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

            // Configure Algorithm
            modelBuilder.Entity<Algorithm>()
                .Property(x => x.AlgorithmType)
                .HasConversion(
                    y => y.ToString(),
                    y => (AlgorithmType) Enum.Parse(typeof(AlgorithmType), y)
                );

            // Configure Experiment Request
            modelBuilder.Entity<Algorithm>()
                .HasOne(x => x.User)
                .WithMany(x => x.Algorithms)
                .HasForeignKey(x => x.UserID);

            modelBuilder.Entity<ExperimentRequest>()
                .Property(x => x.Status)
                .HasConversion(
                    y => y.ToString(),
                    y => (ExperimentStatus)Enum.Parse(typeof(ExperimentStatus), y)
                );
            
            modelBuilder.Entity<ExperimentRequest>()
                .HasOne(x => x.Algorithm)
                .WithMany(e => e.ExperimentRequests)
                .HasForeignKey(x => x.AlgorithmID);

            modelBuilder.Entity<ExperimentRequest>()
                .HasOne(x => x.User)
                .WithMany(x => x.ExperimentRequests)
                .HasForeignKey(x => x.UserID);
               
            // Configure ExperimentAlgorithmParameterValue
            modelBuilder.Entity<ExperimentAlgorithmParameterValue>()
                .HasKey(e => new { e.ExperimentID, e.ParameterID });

            modelBuilder.Entity<ExperimentAlgorithmParameterValue>()
                .HasOne(e => e.AlgorithmParameter)
                .WithMany(e => e.AlgorithmParameterValues)
                .HasForeignKey(e => e.ParameterID);

            // Cofnigure Algorithm Parameter
            modelBuilder.Entity<AlgorithmParameter>()
                .HasOne(e => e.Algorithm)
                .WithMany(e => e.Parameters)
                .HasForeignKey(e => e.AlgorithmID);

            base.OnModelCreating(modelBuilder);
        }
    }
}