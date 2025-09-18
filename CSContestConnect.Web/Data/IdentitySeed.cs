using CSContestConnect.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CSContestConnect.Web.Data
{
    public static class IdentitySeed
    {
        public const string RoleAdmin = "Admin";
        public const string RoleUser  = "User";

        /// <summary>
        /// Runs EF migrations, ensures roles exist, and creates a seed admin user.
        /// Idempotent: safe to call on every startup.
        /// </summary>
        public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            var db        = sp.GetRequiredService<AppDbContext>();
            var roleMgr   = sp.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr   = sp.GetRequiredService<UserManager<ApplicationUser>>();

            // 1) Apply migrations
            await db.Database.MigrateAsync();

            // 2) Ensure roles
            foreach (var role in new[] { RoleAdmin, RoleUser })
            {
                if (!await roleMgr.RoleExistsAsync(role))
                    await roleMgr.CreateAsync(new IdentityRole(role));
            }

            // 3) Ensure seed admin
            var adminEmail = config["SeedAdmin:Email"]    ?? "admin@cscontest.local";
            var adminPass  = config["SeedAdmin:Password"] ?? "Admin#12345";

            var admin = await userMgr.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,        // required since you set RequireConfirmedEmail = true
                    DisplayName = "Site Admin"
                };
                var create = await userMgr.CreateAsync(admin, adminPass);
                if (!create.Succeeded)
                    throw new Exception("Seed admin creation failed: " + string.Join(", ", create.Errors.Select(e => e.Description)));
            }

            if (!await userMgr.IsInRoleAsync(admin, RoleAdmin))
                await userMgr.AddToRoleAsync(admin, RoleAdmin);
        }
    }
}
