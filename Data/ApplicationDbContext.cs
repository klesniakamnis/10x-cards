using Microsoft.EntityFrameworkCore;

namespace _10x_cards.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Flashcard> Flashcards => Set<Flashcard>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Flashcard>(entity =>
        {
            entity.Property(f => f.Source)
                .HasConversion<string>();

            entity.Property(f => f.EasinessFactor)
                .HasDefaultValue(2.5);

            entity.Property(f => f.Interval)
                .HasDefaultValue(0);

            entity.Property(f => f.Repetitions)
                .HasDefaultValue(0);

            entity.HasOne(f => f.User)
                .WithMany(u => u.Flashcards)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MagicLinkToken>(entity =>
        {
            entity.HasIndex(t => t.Token).IsUnique();
        });

        modelBuilder.Entity<AuthSession>(entity =>
        {
            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        SetTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IHasTimestamps>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.UpdatedAt = now;

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;

                if (entry.Entity is Flashcard flashcard && flashcard.NextReviewDate == default)
                    flashcard.NextReviewDate = now;
            }
        }
    }
}
