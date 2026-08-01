using Microsoft.EntityFrameworkCore;
using PollService.Domain;

namespace PollService.Data;

public sealed class PollDbContext(DbContextOptions<PollDbContext> options) : DbContext(options)
{
    public DbSet<Poll> Polls => Set<Poll>();
    public DbSet<PollOption> PollOptions => Set<PollOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Poll>(entity =>
        {
            entity.ToTable("polls");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(12).IsRequired();
            entity.Property(x => x.Question).HasMaxLength(500).IsRequired();
            entity.Property(x => x.CreatorTokenHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt);
            entity.Property(x => x.DeletedAt);
            entity.HasMany(x => x.Options)
                .WithOne(x => x.Poll)
                .HasForeignKey(x => x.PollId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PollOption>(entity =>
        {
            entity.ToTable("poll_options");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.PollId, x.Position }).IsUnique();
            entity.Property(x => x.Text).HasMaxLength(200).IsRequired();
        });
    }
}
