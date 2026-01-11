using System.ComponentModel.DataAnnotations;

namespace IT_Asset_Management_System.Models
{
    // Application inherits from BaseAsset - contains ONLY Application-specific fields (3NF compliant, no duplicates)
    public class Application : BaseAsset
    {
        [StringLength(100)]
        public string Version { get; set; } = string.Empty;

        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Requires License")]
        public bool RequiresLicense { get; set; } = false;

        [StringLength(200)]
        [Display(Name = "License Key")]
        public string LicenseKey { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "License Type")]
        public string LicenseType { get; set; } = string.Empty;

        [Display(Name = "Total Licenses")]
        public int? TotalLicenses { get; set; }

        [Display(Name = "Used Licenses")]
        public int? UsedLicenses { get; set; }

        [Display(Name = "License Expiry Date")]
        public DateTime? LicenseExpiryDate { get; set; }

        [StringLength(100)]
        [Display(Name = "Business Unit")]
        public string BusinessUnit { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Application Owner")]
        public string ApplicationOwner { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "License Holder")]
        public string LicenseHolder { get; set; } = string.Empty;

        // Legacy navigation property (kept for backward compatibility with ServerApplication)
        public virtual ICollection<ServerApplication> ServerApplications { get; set; } = new List<ServerApplication>();
    }
}
