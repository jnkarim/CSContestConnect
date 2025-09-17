using CSContestConnect.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CSContestConnect.Web.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Add DbSet for UserProfile
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure one-to-one relationship between ApplicationUser and UserProfile
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.UserProfile)
                .WithOne() // no navigation back on UserProfile
                .HasForeignKey<ApplicationUser>(u => u.UserProfileId)
                .OnDelete(DeleteBehavior.SetNull); // Changed from Cascade to SetNull for safety
                
            // Configure UserProfile
            builder.Entity<UserProfile>()
                .HasIndex(p => p.Id)
                .IsUnique();
        }
    }
}