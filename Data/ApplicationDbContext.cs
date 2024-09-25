// File: Data/ApplicationDbContext.cs
using API_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace API_Backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        // Constructor accepting DbContextOptions
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for your entities
        public DbSet<User> Users { get; set; }
        public DbSet<ExperimentRequest> ExperimentRequests { get; set; }
        public DbSet<DockerSwarmParameters> DockerSwarmParameters { get; set; }
        public DbSet<Algorithm> Algorithms { get; set; }
        public DbSet<AlgorithmParameter> AlgorithmParameters { get; set; }
        public DbSet<ExperimentAlgorithmParameterValue> ExperimentAlgorithmParameterValues { get; set; }
        public DbSet<ExperimentResult> ExperimentResults { get; set; }
        public DbSet<DataVisualization> DataVisualizations { get; set; }
        public DbSet<VisualizationExperiment> VisualizationExperiments { get; set; }
        public DbSet<StoredDataSet> StoredDataSets { get; set; }

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

            // Example for StoredDataSet
            modelBuilder.Entity<StoredDataSet>(entity =>
            {
                entity.HasKey(e => e.DataSetID);
                entity.HasOne(e => e.User)
                      .WithMany(u => u.StoredDataSets)
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure other entities similarly...

            base.OnModelCreating(modelBuilder);
        }
    }
}