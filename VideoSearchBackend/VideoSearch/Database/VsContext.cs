using Microsoft.EntityFrameworkCore;
using VideoSearch.Database.Models;

namespace VideoSearch.Database;

public class VsContext : DbContext
{
    public DbSet<VideoMeta> VideoMetas { get; set; }
    public DbSet<VideoIndex> VideoIndices { get; set; }
    public DbSet<NgramModel> Ngrams { get; set; }
    public DbSet<NgramDocument> NgramDocuments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<VideoMeta>()
            .HasKey(m => m.Id);

        modelBuilder.Entity<VideoMeta>()
            .HasIndex(m => m.Status);
        modelBuilder.Entity<VideoMeta>()
            .HasIndex(m => m.Processing);
        modelBuilder.Entity<VideoMeta>()
            .HasIndex(m => m.CreatedAt);
        modelBuilder.Entity<VideoMeta>()
            .HasIndex(m => m.StatusChangedAt);

        modelBuilder.Entity<VideoMeta>()
            .Property(m => m.Keywords).IsRequired(false);
        modelBuilder.Entity<VideoMeta>()
            .Property(m => m.Centroids).IsRequired(false);
        modelBuilder.Entity<VideoMeta>()
            .Property(m => m.TranslatedDescription).IsRequired(false);
        modelBuilder.Entity<VideoMeta>()
            .Property(m => m.RawDescription).IsRequired(false);
        modelBuilder.Entity<VideoMeta>()
            .Property(m => m.SttKeywords).IsRequired(false);

        modelBuilder.Entity<VideoIndex>()
            .HasKey(i => i.Id);

        modelBuilder.Entity<VideoIndex>()
            .HasIndex(i => i.Type);
        
        modelBuilder.Entity<VideoIndex>()
            .HasIndex(i => i.VideoMetaId);

        modelBuilder.Entity<VideoIndex>()
            .HasIndex(i => i.Vector)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasStorageParameter("m", 16)
            .HasStorageParameter("ef_construction", 64);

        modelBuilder.Entity<NgramModel>()
            .HasKey(n => n.Ngram);

        modelBuilder.Entity<NgramDocument>()
            .HasKey(d => new { d.Ngram, d.DocumentId });

        modelBuilder.Entity<NgramDocument>()
            .HasIndex(d => d.Ngram);
        modelBuilder.Entity<NgramDocument>()
            .HasIndex(d => d.DocumentId);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string connectionString = Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION")
                                  ?? throw new Exception("No POSTGRESQL_CONNECTION env variable set");

        optionsBuilder.UseNpgsql(connectionString, o => o.UseVector());
    }
}