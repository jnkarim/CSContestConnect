using CSContestConnect.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CSContestConnect.Web.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<Event> Events => Set<Event>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ----- ApplicationUser <-> UserProfile (1:1) -----
            // FK lives on ApplicationUser.UserProfileId (nullable)
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.UserProfile)
                .WithOne() // no back navigation on UserProfile
                .HasForeignKey<ApplicationUser>(u => u.UserProfileId)
                .OnDelete(DeleteBehavior.SetNull);

            // If you want to be explicit:
            builder.Entity<ApplicationUser>()
                .Property(u => u.UserProfileId)
                .IsRequired(false);

            // NOTE: You don't need a unique index on UserProfile.Id — it's already the PK.

            // ----- Event configuration -----
            builder.Entity<Event>()
                .HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Event>()
                .Property(e => e.Price)
                .HasPrecision(10, 2);
        }
    }
}
