using Microsoft.EntityFrameworkCore;
using VotingService.Domain;

namespace VotingService.Data;

public sealed class VoteDbContext(DbContextOptions<VoteDbContext> options) : DbContext(options)
{
    public DbSet<Vote> Votes => Set<Vote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vote>(entity =>
        {
            entity.ToTable("votes");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.PollCode, x.VoterTokenHash }).IsUnique();
            entity.HasIndex(x => new { x.PollCode, x.OptionId });
            entity.Property(x => x.PollCode).HasMaxLength(12).IsRequired();
            entity.Property(x => x.VoterTokenHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.VotedAt).IsRequired();
        });
    }
}

