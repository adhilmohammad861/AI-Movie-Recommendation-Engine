using Microsoft.EntityFrameworkCore;
using MovieApi.Models;
using Pgvector.EntityFrameworkCore;

namespace MovieApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Movie> Movies => Set<Movie>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        // Movie entity — maps to the lowercase 'movies' table
        modelBuilder.Entity<Movie>(entity =>
        {
            entity.ToTable("movies");
            entity.Property(m => m.Id).HasColumnName("id");
            entity.Property(m => m.Title).HasColumnName("title");
            entity.Property(m => m.Genre).HasColumnName("genre");
            entity.Property(m => m.Description).HasColumnName("description");
            entity.Property(m => m.CreatedAt).HasColumnName("created_at");
            entity.Property(m => m.Embedding)
                  .HasColumnName("embedding")
                  .HasColumnType("vector(1536)");
        });

        // Keyless result type used by the raw cosine-similarity SQL query
        modelBuilder.Entity<MovieQueryResult>(entity =>
        {
            entity.HasNoKey();
            // No extra column mappings needed — Database.SqlQueryRaw maps
            // by SQL column name → C# property name directly (case-insensitive).
        });
    }
}
