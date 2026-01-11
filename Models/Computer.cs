using System.ComponentModel.DataAnnotations;

namespace IT_Asset_Management_System.Models
{
    // Computer inherits from BaseAsset - contains ONLY Computer-specific fields (3NF compliant, no duplicates)
    public class Computer : BaseAsset
    {
        [StringLength(50)]
        public string Processor { get; set; } = string.Empty;

        [Display(Name = "RAM (GB)")]
        public int RAM { get; set; }

        [Display(Name = "Storage (GB)")]
        public int Storage { get; set; }

        [StringLength(50)]
        [Display(Name = "Operating System")]
        public string OperatingSystem { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Assigned To")]
        public string AssignedTo { get; set; } = string.Empty;
    }
}
