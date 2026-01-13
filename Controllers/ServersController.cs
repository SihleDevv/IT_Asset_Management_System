using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT_Asset_Management_System.Data;
using IT_Asset_Management_System.Models;
using IT_Asset_Management_System.ViewModels;

namespace IT_Asset_Management_System.Controllers
{
    [Authorize]
    public class ServersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var servers = await _context.Servers
                .Include(s => s.ServerApplications)
                    .ThenInclude(sa => sa.Application)
                .Where(s => s.AssetTag != null 
                    && s.AssetName != null 
                    && !string.IsNullOrWhiteSpace(s.AssetTag) 
                    && !string.IsNullOrWhiteSpace(s.AssetName))
                .ToListAsync();
            return View(servers);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var server = await _context.Servers
                .Include(s => s.ServerApplications)
                    .ThenInclude(sa => sa.Application)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (server == null)
            {
                return NotFound();
            }

            return View(server);
        }

        [Authorize(Policy = "RequireAdmin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Create([Bind("AssetTag,AssetName,AssetType,IPAddress,Brand,Model,SerialNumber,Processor,RAM,Storage,OperatingSystem,ServerType,PurchaseDate,PurchasePrice,Vendor,WarrantyExpiryDate,Location,Status,Notes")] Server server)
        {
            if (ModelState.IsValid)
            {
                server.AssetType = "Server";
                server.CreatedBy = User.Identity?.Name;
                server.CreatedDate = DateTime.Now;
                _context.Add(server);
                await _context.SaveChangesAsync();

                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "",
                    Action = "Create",
                    EntityType = "Server",
                    EntityId = server.Id,
                    Details = $"Created server: {server.AssetName}",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                });
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(server);
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var server = await _context.Servers.FindAsync(id);
            if (server == null)
            {
                return NotFound();
            }
            return View(server);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AssetTag,AssetName,AssetType,IPAddress,Brand,Model,SerialNumber,Processor,RAM,Storage,OperatingSystem,ServerType,PurchaseDate,PurchasePrice,Vendor,WarrantyExpiryDate,Location,Status,Notes")] Server server)
        {
            if (id != server.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingServer = await _context.Servers.FindAsync(id);
                    if (existingServer != null)
                    {
                        existingServer.AssetTag = server.AssetTag;
                        existingServer.AssetName = server.AssetName;
                        existingServer.AssetType = "Server";
                        existingServer.IPAddress = server.IPAddress;
                        existingServer.Brand = server.Brand;
                        existingServer.Model = server.Model;
                        existingServer.SerialNumber = server.SerialNumber;
                        existingServer.Processor = server.Processor;
                        existingServer.RAM = server.RAM;
                        existingServer.Storage = server.Storage;
                        existingServer.OperatingSystem = server.OperatingSystem;
                        existingServer.ServerType = server.ServerType;
                        existingServer.PurchaseDate = server.PurchaseDate;
                        existingServer.PurchasePrice = server.PurchasePrice;
                        existingServer.Vendor = server.Vendor;
                        existingServer.WarrantyExpiryDate = server.WarrantyExpiryDate;
                        existingServer.Location = server.Location;
                        existingServer.Status = server.Status;
                        existingServer.Notes = server.Notes;
                        existingServer.ModifiedBy = User.Identity?.Name;
                        existingServer.ModifiedDate = DateTime.Now;

                        await _context.SaveChangesAsync();

                        _context.AuditLogs.Add(new AuditLog
                        {
                            UserName = User.Identity?.Name ?? "",
                            Action = "Update",
                            EntityType = "Server",
                            EntityId = server.Id,
                            Details = $"Updated server: {server.AssetName}",
                            IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                        });
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServerExists(server.Id))
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
            return View(server);
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var server = await _context.Servers
                .Include(s => s.ServerApplications)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (server == null)
            {
                return NotFound();
            }

            // Check if server has installed applications
            var installedAppCount = server.ServerApplications?.Count ?? 0;
            ViewBag.HasInstalledApplications = installedAppCount > 0;
            ViewBag.InstalledApplicationCount = installedAppCount;

            return View(server);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var server = await _context.Servers
                .Include(s => s.ServerApplications)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            if (server == null)
            {
                return NotFound();
            }

            // Check if server has installed applications
            var installedAppCount = server.ServerApplications?.Count ?? 0;
            if (installedAppCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete server '{server.AssetName}'. It has {installedAppCount} installed application(s). Please remove all applications before deleting the server.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Servers.Remove(server);
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserName = User.Identity?.Name ?? "",
                Action = "Delete",
                EntityType = "Server",
                EntityId = id,
                Details = $"Deleted server: {server.AssetName}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
            });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> ManageApplications(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var server = await _context.Servers
                .Include(s => s.ServerApplications)
                    .ThenInclude(sa => sa.Application)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (server == null)
            {
                return NotFound();
            }

            var installedAppIds = server.ServerApplications.Select(sa => sa.ApplicationId).ToList();
            var availableApplications = await _context.Applications
                .Where(a => !installedAppIds.Contains(a.Id))
                .ToListAsync();

            var viewModel = new ServerApplicationViewModel
            {
                Server = server,
                AvailableApplications = availableApplications,
                InstalledApplications = server.ServerApplications.ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> InstallApplication(int serverId, int applicationId, string installedVersion, string status, string? notes)
        {
            // Verify server and application exist
            var server = await _context.Servers.FindAsync(serverId);
            var application = await _context.Applications.FindAsync(applicationId);

            if (server == null || application == null)
            {
                return NotFound();
            }

            // Check if application is already installed
            var existingInstallation = await _context.ServerApplications
                .FirstOrDefaultAsync(sa => sa.ServerId == serverId && sa.ApplicationId == applicationId);

            if (existingInstallation != null)
            {
                TempData["ErrorMessage"] = "This application is already installed on this server.";
                return RedirectToAction(nameof(ManageApplications), new { id = serverId });
            }

            var serverApp = new ServerApplication
            {
                ServerId = serverId,
                ApplicationId = applicationId,
                InstallationDate = DateTime.Now,
                InstalledVersion = installedVersion ?? application.Version,
                Status = status ?? "Running",
                Notes = notes ?? string.Empty,
                CreatedBy = User.Identity?.Name ?? "System",
                CreatedDate = DateTime.Now
            };

            _context.ServerApplications.Add(serverApp);
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserName = User.Identity?.Name ?? "",
                Action = "Install Application",
                EntityType = "ServerApplication",
                EntityId = serverApp.Id,
                Details = $"Installed {application.AssetName} (Version: {serverApp.InstalledVersion}) on {server.AssetName} by {User.Identity?.Name}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Application '{application.AssetName}' has been successfully installed on '{server.AssetName}'.";

            return RedirectToAction(nameof(ManageApplications), new { id = serverId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> UninstallApplication(int id, int serverId)
        {
            var serverApp = await _context.ServerApplications
                .Include(sa => sa.Server)
                .Include(sa => sa.Application)
                .FirstOrDefaultAsync(sa => sa.Id == id);

            if (serverApp != null)
            {
                _context.ServerApplications.Remove(serverApp);
                await _context.SaveChangesAsync();

                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "",
                    Action = "Uninstall Application",
                    EntityType = "ServerApplication",
                    Details = $"Uninstalled {serverApp.Application?.AssetName} from {serverApp.Server?.AssetName}",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ManageApplications), new { id = serverId });
        }

        private bool ServerExists(int id)
        {
            return _context.Servers.Any(e => e.Id == id);
        }
    }
}