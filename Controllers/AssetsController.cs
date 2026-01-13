using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT_Asset_Management_System.Data;
using IT_Asset_Management_System.Models;

namespace IT_Asset_Management_System.Controllers
{
    [Authorize]
    public class AssetsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssetsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? assetType, string? searchTerm)
        {
            var query = _context.Assets.AsQueryable();

            if (!string.IsNullOrEmpty(assetType))
            {
                query = query.Where(a => a.AssetType == assetType);
            }

            // Filter out null records - exclude assets with null AssetTag or AssetName
            query = query.Where(a => a.AssetTag != null 
                && a.AssetName != null 
                && !string.IsNullOrWhiteSpace(a.AssetTag) 
                && !string.IsNullOrWhiteSpace(a.AssetName));

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(a => 
                    a.AssetTag.Contains(searchTerm) ||
                    a.AssetName.Contains(searchTerm) ||
                    (a.Brand != null && a.Brand.Contains(searchTerm)) ||
                    (a.Location != null && a.Location.Contains(searchTerm)) ||
                    (a.Status != null && a.Status.Contains(searchTerm))
                );
            }

            ViewBag.AssetType = assetType;
            ViewBag.SearchTerm = searchTerm;
            var assets = await query.OrderBy(a => a.AssetName).ToListAsync();
            return View(assets);
        }

        public async Task<IActionResult> ExportToCsv(string? assetType, string? searchTerm)
        {
            var query = _context.Assets.AsQueryable();

            if (!string.IsNullOrEmpty(assetType))
            {
                query = query.Where(a => a.AssetType == assetType);
            }

            // Filter out null records
            query = query.Where(a => a.AssetTag != null 
                && a.AssetName != null 
                && !string.IsNullOrWhiteSpace(a.AssetTag) 
                && !string.IsNullOrWhiteSpace(a.AssetName));

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(a => 
                    a.AssetTag.Contains(searchTerm) ||
                    a.AssetName.Contains(searchTerm) ||
                    (a.Brand != null && a.Brand.Contains(searchTerm)) ||
                    (a.Location != null && a.Location.Contains(searchTerm)) ||
                    (a.Status != null && a.Status.Contains(searchTerm))
                );
            }

            var assets = await query.OrderBy(a => a.AssetName).ToListAsync();

            // Generate CSV content
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Asset Tag,Asset Name,Type,Brand,Model,Location,Status,Purchase Date,Purchase Price,Vendor");

            foreach (var asset in assets)
            {
                csv.AppendLine($"{EscapeCsvField(asset.AssetTag)},{EscapeCsvField(asset.AssetName)},{EscapeCsvField(asset.AssetType)},{EscapeCsvField(asset.Brand)},{EscapeCsvField(asset.Model)},{EscapeCsvField(asset.Location)},{EscapeCsvField(asset.Status)},{asset.PurchaseDate:yyyy-MM-dd},R {asset.PurchasePrice:N2},{EscapeCsvField(asset.Vendor)}");
            }

            var fileName = string.IsNullOrEmpty(assetType) ? "AllAssets" : $"{assetType}s";
            fileName += $"_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            // If field contains comma, quote, or newline, wrap in quotes and escape quotes
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var asset = await _context.Assets.FirstOrDefaultAsync(m => m.Id == id);
            if (asset == null)
            {
                return NotFound();
            }

            // Load server applications if it's a server using ServerApplications
            if (asset.AssetType == "Server")
            {
                var server = await _context.Servers
                    .Include(s => s.ServerApplications)
                        .ThenInclude(sa => sa.Application)
                    .FirstOrDefaultAsync(s => s.Id == id);
                
                if (server != null)
                {
                    ViewBag.ServerApplications = server.ServerApplications?.ToList() ?? new List<ServerApplication>();
                }
            }

            return View(asset);
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Create(string? assetType)
        {
            // Load users for dropdowns
            var users = await _userManager.Users
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.FullName)
                .ToListAsync();
            ViewBag.Users = users;
            
            // Pass assetType to view for auto-selection
            if (!string.IsNullOrEmpty(assetType))
            {
                ViewBag.PreSelectedAssetType = assetType;
            }
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Create(IFormCollection form)
        {
            // BaseAsset is abstract, so we can't bind to it directly
            // Read AssetType from form
            var assetType = form["AssetType"].ToString();
            
            if (string.IsNullOrWhiteSpace(assetType))
            {
                ModelState.AddModelError("AssetType", "Asset Type is required.");
                return View();
            }

            // Create the appropriate asset type based on AssetType
            BaseAsset? newAsset = null;
            
            try
            {
                switch (assetType)
                {
                    case "Computer":
                        newAsset = new Computer
                        {
                            AssetTag = form["AssetTag"].ToString(),
                            AssetName = form["AssetName"].ToString(),
                            AssetType = "Computer",
                            Brand = form["Brand"].ToString(),
                            Model = form["Model"].ToString(),
                            SerialNumber = form["SerialNumber"].ToString(),
                            PurchaseDate = DateTime.TryParse(form["PurchaseDate"], out var compPurchaseDate) ? compPurchaseDate : DateTime.Now,
                            PurchasePrice = decimal.TryParse(form["PurchasePrice"], out var compPrice) ? compPrice : null,
                            Vendor = form["Vendor"].ToString(),
                            WarrantyExpiryDate = DateTime.TryParse(form["WarrantyExpiryDate"], out var compWarranty) ? compWarranty : null,
                            Location = form["Location"].ToString(),
                            Status = form["Status"].ToString(),
                            Notes = form["Notes"].ToString(),
                            Processor = form["Processor"].ToString(),
                            RAM = int.TryParse(form["RAM"], out var compRAM) ? compRAM : 0,
                            Storage = int.TryParse(form["Storage"], out var compStorage) ? compStorage : 0,
                            OperatingSystem = form["OperatingSystem"].ToString(),
                            AssignedTo = form["AssignedTo"].ToString()
                        };
                        break;
                    
                    case "Server":
                        newAsset = new Server
                        {
                            AssetTag = form["AssetTag"].ToString(),
                            AssetName = form["AssetName"].ToString(),
                            AssetType = "Server",
                            Brand = form["Brand"].ToString(),
                            Model = form["Model"].ToString(),
                            SerialNumber = form["SerialNumber"].ToString(),
                            PurchaseDate = DateTime.TryParse(form["PurchaseDate"], out var servPurchaseDate) ? servPurchaseDate : DateTime.Now,
                            PurchasePrice = string.IsNullOrWhiteSpace(form["PurchasePrice"].ToString()) ? null : (decimal.TryParse(form["PurchasePrice"], out var servPrice) ? servPrice : null),
                            Vendor = form["Vendor"].ToString(),
                            WarrantyExpiryDate = string.IsNullOrWhiteSpace(form["WarrantyExpiryDate"].ToString()) ? null : (DateTime.TryParse(form["WarrantyExpiryDate"], out var servWarranty) ? servWarranty : null),
                            Location = form["Location"].ToString(),
                            Status = form["Status"].ToString(),
                            Notes = form["Notes"].ToString(),
                            IPAddress = form["IPAddress"].ToString(),
                            ServerType = form["ServerType"].ToString(),
                            Processor = form["Processor"].ToString(),
                            RAM = int.TryParse(form["RAM"], out var servRAM) ? servRAM : 0,
                            Storage = int.TryParse(form["Storage"], out var servStorage) ? servStorage : 0,
                            OperatingSystem = form["OperatingSystem"].ToString(),
                            BackupRequired = form["BackupRequired"].ToString() == "true" || form["BackupRequired"].ToString() == "on",
                            BackupComments = form["BackupComments"].ToString(),
                            ProjectManagerName = form["ProjectManagerName"].ToString()
                        };
                        break;
                    
                    case "Application":
                        newAsset = new Application
                        {
                            AssetTag = form["AssetTag"].ToString(),
                            AssetName = form["AssetName"].ToString(),
                            AssetType = "Application",
                            Brand = form["Brand"].ToString(),
                            Model = form["Model"].ToString(),
                            SerialNumber = form["SerialNumber"].ToString(),
                            PurchaseDate = DateTime.TryParse(form["PurchaseDate"], out var appPurchaseDate) ? appPurchaseDate : DateTime.Now,
                            PurchasePrice = decimal.TryParse(form["PurchasePrice"], out var appPrice) ? appPrice : null,
                            Vendor = form["Vendor"].ToString(),
                            WarrantyExpiryDate = DateTime.TryParse(form["WarrantyExpiryDate"], out var appWarranty) ? appWarranty : null,
                            Location = form["Location"].ToString(),
                            Status = form["Status"].ToString(),
                            Notes = form["Notes"].ToString(),
                            Version = form["Version"].ToString(),
                            Category = form["Category"].ToString(),
                            Description = form["Description"].ToString(),
                            RequiresLicense = form["RequiresLicense"].ToString() == "on" || form["RequiresLicense"].ToString() == "true",
                            LicenseKey = form["LicenseKey"].ToString(),
                            LicenseType = form["LicenseType"].ToString(),
                            TotalLicenses = int.TryParse(form["TotalLicenses"], out var totalLicenses) ? totalLicenses : null,
                            UsedLicenses = int.TryParse(form["UsedLicenses"], out var usedLicenses) ? usedLicenses : null,
                            LicenseExpiryDate = DateTime.TryParse(form["LicenseExpiryDate"], out var licenseExpiry) ? licenseExpiry : null,
                            BusinessUnit = form["BusinessUnit"].ToString(),
                            ApplicationOwner = form["ApplicationOwner"].ToString(),
                            LicenseHolder = form["LicenseHolder"].ToString()
                        };
                        break;
                }

                if (newAsset == null)
                {
                    ModelState.AddModelError("AssetType", "Invalid Asset Type.");
                    return View();
                }

                // Validate model
                newAsset.CreatedBy = User.Identity?.Name;
                newAsset.CreatedDate = DateTime.Now;

                if (TryValidateModel(newAsset))
                {
                    _context.Add(newAsset);
                    await _context.SaveChangesAsync();

                    _context.AuditLogs.Add(new AuditLog
                    {
                        UserName = User.Identity?.Name ?? "",
                        Action = "Create",
                        EntityType = $"Asset ({newAsset.AssetType})",
                        EntityId = newAsset.Id,
                        Details = $"Created {newAsset.AssetType}: {newAsset.AssetName}",
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                        Timestamp = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index), new { assetType = newAsset.AssetType });
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            }

            return View();
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            // Load users for dropdowns
            var users = await _userManager.Users
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.FullName)
                .ToListAsync();
            ViewBag.Users = users;

            return View(asset);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Edit(int id, IFormCollection form)
        {
            // BaseAsset is abstract, so we can't bind to it directly
            // Get the existing asset to determine its type
            var existingAsset = await _context.Assets.FindAsync(id);
            if (existingAsset == null)
            {
                return NotFound();
            }

            var assetType = existingAsset.AssetType;
            if (string.IsNullOrWhiteSpace(assetType))
            {
                assetType = form["AssetType"].ToString();
            }

            try
            {
                // Update based on asset type
                switch (assetType)
                {
                    case "Computer":
                        var existingComputer = await _context.Computers.FindAsync(id);
                        if (existingComputer != null)
                        {
                            existingComputer.AssetTag = form["AssetTag"].ToString();
                            existingComputer.AssetName = form["AssetName"].ToString();
                            existingComputer.Brand = form["Brand"].ToString();
                            existingComputer.Model = form["Model"].ToString();
                            existingComputer.SerialNumber = form["SerialNumber"].ToString();
                            existingComputer.PurchaseDate = DateTime.TryParse(form["PurchaseDate"], out var compPurchaseDate) ? compPurchaseDate : existingComputer.PurchaseDate;
                            existingComputer.PurchasePrice = string.IsNullOrWhiteSpace(form["PurchasePrice"].ToString()) ? null : (decimal.TryParse(form["PurchasePrice"], out var compPrice) ? compPrice : existingComputer.PurchasePrice);
                            existingComputer.Vendor = form["Vendor"].ToString();
                            existingComputer.WarrantyExpiryDate = DateTime.TryParse(form["WarrantyExpiryDate"], out var compWarranty) ? compWarranty : null;
                            existingComputer.Location = form["Location"].ToString();
                            existingComputer.Status = form["Status"].ToString();
                            existingComputer.Notes = form["Notes"].ToString();
                            existingComputer.Processor = form["Processor"].ToString();
                            existingComputer.RAM = int.TryParse(form["RAM"], out var compRAM) ? compRAM : existingComputer.RAM;
                            existingComputer.Storage = int.TryParse(form["Storage"], out var compStorage) ? compStorage : existingComputer.Storage;
                            existingComputer.OperatingSystem = form["OperatingSystem"].ToString();
                            existingComputer.AssignedTo = form["AssignedTo"].ToString();
                            existingComputer.ModifiedBy = User.Identity?.Name;
                            existingComputer.ModifiedDate = DateTime.Now;

                            await _context.SaveChangesAsync();

                            _context.AuditLogs.Add(new AuditLog
                            {
                                UserName = User.Identity?.Name ?? "",
                                Action = "Update",
                                EntityType = "Computer",
                                EntityId = id,
                                Details = $"Updated computer: {existingComputer.AssetName}",
                                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                                Timestamp = DateTime.Now
                            });
                            await _context.SaveChangesAsync();
                        }
                        break;

                    case "Server":
                        var existingServer = await _context.Servers.FindAsync(id);
                        if (existingServer != null)
                        {
                            existingServer.AssetTag = form["AssetTag"].ToString();
                            existingServer.AssetName = form["AssetName"].ToString();
                            existingServer.Brand = form["Brand"].ToString();
                            existingServer.Model = form["Model"].ToString();
                            existingServer.SerialNumber = form["SerialNumber"].ToString();
                            existingServer.PurchaseDate = DateTime.TryParse(form["PurchaseDate"], out var servPurchaseDate) ? servPurchaseDate : existingServer.PurchaseDate;
                            existingServer.PurchasePrice = string.IsNullOrWhiteSpace(form["PurchasePrice"].ToString()) ? null : (decimal.TryParse(form["PurchasePrice"], out var servPrice) ? servPrice : existingServer.PurchasePrice);
                            existingServer.Vendor = form["Vendor"].ToString();
                            existingServer.WarrantyExpiryDate = string.IsNullOrWhiteSpace(form["WarrantyExpiryDate"].ToString()) ? null : (DateTime.TryParse(form["WarrantyExpiryDate"], out var servWarranty) ? servWarranty : null);
                            existingServer.Location = form["Location"].ToString();
                            existingServer.Status = form["Status"].ToString();
                            existingServer.Notes = form["Notes"].ToString();
                            existingServer.IPAddress = form["IPAddress"].ToString();
                            existingServer.ServerType = form["ServerType"].ToString();
                            existingServer.Processor = form["Processor"].ToString();
                            existingServer.RAM = int.TryParse(form["RAM"], out var servRAM) ? servRAM : existingServer.RAM;
                            existingServer.Storage = int.TryParse(form["Storage"], out var servStorage) ? servStorage : existingServer.Storage;
                            existingServer.OperatingSystem = form["OperatingSystem"].ToString();
                            existingServer.BackupRequired = form["BackupRequired"].ToString() == "true" || form["BackupRequired"].ToString() == "on";
                            existingServer.BackupComments = form["BackupComments"].ToString();
                            existingServer.ProjectManagerName = form["ProjectManagerName"].ToString();
                            existingServer.ModifiedBy = User.Identity?.Name;
                            existingServer.ModifiedDate = DateTime.Now;

                            await _context.SaveChangesAsync();

                            _context.AuditLogs.Add(new AuditLog
                            {
                                UserName = User.Identity?.Name ?? "",
                                Action = "Update",
                                EntityType = "Server",
                                EntityId = id,
                                Details = $"Updated server: {existingServer.AssetName}",
                                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                                Timestamp = DateTime.Now
                            });
                            await _context.SaveChangesAsync();
                        }
                        break;

                    case "Application":
                        var existingApplication = await _context.Applications.FindAsync(id);
                        if (existingApplication != null)
                        {
                            existingApplication.AssetTag = form["AssetTag"].ToString();
                            existingApplication.AssetName = form["AssetName"].ToString();
                            existingApplication.Brand = form["Brand"].ToString();
                            existingApplication.Model = form["Model"].ToString();
                            existingApplication.SerialNumber = form["SerialNumber"].ToString();
                            existingApplication.PurchaseDate = DateTime.TryParse(form["PurchaseDate"], out var appPurchaseDate) ? appPurchaseDate : existingApplication.PurchaseDate;
                            existingApplication.PurchasePrice = string.IsNullOrWhiteSpace(form["PurchasePrice"].ToString()) ? null : (decimal.TryParse(form["PurchasePrice"], out var appPrice) ? appPrice : existingApplication.PurchasePrice);
                            existingApplication.Vendor = form["Vendor"].ToString();
                            existingApplication.WarrantyExpiryDate = DateTime.TryParse(form["WarrantyExpiryDate"], out var appWarranty) ? appWarranty : null;
                            existingApplication.Location = form["Location"].ToString();
                            existingApplication.Status = form["Status"].ToString();
                            existingApplication.Notes = form["Notes"].ToString();
                            existingApplication.Version = form["Version"].ToString();
                            existingApplication.Category = form["Category"].ToString();
                            existingApplication.Description = form["Description"].ToString();
                            existingApplication.RequiresLicense = form["RequiresLicense"].ToString() == "on" || form["RequiresLicense"].ToString() == "true";
                            existingApplication.LicenseKey = form["LicenseKey"].ToString();
                            existingApplication.LicenseType = form["LicenseType"].ToString();
                            existingApplication.TotalLicenses = int.TryParse(form["TotalLicenses"], out var totalLicenses) ? totalLicenses : null;
                            existingApplication.UsedLicenses = int.TryParse(form["UsedLicenses"], out var usedLicenses) ? usedLicenses : null;
                            existingApplication.LicenseExpiryDate = DateTime.TryParse(form["LicenseExpiryDate"], out var licenseExpiry) ? licenseExpiry : null;
                            existingApplication.BusinessUnit = form["BusinessUnit"].ToString();
                            existingApplication.ApplicationOwner = form["ApplicationOwner"].ToString();
                            existingApplication.LicenseHolder = form["LicenseHolder"].ToString();
                            existingApplication.ModifiedBy = User.Identity?.Name;
                            existingApplication.ModifiedDate = DateTime.Now;

                            await _context.SaveChangesAsync();

                            _context.AuditLogs.Add(new AuditLog
                            {
                                UserName = User.Identity?.Name ?? "",
                                Action = "Update",
                                EntityType = "Application",
                                EntityId = id,
                                Details = $"Updated application: {existingApplication.AssetName}",
                                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                                Timestamp = DateTime.Now
                            });
                            await _context.SaveChangesAsync();
                        }
                        break;
                }

                return RedirectToAction(nameof(Index), new { assetType });
            }
            catch (DbUpdateConcurrencyException)
            {
                var assetExists = await _context.Assets.AnyAsync(e => e.Id == id);
                if (!assetExists)
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                var assetForView = await _context.Assets.FindAsync(id);
                return View(assetForView);
            }
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var asset = await _context.Assets.FirstOrDefaultAsync(m => m.Id == id);
            if (asset == null)
            {
                return NotFound();
            }

            // Check dependencies based on asset type
            if (asset.AssetType == "Server")
            {
                var server = await _context.Servers
                    .Include(s => s.ServerApplications)
                    .FirstOrDefaultAsync(s => s.Id == id);
                if (server != null)
                {
                    var installedAppCount = server.ServerApplications?.Count ?? 0;
                    ViewBag.HasInstalledApplications = installedAppCount > 0;
                    ViewBag.InstalledApplicationCount = installedAppCount;
                }
            }
            else if (asset.AssetType == "Application")
            {
                var application = await _context.Applications
                    .Include(a => a.ServerApplications)
                    .FirstOrDefaultAsync(a => a.Id == id);
                if (application != null)
                {
                    var serverCount = application.ServerApplications?.Count ?? 0;
                    ViewBag.IsInstalledOnServers = serverCount > 0;
                    ViewBag.ServerCount = serverCount;
                }
            }

            return View(asset);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            var assetType = asset.AssetType;

            // Check dependencies before deletion
            if (assetType == "Server")
            {
                var server = await _context.Servers
                    .Include(s => s.ServerApplications)
                    .FirstOrDefaultAsync(s => s.Id == id);
                if (server != null)
                {
                    var installedAppCount = server.ServerApplications?.Count ?? 0;
                    if (installedAppCount > 0)
                    {
                        TempData["ErrorMessage"] = $"Cannot delete server '{server.AssetName}'. It has {installedAppCount} installed application(s). Please remove all applications before deleting the server.";
                        return RedirectToAction(nameof(Delete), new { id });
                    }
                }
            }
            else if (assetType == "Application")
            {
                var application = await _context.Applications
                    .Include(a => a.ServerApplications)
                    .FirstOrDefaultAsync(a => a.Id == id);
                if (application != null)
                {
                    var serverCount = application.ServerApplications?.Count ?? 0;
                    if (serverCount > 0)
                    {
                        TempData["ErrorMessage"] = $"Cannot delete application '{application.AssetName}'. It is installed on {serverCount} server(s). Please remove it from all servers before deleting the application.";
                        return RedirectToAction(nameof(Delete), new { id });
                    }
                }
            }

            _context.Assets.Remove(asset);
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserName = User.Identity?.Name ?? "",
                Action = "Delete",
                EntityType = $"Asset ({assetType})",
                EntityId = id,
                Details = $"Deleted {assetType}: {asset.AssetName}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
            });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { assetType });
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

            var installedAppIds = server.ServerApplications?.Select(sa => sa.ApplicationId).ToList() ?? new List<int>();
            var availableApplications = await _context.Applications
                .Where(a => !installedAppIds.Contains(a.Id))
                .ToListAsync();

            ViewBag.Server = server;
            ViewBag.InstalledApplications = server.ServerApplications?.ToList() ?? new List<ServerApplication>();
            ViewBag.AvailableApplications = availableApplications;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> InstallApplication(int serverId, int applicationId, string status, string? notes)
        {
            var server = await _context.Servers.FindAsync(serverId);
            var application = await _context.Applications.FindAsync(applicationId);

            if (server == null || application == null)
            {
                return NotFound();
            }

            // Check if already installed
            var existing = await _context.ServerApplications
                .FirstOrDefaultAsync(sa => sa.ServerId == serverId && sa.ApplicationId == applicationId);
            
            if (existing != null)
            {
                TempData["ErrorMessage"] = "Application is already installed on this server.";
                return RedirectToAction(nameof(ManageApplications), new { id = serverId });
            }

            // Use the application's base version
            var serverApp = new ServerApplication
            {
                ServerId = serverId,
                ApplicationId = applicationId,
                InstallationDate = DateTime.Now,
                InstalledVersion = application.Version ?? "1.0.0",
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
                Details = $"Installed {application.AssetName} (Version: {serverApp.InstalledVersion}) on {server.AssetName}",
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

        private bool AssetExists(int id)
        {
            return _context.Assets.Any(e => e.Id == id);
        }
    }
}