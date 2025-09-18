using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CSContestConnect.Web.Models
{
    public enum EventApprovalStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }

    public class Event
    {
        public int Id { get; set; }

        [Required, StringLength(160)]
        public string Title { get; set; } = default!;

        [Required, StringLength(4000)]
        public string Description { get; set; } = default!;

        [Display(Name = "Starts At"), DataType(DataType.DateTime)]
        public DateTime StartsAt { get; set; }

        [Display(Name = "Ends At"), DataType(DataType.DateTime)]
        public DateTime EndsAt { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        public bool IsOnline { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 999999)]
        public decimal Price { get; set; }

        // Ownership — set by the server, not posted from the form
        [BindNever]             // don’t bind from request
        [ValidateNever]         // don’t validate on POST
        public string CreatedById { get; set; } = default!;

        [ValidateNever]
        public ApplicationUser? CreatedBy { get; set; }

        // Moderation
        public EventApprovalStatus ApprovalStatus { get; set; } = EventApprovalStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedById { get; set; }
    }
}
