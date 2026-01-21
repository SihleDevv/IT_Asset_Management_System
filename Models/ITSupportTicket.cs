using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Asset_Management_System.Models
{
    public class ITSupportTicket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        [Display(Name = "Issue Description")]
        public string Description { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending"; // Pending, In Progress, Resolved, Closed

        [StringLength(50)]
        [Display(Name = "Priority")]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical

        [Required]
        [StringLength(50)]
        [Display(Name = "Asset Type")]
        public string AssetType { get; set; } = string.Empty; // Computer, Server, Application

        [Required]
        [Display(Name = "Related Asset ID")]
        public int RelatedAssetId { get; set; }

        [StringLength(200)]
        [Display(Name = "Related Asset Name")]
        public string? RelatedAssetName { get; set; }

        [Required]
        [Display(Name = "Reported By")]
        public string ReportedByUserId { get; set; } = string.Empty;

        [ForeignKey("ReportedByUserId")]
        public virtual ApplicationUser? ReportedByUser { get; set; }

        [StringLength(450)]
        [Display(Name = "Assigned To")]
        public string? AssignedToUserId { get; set; }

        [ForeignKey("AssignedToUserId")]
        public virtual ApplicationUser? AssignedToUser { get; set; }

        [StringLength(2000)]
        [Display(Name = "Admin Response")]
        public string? AdminResponse { get; set; }

        [StringLength(2000)]
        [Display(Name = "Resolution Notes")]
        public string? ResolutionNotes { get; set; }

        [StringLength(2000)]
        [Display(Name = "Technician Notes")]
        public string? TechnicianNotes { get; set; }

        [Display(Name = "Technician Notes Date")]
        public DateTime? TechnicianNotesDate { get; set; }

        [Display(Name = "Status Changed Date")]
        public DateTime? StatusChangedDate { get; set; }

        [StringLength(450)]
        [Display(Name = "Status Changed By")]
        public string? StatusChangedByUserId { get; set; }

        [ForeignKey("StatusChangedByUserId")]
        public virtual ApplicationUser? StatusChangedByUser { get; set; }

        [Display(Name = "Replacement Requested")]
        public bool ReplacementRequested { get; set; } = false;

        [StringLength(2000)]
        [Display(Name = "Replacement Reason")]
        public string? ReplacementReason { get; set; }

        [Display(Name = "Replacement Approved")]
        public bool? ReplacementApproved { get; set; }

        [StringLength(2000)]
        [Display(Name = "Admin Response to Replacement")]
        public string? ReplacementAdminResponse { get; set; }

        [StringLength(2000)]
        [Display(Name = "User Follow Up")]
        public string? UserFollowUp { get; set; }

        [Display(Name = "Follow Up Date")]
        public DateTime? FollowUpDate { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Updated Date")]
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Last Action Date")]
        public DateTime? LastActionDate { get; set; }

        [Display(Name = "Resolved Date")]
        public DateTime? ResolvedDate { get; set; }
    }
}
