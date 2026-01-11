using System.ComponentModel.DataAnnotations;

namespace IT_Asset_Management_System.Models
{
    // Server inherits from BaseAsset - contains ONLY Server-specific fields (3NF compliant, no duplicates)
    public class Server : BaseAsset
    {
        [StringLength(100)]
        [Display(Name = "IP Address")]
        public string IPAddress { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Server Type")]
        public string ServerType { get; set; } = string.Empty;

        [StringLength(100)]
        public string Purpose { get; set; } = string.Empty;

        [StringLength(50)]
        public string Processor { get; set; } = string.Empty;

        [Display(Name = "RAM (GB)")]
        public int RAM { get; set; }

        [Display(Name = "Storage (GB)")]
        public int Storage { get; set; }

        [StringLength(50)]
        [Display(Name = "Operating System")]
        public string OperatingSystem { get; set; } = string.Empty;

        [Display(Name = "Backup Required")]
        public bool BackupRequired { get; set; } = false;

        [StringLength(500)]
        [Display(Name = "Backup Comments")]
        public string BackupComments { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Project Manager Name")]
        public string ProjectManagerName { get; set; } = string.Empty;

        // Legacy navigation property (kept for backward compatibility with ServerApplication)
        public virtual ICollection<ServerApplication> ServerApplications { get; set; } = new List<ServerApplication>();
    }
}