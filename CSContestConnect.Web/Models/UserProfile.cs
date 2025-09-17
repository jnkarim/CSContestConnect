using System;
using System.ComponentModel.DataAnnotations;

namespace CSContestConnect.Web.Models
{
    public class UserProfile
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();

        // Basics
        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(160)]
        public string? Bio { get; set; }
        public DateTime? DateOfBirth { get; set; }
        [StringLength(20)] public string? Gender { get; set; }

        // Contacts
        [Phone, StringLength(30)] public string? Phone { get; set; }
        [StringLength(200)] public string? Website { get; set; }
        [StringLength(200)] public string? LinkedIn { get; set; }
        [StringLength(200)] public string? GitHub { get; set; }

        // Location
        [StringLength(100)] public string? Country { get; set; }
        [StringLength(100)] public string? City { get; set; }

        // Education
        [StringLength(120)] public string? School { get; set; }
        [StringLength(120)] public string? College { get; set; }
        [StringLength(120)] public string? University { get; set; }
        [StringLength(120)] public string? Degree { get; set; }
        public int? GraduationYear { get; set; }

        // Image (relative path like /uploads/profiles/abc.jpg)
        [StringLength(260)] public string? ProfileImagePath { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}