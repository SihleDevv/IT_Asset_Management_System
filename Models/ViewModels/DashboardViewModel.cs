namespace IT_Asset_Management_System.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalComputers { get; set; }
        public int TotalServers { get; set; }
        public int TotalApplications { get; set; }
        public int ActiveServers { get; set; }
        public int InactiveServers { get; set; }
        public int ComputersInUse { get; set; }
        public int ComputersAvailable { get; set; }
        public int ExpiringSoonLicenses { get; set; }
        public int ExpiredLicenses { get; set; }
        public int? TotalTickets { get; set; }
        public int? AssignedTickets { get; set; }
        public List<RecentActivity> RecentActivities { get; set; } = new List<RecentActivity>();
    }

    public class RecentActivity
    {
        public string? UserName { get; set; }
        public string? Action { get; set; }
        public string? EntityType { get; set; }
        public DateTime Timestamp { get; set; }
    }
}