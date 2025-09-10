using CSContestConnect.Web.Data;
using CSContestConnect.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// 👉 add (optional but helpful) using directives
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

// --- EF Core + PostgreSQL ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Identity (User + Role) ---
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // beginner-friendly password policy (tighten later!)
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.SignIn.RequireConfirmedAccount = false; // for dev/testing
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

//  Add Google authentication (works with Identity cookies)
builder.Services
    .AddAuthentication(options =>
    {
        // Identity already configures cookie schemes; keeping defaults is fine.
        // These two lines are optional when using AddIdentity, but safe:
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddGoogle(options =>
    {
        // Keep secrets outside source code: appsettings.json / user-secrets / env vars
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

        // Optional but handy:
        options.SaveTokens = true;
        // options.CallbackPath = "/signin-google"; // default—change only if you need a custom path
        // options.Scope.Add("profile"); // uncomment if you need extra scopes
    });

// MVC with Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Auto-run migrations on startup (handy in dev)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // must be before Authorization
app.UseAuthorization();

// If you use an external login button (e.g., /Account/ExternalLogin),
// Identity or your controller will issue the Google challenge automatically.

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
 