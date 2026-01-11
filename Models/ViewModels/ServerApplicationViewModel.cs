using IT_Asset_Management_System.Models;

namespace IT_Asset_Management_System.ViewModels
{
    public class ServerApplicationViewModel
    {
        public Server? Server { get; set; }
        public List<Application> AvailableApplications { get; set; } = new List<Application>();
        public List<ServerApplication> InstalledApplications { get; set; } = new List<ServerApplication>();
    }
}