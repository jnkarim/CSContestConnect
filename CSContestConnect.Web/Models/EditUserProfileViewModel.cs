using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CSContestConnect.Web.Models.ViewModels
{
    public class EditUserProfileViewModel
    {
        // Readonly display (from Identity)
        public string Email { get; set; } = string.Empty;

        // Basics
        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(160)]
        public string? Bio { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        // Contacts
        [Phone, StringLength(30)]
        public string? Phone { get; set; }

        [Url, StringLength(200)]
        public string? Website { get; set; }

        [Url, StringLength(200)]
        public string? LinkedIn { get; set; }

        [Url, StringLength(200)]
        public string? GitHub { get; set; }

        // Location
        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        // Education
        [StringLength(120)]
        public string? School { get; set; }

        [StringLength(120)]
        public string? College { get; set; }

        [StringLength(120)]
        public string? University { get; set; }

        [StringLength(120)]
        public string? Degree { get; set; }

        public int? GraduationYear { get; set; }

        // Image
        public string? CurrentImagePath { get; set; }  // show existing
        public IFormFile? ProfileImageFile { get; set; } // upload
    }
}