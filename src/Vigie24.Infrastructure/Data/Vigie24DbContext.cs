using Microsoft.EntityFrameworkCore;
using Vigie24.Domain.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Vigie24.Infrastructure.Data;

public class Vigie24DbContext : DbContext
{
    public Vigie24DbContext(DbContextOptions<Vigie24DbContext> options)
        : base(options)
    {
    }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ArticleSource> ArticleSources => Set<ArticleSource>();
    public DbSet<ArticleAuthor> ArticleAuthors => Set<ArticleAuthor>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ArticleVote> ArticleVotes => Set<ArticleVote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ALL DateTime properties to use UTC
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(
                        new ValueConverter<DateTime, DateTime>(
                            dt => dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc),
                            dt => dt
                        )
                    );
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(
                        new ValueConverter<DateTime?, DateTime?>(
                            dt => dt.HasValue ? (dt.Value.Kind == DateTimeKind.Utc ? dt.Value : DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc)) : null,
                            dt => dt
                        )
                    );
                }
            }
        }

        // Configure Article
        modelBuilder.Entity<Article>(entity =>
        {
            entity.ToTable("articles");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CanonicalUrl).IsUnique();
            entity.HasIndex(e => e.PublishedAt);
            entity.HasIndex(e => e.RankingScore);
            
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Summary).IsRequired();
            entity.Property(e => e.CanonicalUrl).IsRequired();
            
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Source
        modelBuilder.Entity<Source>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WebsiteUrl).IsUnique();
            entity.ToTable("sources");
            
            entity.Property(e => e.Name).HasMaxLength(500).IsRequired();
            entity.Property(e => e.WebsiteUrl).IsRequired();
        });

        // Configure ArticleSource (junction table)
        modelBuilder.Entity<ArticleSource>(entity =>
        {
            entity.ToTable("article_sources");
            entity.HasKey(es => new { es.ArticleId, es.SourceId });
            
            entity.Property(e => e.SourceArticleUrl).IsRequired();
            
            entity.HasOne(es => es.Article)
                .WithMany(a => a.Sources)
                .HasForeignKey(es => es.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            
        });

        // Configure ArticleAuthor (junction table)
        modelBuilder.Entity<ArticleAuthor>(entity =>
        {
            entity.ToTable("article_authors");
            entity.HasKey(ea => new { ea.ArticleId, ea.AuthorId });
            
            entity.HasOne(ea => ea.Article)
                .WithMany(a => a.Authors)
                .HasForeignKey(ea => ea.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(ea => ea.Author)
                .WithMany(a => a.Articles)
                .HasForeignKey(ea => ea.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ArticleTag (junction table)
        modelBuilder.Entity<ArticleTag>(entity =>
        {
            entity.ToTable("article_tags");
            entity.HasKey(et => new { et.ArticleId, et.TagId });
            
            entity.HasOne(et => et.Article)
                .WithMany(a => a.Tags)
                .HasForeignKey(et => et.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(et => et.Tag)
                .WithMany(t => t.Articles)
                .HasForeignKey(et => et.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            
            entity.Property(e => e.Slug).HasMaxLength(200).IsRequired();
            
            entity.HasOne(e => e.ParentCategory)
                .WithMany(e => e.SubCategories)
                .HasForeignKey(e => e.ParentCategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Author
        modelBuilder.Entity<Author>(entity =>
        {
            entity.ToTable("authors");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            
            entity.Property(e => e.Name).HasMaxLength(500).IsRequired();
        });

        // Configure Tag
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            
            entity.Property(e => e.Slug).HasMaxLength(200).IsRequired();
        });

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.Property(e => e.Email).HasMaxLength(500).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(50).IsRequired();
        });

        // Configure ArticleVote
        modelBuilder.Entity<ArticleVote>(entity =>
        {
            entity.ToTable("article_votes");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.ArticleId }).IsUnique();
            
            entity.Property(e => e.VoteValue).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Votes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Article)
                .WithMany()
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ALL DateTime properties to use 'timestamp' (without time zone) to match PostgreSQL schema
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamp with time zone");
                }
            }
        }
    }
}
