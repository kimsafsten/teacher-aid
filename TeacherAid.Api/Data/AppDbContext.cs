namespace TeacherAid.Api.Data
{
    using Microsoft.EntityFrameworkCore;
    using Pgvector.EntityFrameworkCore;
    using TeacherAid.Api.Models;

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Submission> Submissions => Set<Submission>();
        public DbSet<FeedbackDraft> FeedbackDrafts => Set<FeedbackDraft>();
        public DbSet<CourseDocument> CourseDocuments => Set<CourseDocument>();
        public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("vector");
            modelBuilder.Entity<DocumentChunk>()
                .Property(c => c.Embedding)
                .HasColumnType("vector(768)");
        }
    }
}
