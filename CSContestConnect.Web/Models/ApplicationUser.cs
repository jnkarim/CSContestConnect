using Microsoft.AspNetCore.Identity;

namespace CSContestConnect.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        
        // Foreign key to UserProfile
        public Guid? UserProfileId { get; set; }
        
        // Navigation property
        public UserProfile? UserProfile { get; set; }
        
        // Add any other custom properties you need
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}