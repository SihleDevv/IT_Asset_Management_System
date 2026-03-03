using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var isAdminOrITManager = User.IsInRole("Admin") || User.IsInRole("IT Manager");
            var isITSupportSupervisor = User.IsInRole("IT Support Supervisor");
            var isITSupport = User.IsInRole("IT Support") && !isITSupportSupervisor && !isAdminOrITManager;

            // Base query filters
            var baseComputerQuery = _context.Computers
                .Where(c => !string.IsNullOrEmpty(c.AssetTag) && !string.IsNullOrEmpty(c.AssetName));
            
            var baseServerQuery = _context.Servers
                .Where(s => !string.IsNullOrEmpty(s.AssetTag) && !string.IsNullOrEmpty(s.AssetName));
            
            var baseApplicationQuery = _context.Applications
                .Where(a => !string.IsNullOrEmpty(a.AssetTag) && !string.IsNullOrEmpty(a.AssetName));

            // Filter by user assignment for Employee/Read Only users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email;
                var userEmail = currentUser.Email;

#pragma warning disable CS8602 // Dereference of a possibly null reference - null checks are performed before dereferencing
                // Filter computers assigned to user
                baseComputerQuery = baseComputerQuery
                    .Where(c => c.AssignedTo != null && c.AssignedTo != "" &&
                                (c.AssignedTo!.ToLower() == userFullName.ToLower() || 
                                c.AssignedTo!.ToLower() == userEmail!.ToLower()) &&
                                c.AssignedTo!.ToLower() != "unassigned");

                // Filter servers where user is project manager
                baseServerQuery = baseServerQuery
                    .Where(s => s.ProjectManagerName != null && s.ProjectManagerName != "" && 
                                (s.ProjectManagerName!.ToLower() == userFullName.ToLower() || 
                                 s.ProjectManagerName!.ToLower() == userEmail!.ToLower()));

                // Filter applications where user is owner
                baseApplicationQuery = baseApplicationQuery
                    .Where(a => a.ApplicationOwner != null && a.ApplicationOwner != "" && 
                                (a.ApplicationOwner!.ToLower() == userFullName.ToLower() || 
                                 a.ApplicationOwner!.ToLower() == userEmail!.ToLower()));
#pragma warning restore CS8602
            }

            var viewModel = new DashboardViewModel
            {
                // Query derived types directly for accurate counts
                TotalComputers = await baseComputerQuery.CountAsync(),
                TotalServers = await baseServerQuery.CountAsync(),
                TotalApplications = await baseApplicationQuery.CountAsync(),
                ActiveServers = await baseServerQuery
                    .Where(s => s.Status == "Active")
                    .CountAsync(),
                InactiveServers = await baseServerQuery
                    .Where(s => s.Status != "Active")
                    .CountAsync(),
                ComputersInUse = await baseComputerQuery
                    .Where(c => c.Status == "In Use")
                    .CountAsync(),
                ComputersAvailable = await baseComputerQuery
                    .Where(c => c.Status == "Available")
                    .CountAsync(),
                ExpiringSoonLicenses = await baseApplicationQuery
                    .Where(a => a.RequiresLicense && 
                               a.LicenseExpiryDate.HasValue &&
                               a.LicenseExpiryDate.Value <= DateTime.Now.AddDays(30) &&
                               a.LicenseExpiryDate.Value > DateTime.Now)
                    .CountAsync(),
                ExpiredLicenses = await baseApplicationQuery
                    .Where(a => a.RequiresLicense && 
                               a.LicenseExpiryDate.HasValue &&
                               a.LicenseExpiryDate.Value < DateTime.Now)
                    .CountAsync(),
                // Ticket metrics for IT Support roles
                TotalTickets = (isITSupportSupervisor || isITSupport) 
                    ? await _context.ITSupportTickets.CountAsync() 
                    : null,
                AssignedTickets = isITSupport 
                    ? await _context.ITSupportTickets
                        .Where(t => t.AssignedToUserId == currentUser.Id)
                        .CountAsync() 
                    : null,
                RecentActivities = isAdminOrITManager
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