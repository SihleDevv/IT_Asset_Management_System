using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using IT_Asset_Management_System.Data;
using IT_Asset_Management_System.Models;
using IT_Asset_Management_System.ViewModels;

namespace IT_Asset_Management_System.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel
            {
                // Query derived types directly for accurate counts
                TotalComputers = await _context.Computers
                    .CountAsync(c => !string.IsNullOrEmpty(c.AssetTag) && !string.IsNullOrEmpty(c.AssetName)),
                TotalServers = await _context.Servers
                    .CountAsync(s => !string.IsNullOrEmpty(s.AssetTag) && !string.IsNullOrEmpty(s.AssetName)),
                TotalApplications = await _context.Applications
                    .CountAsync(a => !string.IsNullOrEmpty(a.AssetTag) && !string.IsNullOrEmpty(a.AssetName)),
                ActiveServers = await _context.Servers
                    .CountAsync(s => s.Status == "Active" && !string.IsNullOrEmpty(s.AssetTag) && !string.IsNullOrEmpty(s.AssetName)),
                InactiveServers = await _context.Servers
                    .CountAsync(s => s.Status != "Active" && !string.IsNullOrEmpty(s.AssetTag) && !string.IsNullOrEmpty(s.AssetName)),
                ComputersInUse = await _context.Computers
                    .CountAsync(c => c.Status == "In Use" && !string.IsNullOrEmpty(c.AssetTag) && !string.IsNullOrEmpty(c.AssetName)),
                ComputersAvailable = await _context.Computers
                    .CountAsync(c => c.Status == "Available" && !string.IsNullOrEmpty(c.AssetTag) && !string.IsNullOrEmpty(c.AssetName)),
                ExpiringSoonLicenses = await _context.Applications
                    .CountAsync(a => a.RequiresLicense && a.LicenseExpiryDate.HasValue
                        && a.LicenseExpiryDate.Value <= DateTime.Now.AddDays(30)
                        && a.LicenseExpiryDate.Value > DateTime.Now
                        && !string.IsNullOrEmpty(a.AssetTag) && !string.IsNullOrEmpty(a.AssetName)),
                ExpiredLicenses = await _context.Applications
                    .CountAsync(a => a.RequiresLicense && a.LicenseExpiryDate.HasValue
                        && a.LicenseExpiryDate.Value < DateTime.Now
                        && !string.IsNullOrEmpty(a.AssetTag) && !string.IsNullOrEmpty(a.AssetName)),
                RecentActivities = (User.IsInRole("Admin") || User.IsInRole("IT Manager"))
                    ? await _context.AuditLogs
                        .OrderByDescending(a => a.Timestamp)
                        .Take(10)
                        .Select(a => new RecentActivity
                        {
                            UserName = a.UserName,
                            Action = a.Action,
                            EntityType = a.EntityType,
                            Timestamp = a.Timestamp
                        })
                        .ToListAsync()
                    : new List<RecentActivity>()
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}