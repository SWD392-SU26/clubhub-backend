using ClubHub.API.Entities;
using ClubHub.API.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubMember> ClubMembers => Set<ClubMember>();
    public DbSet<ClubProposal> ClubProposals => Set<ClubProposal>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.SystemRole).HasConversion<string>();
        });

        // ── Club ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Club>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Category).HasConversion<string>();
            e.Property(c => c.Status).HasConversion<string>();

            e.HasOne(c => c.Creator)
             .WithMany()
             .HasForeignKey(c => c.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ClubMember ───────────────────────────────────────────────────────
        modelBuilder.Entity<ClubMember>(e =>
        {
            e.HasKey(cm => cm.Id);
            e.HasIndex(cm => new { cm.UserId, cm.ClubId });
            e.Property(cm => cm.RoleInClub).HasConversion<string>();
            e.Property(cm => cm.Status).HasConversion<string>();

            e.HasOne(cm => cm.User)
             .WithMany(u => u.ClubMemberships)
             .HasForeignKey(cm => cm.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(cm => cm.Club)
             .WithMany(c => c.Members)
             .HasForeignKey(cm => cm.ClubId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ClubProposal ─────────────────────────────────────────────────────
        modelBuilder.Entity<ClubProposal>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Category).HasConversion<string>();
            e.Property(p => p.Status).HasConversion<string>();

            e.HasOne(p => p.Submitter)
             .WithMany(u => u.Proposals)
             .HasForeignKey(p => p.SubmittedBy)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.Reviewer)
             .WithMany()
             .HasForeignKey(p => p.ReviewedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Event ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Event>(e =>
        {
            e.HasKey(ev => ev.Id);
            e.Property(ev => ev.Status).HasConversion<string>();

            e.HasOne(ev => ev.Club)
             .WithMany(c => c.Events)
             .HasForeignKey(ev => ev.ClubId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ev => ev.Creator)
             .WithMany()
             .HasForeignKey(ev => ev.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── EventRegistration ────────────────────────────────────────────────
        modelBuilder.Entity<EventRegistration>(e =>
        {
            e.HasKey(er => er.Id);
            e.HasIndex(er => new { er.EventId, er.UserId }).IsUnique();

            e.HasOne(er => er.Event)
             .WithMany(ev => ev.Registrations)
             .HasForeignKey(er => er.EventId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(er => er.User)
             .WithMany(u => u.EventRegistrations)
             .HasForeignKey(er => er.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Feedback ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Feedback>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => new { f.EventId, f.UserId }).IsUnique();

            e.HasOne(f => f.Event)
             .WithMany(ev => ev.Feedbacks)
             .HasForeignKey(f => f.EventId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(f => f.User)
             .WithMany(u => u.Feedbacks)
             .HasForeignKey(f => f.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PointTransaction ─────────────────────────────────────────────────
        modelBuilder.Entity<PointTransaction>(e =>
        {
            e.HasKey(pt => pt.Id);
            e.Property(pt => pt.Type).HasConversion<string>();

            e.HasOne(pt => pt.User)
             .WithMany(u => u.PointTransactions)
             .HasForeignKey(pt => pt.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(pt => pt.Club)
             .WithMany(c => c.PointTransactions)
             .HasForeignKey(pt => pt.ClubId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Notification ─────────────────────────────────────────────────────
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);

            e.HasOne(n => n.User)
             .WithMany(u => u.Notifications)
             .HasForeignKey(n => n.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
