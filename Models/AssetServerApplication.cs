using System.ComponentModel.DataAnnotations;

namespace IT_Asset_Management_System.Models
{
    public class AssetServerApplication
    {
        public int Id { get; set; }
        public int ServerId { get; set; }
        public virtual Server? Server { get; set; }
        public int ApplicationId { get; set; }
        public virtual Application? Application { get; set; }
        public DateTime InstallationDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string InstalledVersion { get; set; } = string.Empty;

        [StringLength(50)]
        public string Status { get; set; } = "Running";

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
    }
}