using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT_Asset_Management_System.Data;
using IT_Asset_Management_System.Models;

namespace IT_Asset_Management_System.Controllers
{
    [Authorize]
    public class ApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ApplicationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var applications = await _context.Applications
                .Where(a => a.AssetName != null && !string.IsNullOrWhiteSpace(a.AssetName))
                .ToListAsync();
            return View(applications);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var application = await _context.Applications
                .Include(a => a.ServerApplications)
                    .ThenInclude(sa => sa.Server)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        [Authorize(Policy = "RequireAdmin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Create([Bind("AssetTag,AssetName,AssetType,Version,Vendor,Category,Description,PurchaseDate,PurchasePrice,RequiresLicense,LicenseKey,LicenseType,TotalLicenses,UsedLicenses,LicenseExpiryDate,Status,Notes")] Application application)
        {
            if (ModelState.IsValid)
            {
                application.AssetType = "Application";
                application.CreatedBy = User.Identity?.Name;
                application.CreatedDate = DateTime.Now;
                _context.Add(application);
                await _context.SaveChangesAsync();

                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "",
                    Action = "Create",
                    EntityType = "Application",
                    EntityId = application.Id,
                    Details = $"Created application: {application.AssetName}",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                });
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(application);
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var application = await _context.Applications.FindAsync(id);
            if (application == null)
            {
                return NotFound();
            }
            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AssetTag,AssetName,AssetType,Version,Vendor,Category,Description,PurchaseDate,PurchasePrice,RequiresLicense,LicenseKey,LicenseType,TotalLicenses,UsedLicenses,LicenseExpiryDate,Status,Notes")] Application application)
        {
            if (id != application.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingApp = await _context.Applications.FindAsync(id);
                    if (existingApp != null)
                    {
                        existingApp.AssetTag = application.AssetTag;
                        existingApp.AssetName = application.AssetName;
                        existingApp.AssetType = "Application";
                        existingApp.Version = application.Version;
                        existingApp.Vendor = application.Vendor;
                        existingApp.Category = application.Category;
                        existingApp.Description = application.Description;
                        existingApp.PurchaseDate = application.PurchaseDate;
                        existingApp.PurchasePrice = application.PurchasePrice;
                        existingApp.RequiresLicense = application.RequiresLicense;
                        existingApp.LicenseKey = application.LicenseKey;
                        existingApp.LicenseType = application.LicenseType;
                        existingApp.TotalLicenses = application.TotalLicenses;
                        existingApp.UsedLicenses = application.UsedLicenses;
                        existingApp.LicenseExpiryDate = application.LicenseExpiryDate;
                        existingApp.Status = application.Status;
                        existingApp.Notes = application.Notes;
                        existingApp.ModifiedBy = User.Identity?.Name;
                        existingApp.ModifiedDate = DateTime.Now;

                        await _context.SaveChangesAsync();

                        _context.AuditLogs.Add(new AuditLog
                        {
                            UserName = User.Identity?.Name ?? "",
                            Action = "Update",
                            EntityType = "Application",
                            EntityId = application.Id,
                            Details = $"Updated application: {application.AssetName}",
                            IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                        });
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApplicationExists(application.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(application);
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var application = await _context.Applications
                .Include(a => a.ServerApplications)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (application == null)
            {
                return NotFound();
            }

            // Check if application is installed on any servers
            var serverCount = application.ServerApplications?.Count ?? 0;
            ViewBag.IsInstalledOnServers = serverCount > 0;
            ViewBag.ServerCount = serverCount;

            return View(application);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var application = await _context.Applications
                .Include(a => a.ServerApplications)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (application == null)
            {
                return NotFound();
            }

            // Check if application is installed on any servers
            var serverCount = application.ServerApplications?.Count ?? 0;
            if (serverCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete application '{application.AssetName}'. It is installed on {serverCount} server(s). Please remove it from all servers before deleting the application.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Applications.Remove(application);
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserName = User.Identity?.Name ?? "",
                Action = "Delete",
                EntityType = "Application",
                EntityId = id,
                Details = $"Deleted application: {application.AssetName}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
            });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool ApplicationExists(int id)
        {
            return _context.Applications.Any(e => e.Id == id);
        }
    }
}