using System.ComponentModel.DataAnnotations;

namespace IT_Asset_Management_System.Models
{
    // Abstract base class - contains ONLY fields shared by ALL asset types (3NF compliant, no duplicates)
    public abstract class BaseAsset
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Asset Tag")]
        public string AssetTag { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Asset Name")]
        public string AssetName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Asset Type")]
        public string AssetType { get; set; } = string.Empty; // Computer, Server, Application

        [StringLength(100)]
        public string Brand { get; set; } = string.Empty;

        [StringLength(100)]
        public string Model { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; } = string.Empty;

        [Display(Name = "Purchase Date")]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        [Range(0, double.MaxValue)]
        [Display(Name = "Purchase Price")]
        public decimal? PurchasePrice { get; set; }

        [StringLength(100)]
        public string Vendor { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Vendor Type")]
        public string VendorType { get; set; } = string.Empty;

        [Display(Name = "Warranty Expiry Date")]
        public DateTime? WarrantyExpiryDate { get; set; }

        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active";

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
