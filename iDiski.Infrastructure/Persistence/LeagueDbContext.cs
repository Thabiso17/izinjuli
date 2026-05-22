using iDiski.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using iDiski.Application.Common.Interfaces;

namespace iDiski.Infrastructure.Persistence;

public class LeagueDbContext : DbContext, ILeagueDbContext
{
    public LeagueDbContext(DbContextOptions<LeagueDbContext> options) : base(options) { }

    // ── DbSets ────────────────────────────────────────────────────────────────
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<MatchResult> MatchResults => Set<MatchResult>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Sponsor> Sponsors => Set<Sponsor>();
    public DbSet<PageLayoutConfig> PageLayoutConfigs => Set<PageLayoutConfig>();
    public DbSet<Division> Divisions => Set<Division>();
    public DbSet<MatchEvent> MatchEvents => Set<MatchEvent>();
    public DbSet<Suspension> Suspensions => Set<Suspension>();

    // ── Model Configuration ───────────────────────────────────────────────────
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Team ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(t => t.ShortCode)
                  .IsRequired()
                  .HasMaxLength(10);

            entity.HasIndex(t => t.ShortCode)
                  .IsUnique();

            entity.Property(t => t.PrimaryColour).HasMaxLength(7);
            entity.Property(t => t.SecondaryColour).HasMaxLength(7);
        });

        // ── Player ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.FirstName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(p => p.LastName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(p => p.Bio)
                  .HasMaxLength(2000)
                  .IsRequired(false);

            // Store enum as string for readability in the DB
            entity.Property(p => p.Position)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            // Composite index for squad-sheet queries
            entity.HasIndex(p => new { p.TeamId, p.JerseyNumber })
                  .IsUnique();

            entity.HasOne(p => p.Team)
                  .WithMany(t => t.Players)
                  .HasForeignKey(p => p.TeamId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── MatchResult ───────────────────────────────────────────────────────
        modelBuilder.Entity<MatchResult>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            entity.Property(m => m.Notes).HasMaxLength(2000);

            // Prevent a team from being both home and away
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_MatchResult_DifferentTeams",
                "\"HomeTeamId\" <> \"AwayTeamId\""));

            // Index for fixture list queries
            entity.HasIndex(m => new { m.Season, m.MatchweekNumber });
            entity.HasIndex(m => m.MatchDate);

            entity.HasOne(m => m.HomeTeam)
                  .WithMany(t => t.HomeMatches)
                  .HasForeignKey(m => m.HomeTeamId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.AwayTeam)
                  .WithMany(t => t.AwayMatches)
                  .HasForeignKey(m => m.AwayTeamId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Article ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Title)
                  .IsRequired()
                  .HasMaxLength(300);

            entity.Property(a => a.Slug)
                  .IsRequired()
                  .HasMaxLength(300);

            // Unique slug for clean URLs
            entity.HasIndex(a => a.Slug).IsUnique();

            entity.Property(a => a.Content).IsRequired();
            entity.Property(a => a.Author).IsRequired().HasMaxLength(100);

            // Native PostgreSQL text[] — Npgsql handles the serialisation automatically
            entity.Property(a => a.Tags).HasColumnType("text[]");

            entity.HasIndex(a => a.PublishedAt);
        });

        // ── Sponsor ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Sponsor>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(s => s.Tier)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            entity.Property(s => s.Placement)
                  .HasConversion<string>()
                  .HasMaxLength(30);

            // Useful for the Angular ad rotator
            entity.HasIndex(s => new { s.Placement, s.IsActive, s.DisplayOrder });
        });

        // ── PageLayoutConfig ──────────────────────────────────────────────────
        modelBuilder.Entity<PageLayoutConfig>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.PageName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(p => p.ComponentName)
                  .IsRequired()
                  .HasMaxLength(100);

            // One row per component per page
            entity.HasIndex(p => new { p.PageName, p.ComponentName })
                  .IsUnique();

            entity.Property(p => p.ConfigJson).HasColumnType("jsonb");
            entity.Property(p => p.ModifiedByUser).HasMaxLength(100);
        });

        // ── Division ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Division>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(d => d.ShortCode)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.Property(d => d.Season).IsRequired();

            entity.Property(d => d.AgeGroup).HasMaxLength(50);

            entity.Property(d => d.Gender)
                  .HasConversion<string>()
                  .HasMaxLength(10);

            entity.Property(d => d.Description).HasMaxLength(500);

            // Composite unique index on Season and ShortCode
            entity.HasIndex(d => new { d.Season, d.ShortCode }).IsUnique();
            entity.HasIndex(d => d.IsActive);
        });

        // ── MatchEvent ────────────────────────────────────────────────────────
        modelBuilder.Entity<MatchEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventType)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            entity.Property(e => e.Minute).IsRequired();

            entity.Property(e => e.AdditionalInfo).HasMaxLength(500);

            entity.HasIndex(e => e.MatchId);
            entity.HasIndex(e => e.PlayerId);

            // Check constraint for minute range
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_MatchEvent_Minute",
                "\"Minute\" >= 1 AND \"Minute\" <= 120"));

            entity.HasOne(e => e.Match)
                  .WithMany(m => m.MatchEvents)
                  .HasForeignKey(e => e.MatchId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Player)
                  .WithMany(p => p.MatchEvents)
                  .HasForeignKey(e => e.PlayerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Suspension ────────────────────────────────────────────────────────
        modelBuilder.Entity<Suspension>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Reason)
                  .IsRequired()
                  .HasMaxLength(500);

            entity.Property(s => s.StartDate).IsRequired();
            entity.Property(s => s.EndDate).IsRequired();
            entity.Property(s => s.MatchesSuspended).IsRequired();

            entity.Property(s => s.AppliedByUser).HasMaxLength(100);

            entity.HasIndex(s => s.PlayerId);
            entity.HasIndex(s => new { s.PlayerId, s.IsActive });

            // Check constraint for date validation
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Suspension_Dates",
                "\"EndDate\" > \"StartDate\""));

            entity.HasOne(s => s.Player)
                  .WithMany(p => p.Suspensions)
                  .HasForeignKey(s => s.PlayerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Update Team configuration ─────────────────────────────────────────
        modelBuilder.Entity<Team>()
            .HasOne(t => t.Division)
            .WithMany(d => d.Teams)
            .HasForeignKey(t => t.DivisionId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Update MatchResult configuration ──────────────────────────────────
        modelBuilder.Entity<MatchResult>()
            .HasOne(m => m.Division)
            .WithMany(d => d.Matches)
            .HasForeignKey(m => m.DivisionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    // ── Auto-stamp UpdatedAt ──────────────────────────────────────────────────
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
