using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT_Asset_Management_System.Data;
using IT_Asset_Management_System.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IT_Asset_Management_System.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // List assets where AssetType == "Computer"
        public async Task<IActionResult> ComputerReport()
        {
            var computers = await _context.Computers
                .Where(c => !string.IsNullOrEmpty(c.AssetTag) 
                    && !string.IsNullOrEmpty(c.AssetName)
                    && c.AssetTag != null
                    && c.AssetName != null)
                .OrderBy(c => c.Status)
                .ThenBy(c => c.AssetName)
                .ToListAsync();

            return View(computers);
        }

        // List assets where AssetType == "Server"
        public async Task<IActionResult> ServerReport()
        {
            var servers = await _context.Servers
                .Include(s => s.ServerApplications)
                    .ThenInclude(sa => sa.Application)
                .Where(s => !string.IsNullOrEmpty(s.AssetTag) 
                    && !string.IsNullOrEmpty(s.AssetName)
                    && s.AssetTag != null
                    && s.AssetName != null)
                .OrderBy(s => s.Status)
                .ThenBy(s => s.AssetName)
                .ToListAsync();

            // Get application counts for each server using ServerApplications
            var applicationCounts = servers.ToDictionary(
                s => s.Id, 
                s => s.ServerApplications?.Count ?? 0
            );

            ViewBag.ApplicationCounts = applicationCounts;
            return View(servers);
        }

        // List assets where AssetType == "Application"
        public async Task<IActionResult> ApplicationReport()
        {
            var applications = await _context.Applications
                .Include(a => a.ServerApplications)
                    .ThenInclude(sa => sa.Server)
                .Where(a => !string.IsNullOrEmpty(a.AssetTag) 
                    && !string.IsNullOrEmpty(a.AssetName)
                    && a.AssetTag != null
                    && a.AssetName != null)
                .OrderBy(a => a.Status)
                .ThenBy(a => a.AssetName)
                .ToListAsync();

            // Get server counts for each application using ServerApplications
            var serverCounts = applications.ToDictionary(
                a => a.Id, 
                a => a.ServerApplications?.Count ?? 0
            );

            ViewBag.ServerCounts = serverCounts;
            return View(applications);
        }

        // Applications requiring a license
        public async Task<IActionResult> LicenseReport()
        {
            var applications = await _context.Applications
                .Where(a => a.RequiresLicense && !string.IsNullOrEmpty(a.AssetTag) && !string.IsNullOrEmpty(a.AssetName))
                .OrderBy(a => a.LicenseExpiryDate)
                .ToListAsync();

            return View(applications);
        }

        // Recent audit logs
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> AuditLog()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(500)
                .ToListAsync();
            return View(logs);
        }

        // Clear all audit logs
        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAuditLogs()
        {
            var logCount = await _context.AuditLogs.CountAsync();
            
            // Clear all logs
            var allLogs = await _context.AuditLogs.ToListAsync();
            _context.AuditLogs.RemoveRange(allLogs);
            await _context.SaveChangesAsync();

            // Log the clear action after clearing
            _context.AuditLogs.Add(new AuditLog
            {
                UserName = User.Identity?.Name ?? "",
                Action = "Clear Audit Logs",
                EntityType = "System",
                EntityId = null,
                Details = $"Cleared {logCount} audit log entries",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully cleared {logCount} audit log entries.";
            return RedirectToAction(nameof(AuditLog));
        }

        // Summary counts and grouped status
        public async Task<IActionResult> AssetSummary()
        {
            ViewBag.TotalComputers = await _context.Computers.CountAsync();
            ViewBag.TotalServers = await _context.Servers.CountAsync();
            ViewBag.TotalApplications = await _context.Applications.CountAsync();

            ViewBag.ComputersByStatus = await _context.Computers
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.ServersByStatus = await _context.Servers
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.ApplicationsByStatus = await _context.Applications
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return View();
        }
    }
}
