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

        // ---- DateTimes: force UTC to satisfy Npgsql "timestamp with time zone"
        private DateTime _startsAt;
        [Display(Name = "Starts At"), DataType(DataType.DateTime)]
        public DateTime StartsAt
        {
            get => _startsAt;
            set => _startsAt = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private DateTime _endsAt;
        [Display(Name = "Ends At"), DataType(DataType.DateTime)]
        public DateTime EndsAt
        {
            get => _endsAt;
            set => _endsAt = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        [StringLength(200)]
        public string? Location { get; set; }

        public bool IsOnline { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 999999)]
        public decimal Price { get; set; }

        // ---- Cover Image (relative to /wwwroot)
        [StringLength(512)]
        public string? ImagePath { get; set; }

        // ---- Ticketing
        [Range(0, 100000)]
        public int? TicketCapacity { get; set; } // null = unlimited

        [Range(0, 100000)]
        public int RegisteredCount { get; set; } = 0;

        [NotMapped]
        public bool IsFull => TicketCapacity.HasValue && RegisteredCount >= TicketCapacity.Value;

        // ---- Ownership (server-set)
        [BindNever]
        [ValidateNever]
        public string CreatedById { get; set; } = default!;

        [ValidateNever]
        public ApplicationUser? CreatedBy { get; set; }

        // ---- Moderation
        public EventApprovalStatus ApprovalStatus { get; set; } = EventApprovalStatus.Pending;

        // Store as UTC
        private DateTime _createdAt = DateTime.UtcNow;
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => _createdAt = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private DateTime? _approvedAt;
        public DateTime? ApprovedAt
        {
            get => _approvedAt;
            set => _approvedAt = value.HasValue
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                : null;
        }

        public string? ApprovedById { get; set; }
    }
}
