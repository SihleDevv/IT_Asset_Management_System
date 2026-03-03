using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT_Asset_Management_System.Data;
using IT_Asset_Management_System.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;

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
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");
            
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

            // Filter by user assignment or department based on role
            if (isAdminOrITManager)
            {
                // Admin and IT Manager see all assets in their department
                if (!string.IsNullOrWhiteSpace(currentUser.Department))
                {
                    // For now, show all assets. Can add department filtering later if needed
                    // query = query.Where(a => a.Department == currentUser.Department);
                }
            }
            else
            {
                // Regular users (Read Only, Employee) only see assets assigned to them
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email ?? "";
                var userEmail = currentUser.Email ?? "";
                
                if (!string.IsNullOrWhiteSpace(userFullName) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    // Filter computers assigned to user (case-insensitive)
                    var assignedComputerIds = await _context.Computers
                        .Where(c => c.AssignedTo != null &&
                            c.AssignedTo.ToLower() != "unassigned" &&
                            (c.AssignedTo.ToLower() == userFullName.ToLower() || 
                             c.AssignedTo.ToLower() == userEmail.ToLower()))
                        .Select(c => c.Id)
                        .ToListAsync();
                    
                    // Filter servers where user is project manager (case-insensitive)
                    var managedServerIds = await _context.Servers
                        .Where(s => s.ProjectManagerName != null &&
                            (s.ProjectManagerName.ToLower() == userFullName.ToLower() || 
                             s.ProjectManagerName.ToLower() == userEmail.ToLower()))
                        .Select(s => s.Id)
                        .ToListAsync();
                    
                    // Filter applications where user is owner (case-insensitive)
                    var ownedApplicationIds = await _context.Applications
                        .Where(a => a.ApplicationOwner != null &&
                            (a.ApplicationOwner.ToLower() == userFullName.ToLower() || 
                             a.ApplicationOwner.ToLower() == userEmail.ToLower()))
                        .Select(a => a.Id)
                        .ToListAsync();
                    
                    // Combine all assigned asset IDs
                    var assignedAssetIds = assignedComputerIds
                        .Concat(managedServerIds)
                        .Concat(ownedApplicationIds)
                        .ToList();
                    
                    if (assignedAssetIds.Any())
                    {
                        query = query.Where(a => assignedAssetIds.Contains(a.Id));
                    }
                    else
                    {
                        // If user has no assigned assets, return empty list
                        query = query.Where(a => false);
                    }
                }
                else
                {
                    // If user has no name/email, return empty list
                    query = query.Where(a => false);
                }
            }

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
            var assets = await query.OrderBy(a => a.AssetTag).ToListAsync();
            return View(assets);
        }

        public async Task<IActionResult> ExportToCsv(string? assetType, string? searchTerm)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");
            
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

            // Apply same permission filtering as Index
            if (!isAdminOrITManager)
            {
                // Regular users (Read Only, Employee) only see assets assigned to them
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email ?? "";
                var userEmail = currentUser.Email ?? "";
                
                if (!string.IsNullOrWhiteSpace(userFullName) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    // Filter computers assigned to user (case-insensitive)
                    var assignedComputerIds = await _context.Computers
                        .Where(c => c.AssignedTo != null &&
                            c.AssignedTo.ToLower() != "unassigned" &&
                            (c.AssignedTo.ToLower() == userFullName.ToLower() || 
                             c.AssignedTo.ToLower() == userEmail.ToLower()))
                        .Select(c => c.Id)
                        .ToListAsync();
                    
                    // Filter servers where user is project manager (case-insensitive)
                    var managedServerIds = await _context.Servers
                        .Where(s => s.ProjectManagerName != null &&
                            (s.ProjectManagerName.ToLower() == userFullName.ToLower() || 
                             s.ProjectManagerName.ToLower() == userEmail.ToLower()))
                        .Select(s => s.Id)
                        .ToListAsync();
                    
                    // Filter applications where user is owner (case-insensitive)
                    var ownedApplicationIds = await _context.Applications
                        .Where(a => a.ApplicationOwner != null &&
                            (a.ApplicationOwner.ToLower() == userFullName.ToLower() || 
                             a.ApplicationOwner.ToLower() == userEmail.ToLower()))
                        .Select(a => a.Id)
                        .ToListAsync();
                    
                    // Combine all assigned asset IDs
                    var assignedAssetIds = assignedComputerIds
                        .Concat(managedServerIds)
                        .Concat(ownedApplicationIds)
                        .ToList();
                    
                    if (assignedAssetIds.Any())
                    {
                        query = query.Where(a => assignedAssetIds.Contains(a.Id));
                    }
                    else
                    {
                        // If user has no assigned assets, return empty list
                        query = query.Where(a => false);
                    }
                }
                else
                {
                    // If user has no name/email, return empty list
                    query = query.Where(a => false);
                }
            }

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

            var assets = await query.OrderBy(a => a.AssetTag).ToListAsync();

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

        public async Task<IActionResult> ExportToPdf(string? assetType, string? searchTerm)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");
            
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

            // Apply same permission filtering as Index
            if (!isAdminOrITManager)
            {
                // Regular users (Read Only, Employee) only see assets assigned to them
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email ?? "";
                var userEmail = currentUser.Email ?? "";
                
                if (!string.IsNullOrWhiteSpace(userFullName) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    // Filter computers assigned to user (case-insensitive)
                    var assignedComputerIds = await _context.Computers
                        .Where(c => c.AssignedTo != null &&
                            c.AssignedTo.ToLower() != "unassigned" &&
                            (c.AssignedTo.ToLower() == userFullName.ToLower() || 
                             c.AssignedTo.ToLower() == userEmail.ToLower()))
                        .Select(c => c.Id)
                        .ToListAsync();
                    
                    // Filter servers where user is project manager (case-insensitive)
                    var managedServerIds = await _context.Servers
                        .Where(s => s.ProjectManagerName != null &&
                            (s.ProjectManagerName.ToLower() == userFullName.ToLower() || 
                             s.ProjectManagerName.ToLower() == userEmail.ToLower()))
                        .Select(s => s.Id)
                        .ToListAsync();
                    
                    // Filter applications where user is owner (case-insensitive)
                    var ownedApplicationIds = await _context.Applications
                        .Where(a => a.ApplicationOwner != null &&
                            (a.ApplicationOwner.ToLower() == userFullName.ToLower() || 
                             a.ApplicationOwner.ToLower() == userEmail.ToLower()))
                        .Select(a => a.Id)
                        .ToListAsync();
                    
                    // Combine all assigned asset IDs
                    var assignedAssetIds = assignedComputerIds
                        .Concat(managedServerIds)
                        .Concat(ownedApplicationIds)
                        .ToList();
                    
                    if (assignedAssetIds.Any())
                    {
                        query = query.Where(a => assignedAssetIds.Contains(a.Id));
                    }
                    else
                    {
                        // If user has no assigned assets, return empty list
                        query = query.Where(a => false);
                    }
                }
                else
                {
                    // If user has no name/email, return empty list
                    query = query.Where(a => false);
                }
            }

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

            var assets = await query.OrderBy(a => a.AssetTag).ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    var reportTitle = string.IsNullOrEmpty(assetType) ? "All Assets Report" : $"{assetType}s Report";
                    page.Header()
                        .Text(reportTitle)
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);
                            column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(8).FontColor(Colors.Grey.Medium);
                            column.Item().Text($"Total Records: {assets.Count}").FontSize(8).FontColor(Colors.Grey.Medium);
                            
                            if (!string.IsNullOrWhiteSpace(searchTerm))
                                column.Item().Text($"Search Term: {searchTerm}").FontSize(8).FontColor(Colors.Grey.Medium);
                            if (!string.IsNullOrWhiteSpace(assetType))
                                column.Item().Text($"Asset Type: {assetType}").FontSize(8).FontColor(Colors.Grey.Medium);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(2.5f);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Asset Tag");
                                    header.Cell().Element(CellStyle).Text("Asset Name");
                                    header.Cell().Element(CellStyle).Text("Type");
                                    header.Cell().Element(CellStyle).Text("Brand");
                                    header.Cell().Element(CellStyle).Text("Location");
                                    header.Cell().Element(CellStyle).Text("Status");
                                });

                                foreach (var asset in assets)
                                {
                                    table.Cell().Element(CellStyle).Text(asset.AssetTag ?? "—");
                                    table.Cell().Element(CellStyle).Text(asset.AssetName ?? "—");
                                    table.Cell().Element(CellStyle).Text(asset.AssetType ?? "—");
                                    table.Cell().Element(CellStyle).Text(asset.Brand ?? "—");
                                    table.Cell().Element(CellStyle).Text(asset.Location ?? "—");
                                    table.Cell().Element(CellStyle).Text(asset.Status ?? "—");
                                }
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            var pdfBytes = document.GeneratePdf();
            var fileName = string.IsNullOrEmpty(assetType) ? "AllAssets" : $"{assetType}s";
            fileName += $"_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(5)
                .PaddingHorizontal(5);
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

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkEdit(string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
            {
                TempData["ErrorMessage"] = "No assets selected for bulk edit.";
                return RedirectToAction(nameof(Index));
            }

            var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(idStr => int.TryParse(idStr, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();

            if (idList.Count == 0)
            {
                TempData["ErrorMessage"] = "Invalid asset IDs provided.";
                return RedirectToAction(nameof(Index));
            }

            var assets = await _context.Assets
                .Where(a => idList.Contains(a.Id))
                .OrderBy(a => a.AssetName)
                .ToListAsync();

            if (assets.Count == 0)
            {
                TempData["ErrorMessage"] = "No assets found with the provided IDs.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AssetIds = string.Join(",", idList);
            ViewBag.AssetCount = assets.Count;
            ViewBag.AssetTypes = assets.Select(a => a.AssetType).Distinct().ToList();

            // Create a view model with common fields only
            var model = new
            {
                AssetIds = string.Join(",", idList),
                Assets = assets,
                // Common fields - leave empty for bulk edit (user fills them)
                Brand = "",
                Model = "",
                SerialNumber = "",
                Location = "",
                Status = "",
                PurchaseDate = (DateTime?)null,
                PurchasePrice = (decimal?)null,
                Vendor = "",
                WarrantyExpiryDate = (DateTime?)null,
                Notes = ""
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkEdit(string assetIds, IFormCollection form)
        {
            if (string.IsNullOrWhiteSpace(assetIds))
            {
                TempData["ErrorMessage"] = "No assets selected for bulk edit.";
                return RedirectToAction(nameof(Index));
            }

            var idList = assetIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(idStr => int.TryParse(idStr, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();

            if (idList.Count == 0)
            {
                TempData["ErrorMessage"] = "Invalid asset IDs provided.";
                return RedirectToAction(nameof(Index));
            }

            var assets = await _context.Assets
                .Where(a => idList.Contains(a.Id))
                .ToListAsync();

            if (assets.Count == 0)
            {
                TempData["ErrorMessage"] = "No assets found with the provided IDs.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserName = User.Identity?.Name ?? "System";
            var updatedCount = 0;
            var assetType = "";

            // Get form values - only update fields that are provided (not empty)
            var brand = form["Brand"].ToString().Trim();
            var model = form["Model"].ToString().Trim();
            var serialNumber = form["SerialNumber"].ToString().Trim();
            var location = form["Location"].ToString().Trim();
            var status = form["Status"].ToString().Trim();
            var purchaseDateStr = form["PurchaseDate"].ToString().Trim();
            var purchasePriceStr = form["PurchasePrice"].ToString().Trim();
            var vendor = form["Vendor"].ToString().Trim();
            var warrantyExpiryDateStr = form["WarrantyExpiryDate"].ToString().Trim();
            var notes = form["Notes"].ToString().Trim();

            DateTime? purchaseDate = null;
            if (!string.IsNullOrWhiteSpace(purchaseDateStr) && DateTime.TryParse(purchaseDateStr, out var pd))
            {
                purchaseDate = pd;
            }

            decimal? purchasePrice = null;
            if (!string.IsNullOrWhiteSpace(purchasePriceStr) && decimal.TryParse(purchasePriceStr, out var pp))
            {
                purchasePrice = pp;
            }

            DateTime? warrantyExpiryDate = null;
            if (!string.IsNullOrWhiteSpace(warrantyExpiryDateStr) && DateTime.TryParse(warrantyExpiryDateStr, out var wed))
            {
                warrantyExpiryDate = wed;
            }

            foreach (var asset in assets)
            {
                var hasChanges = false;

                // Update only if value is provided
                if (!string.IsNullOrWhiteSpace(brand))
                {
                    asset.Brand = brand;
                    hasChanges = true;
                }
                if (!string.IsNullOrWhiteSpace(model))
                {
                    asset.Model = model;
                    hasChanges = true;
                }
                if (!string.IsNullOrWhiteSpace(serialNumber))
                {
                    asset.SerialNumber = serialNumber;
                    hasChanges = true;
                }
                if (!string.IsNullOrWhiteSpace(location))
                {
                    asset.Location = location;
                    hasChanges = true;
                }
                if (!string.IsNullOrWhiteSpace(status))
                {
                    asset.Status = status;
                    hasChanges = true;
                }
                if (purchaseDate.HasValue)
                {
                    asset.PurchaseDate = purchaseDate.Value;
                    hasChanges = true;
                }
                if (purchasePrice.HasValue)
                {
                    asset.PurchasePrice = purchasePrice;
                    hasChanges = true;
                }
                if (!string.IsNullOrWhiteSpace(vendor))
                {
                    asset.Vendor = vendor;
                    hasChanges = true;
                }
                if (warrantyExpiryDate.HasValue)
                {
                    asset.WarrantyExpiryDate = warrantyExpiryDate;
                    hasChanges = true;
                }
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    asset.Notes = notes;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    asset.ModifiedBy = currentUserName;
                    asset.ModifiedDate = DateTime.Now;
                    updatedCount++;

                    // Log the update
                    _context.AuditLogs.Add(new AuditLog
                    {
                        UserName = currentUserName,
                        Action = "Bulk Update",
                        EntityType = asset.AssetType ?? "Asset",
                        EntityId = asset.Id,
                        Details = $"Bulk updated asset: {asset.AssetName} (Tag: {asset.AssetTag})",
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                        Timestamp = DateTime.Now
                    });

                    if (string.IsNullOrEmpty(assetType))
                    {
                        assetType = asset.AssetType ?? "";
                    }
                }
            }

            await _context.SaveChangesAsync();

            if (updatedCount > 0)
            {
                TempData["SuccessMessage"] = $"Successfully updated {updatedCount} asset(s).";
            }
            else
            {
                TempData["InfoMessage"] = "No changes were made. Please fill in at least one field to update.";
            }

            return RedirectToAction(nameof(Index), new { assetType });
        }

        [Authorize(Roles = "Admin")]
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
            if (asset.AssetType == "Computer")
            {
                var computer = await _context.Computers.FindAsync(id);
                if (computer != null && !string.IsNullOrWhiteSpace(computer.AssignedTo) && 
                    computer.AssignedTo.ToLower() != "unassigned")
                {
                    ViewBag.IsAssignedToUser = true;
                    ViewBag.AssignedToUser = computer.AssignedTo;
                }
            }
            else if (asset.AssetType == "Server")
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            var assetType = asset.AssetType;

            // Check dependencies before deletion
            if (assetType == "Computer")
            {
                var computer = await _context.Computers.FindAsync(id);
                if (computer != null && !string.IsNullOrWhiteSpace(computer.AssignedTo) && 
                    computer.AssignedTo.ToLower() != "unassigned")
                {
                    TempData["ErrorMessage"] = $"Cannot delete computer '{computer.AssetName}'. It is assigned to user '{computer.AssignedTo}'. Please unassign the computer from the user before deleting it.";
                    return RedirectToAction(nameof(Delete), new { id });
                }
            }
            else if (assetType == "Server")
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkDelete(List<int> ids, string? assetType)
        {
            if (ids == null || ids.Count == 0)
            {
                TempData["ErrorMessage"] = "No records selected for deletion.";
                return RedirectToAction(nameof(Index), new { assetType });
            }

            var currentUserName = User.Identity?.Name ?? "System";
            var deletedCount = 0;
            var failedCount = 0;
            var errors = new List<string>();

            foreach (var id in ids)
            {
                var asset = await _context.Assets.FindAsync(id);
                if (asset == null)
                {
                    failedCount++;
                    errors.Add($"Asset with ID {id} not found.");
                    continue;
                }

                var assetTypeName = asset.AssetType;
                bool canDelete = true;
                string? errorMessage = null;

                // Check dependencies before deletion
                if (assetTypeName == "Computer")
                {
                    var computer = await _context.Computers.FindAsync(id);
                    if (computer != null && !string.IsNullOrWhiteSpace(computer.AssignedTo) && 
                        computer.AssignedTo.ToLower() != "unassigned")
                    {
                        canDelete = false;
                        errorMessage = $"Cannot delete computer '{computer.AssetName}'. It is assigned to user '{computer.AssignedTo}'.";
                    }
                }
                else if (assetTypeName == "Server")
                {
                    var server = await _context.Servers
                        .Include(s => s.ServerApplications)
                        .FirstOrDefaultAsync(s => s.Id == id);
                    if (server != null)
                    {
                        var installedAppCount = server.ServerApplications?.Count ?? 0;
                        if (installedAppCount > 0)
                        {
                            canDelete = false;
                            errorMessage = $"Cannot delete server '{server.AssetName}'. It has {installedAppCount} installed application(s).";
                        }
                    }
                }
                else if (assetTypeName == "Application")
                {
                    var application = await _context.Applications
                        .Include(a => a.ServerApplications)
                        .FirstOrDefaultAsync(a => a.Id == id);
                    if (application != null)
                    {
                        var serverCount = application.ServerApplications?.Count ?? 0;
                        if (serverCount > 0)
                        {
                            canDelete = false;
                            errorMessage = $"Cannot delete application '{application.AssetName}'. It is installed on {serverCount} server(s).";
                        }
                    }
                }

                if (canDelete)
                {
                    // Log the deletion
                    _context.AuditLogs.Add(new AuditLog
                    {
                        UserName = currentUserName,
                        Action = "Bulk Delete",
                        EntityType = assetTypeName ?? "Asset",
                        EntityId = id,
                        Details = $"Bulk deleted asset: {asset.AssetName} (Tag: {asset.AssetTag})",
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                        Timestamp = DateTime.Now
                    });

                    _context.Assets.Remove(asset);
                    deletedCount++;
                }
                else
                {
                    failedCount++;
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        errors.Add(errorMessage);
                    }
                }
            }

            await _context.SaveChangesAsync();

            if (deletedCount > 0)
            {
                TempData["SuccessMessage"] = $"Successfully deleted {deletedCount} asset(s).";
            }
            if (failedCount > 0)
            {
                TempData["WarningMessage"] = $"{failedCount} asset(s) could not be deleted. " + string.Join(" ", errors.Take(5));
            }

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
        public async Task<IActionResult> UpdateServerApplication(int id, string status, string? notes)
        {
            var serverApp = await _context.ServerApplications
                .Include(sa => sa.Server)
                .Include(sa => sa.Application)
                .FirstOrDefaultAsync(sa => sa.Id == id);

            if (serverApp == null)
            {
                return NotFound();
            }

            var oldStatus = serverApp.Status;
            serverApp.Status = status ?? "Running";
            serverApp.Notes = notes ?? string.Empty;

            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserName = User.Identity?.Name ?? "",
                Action = "Update Server Application",
                EntityType = "ServerApplication",
                EntityId = id,
                Details = $"Updated {serverApp.Application?.AssetName} status on {serverApp.Server?.AssetName} from '{oldStatus}' to '{serverApp.Status}'",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Application '{serverApp.Application?.AssetName}' status has been updated to '{serverApp.Status}'.";
            return RedirectToAction(nameof(ManageApplications), new { id = serverApp.ServerId });
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

        [HttpGet]
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult DownloadTemplate(string? assetType)
        {
            var csv = new System.Text.StringBuilder();
            
            if (assetType == "Computer")
            {
                csv.AppendLine("Asset Type,Asset Tag,Asset Name,Brand,Model,Serial Number,Location,Status,Assigned To,Operating System,Processor,RAM (GB),Storage (GB),Purchase Date,Purchase Price,Vendor,Warranty Expiry Date,Notes");
                // Use 'Unassigned' and generic sample data (no real person names)
                csv.AppendLine("Computer,COMP-001,Desktop PC,Dell,OptiPlex 7090,SN123456,Office A,In Use,Unassigned,Windows 11,i7-11700,16,512,2024-01-15,1500.00,Dell,2027-01-15,Primary workstation");
            }
            else if (assetType == "Server")
            {
                csv.AppendLine("Asset Type,Asset Tag,Asset Name,Brand,Model,Serial Number,Location,Status,IP Address,Server Type,Purpose,Operating System,Processor,RAM (GB),Storage (GB),Project Manager Name,Backup Required,Backup Comments,Purchase Date,Purchase Price,Vendor,Warranty Expiry Date,Notes");
                // Use 'Unassigned' for Project Manager Name (no real person names)
                csv.AppendLine("Server,SRV-001,Web Server,HP,ProLiant DL380,SN789012,Data Center,Active,192.168.1.10,Physical,Web Hosting,Windows Server 2022,Xeon E5-2680,64,2000,Unassigned,true,Weekly backup,2023-06-01,5000.00,HP,2026-06-01,Main web server");
            }
            else if (assetType == "Application")
            {
                csv.AppendLine("Asset Type,Asset Tag,Asset Name,Brand,Model,Serial Number,Version,Category,Description,Vendor,Location,Status,Business Unit,Application Owner,Requires License,License Key,License Type,Total Licenses,Used Licenses,License Expiry Date,License Holder,Purchase Date,Purchase Price,Warranty Expiry Date,Notes");
                // Use 'Unassigned' for Application Owner (no real person names)
                csv.AppendLine("Application,APP-001,Microsoft Office,Microsoft,Office 2021,SN-APP-001,2021,Productivity,Standard office suite,Microsoft,Company-wide,Active,IT,Unassigned,true,XXXXX-XXXXX-XXXXX,Volume,100,75,2025-12-31,Company,2024-01-01,15000.00,2029-01-01,Standard office suite");
            }
            else
            {
                // Generic template
                csv.AppendLine("Asset Tag,Asset Name,Asset Type,Brand,Model,Serial Number,Location,Status,Purchase Date,Purchase Price,Vendor,Notes");
                csv.AppendLine("ASSET-001,Sample Asset,Computer,Dell,Model X,SN123,Office,Active,2024-01-01,1000.00,Dell,Sample");
            }

            var fileName = $"AssetImportTemplate_{assetType ?? "Generic"}_{DateTime.Now:yyyyMMdd}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        [HttpGet]
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult Import(string? assetType)
        {
            ViewBag.AssetType = assetType ?? "";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Import(IFormFile csvFile, string? assetType)
        {
            var results = new List<string>();
            var successCount = 0;
            var errorCount = 0;

            // Get assetType from form data if not in route parameter
            if (string.IsNullOrWhiteSpace(assetType))
            {
                assetType = Request.Form["assetType"].ToString();
            }

            ViewBag.AssetType = assetType ?? "";

            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a CSV file to import.";
                return View();
            }

            if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Please upload a valid CSV file.";
                return View();
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var currentUserName = currentUser?.FullName ?? currentUser?.UserName ?? User.Identity?.Name ?? "System";

                using (var reader = new System.IO.StreamReader(csvFile.OpenReadStream()))
                {
                    var lineNumber = 0;
                    string? line;

                    // Read header line
                    line = await reader.ReadLineAsync();
                    lineNumber++;
                    if (line == null)
                    {
                        TempData["ErrorMessage"] = "CSV file is empty.";
                        return View();
                    }

                    var headers = ParseCsvLine(line);
                    var assetTypeColumnIndex = headers.FindIndex(h => h.Equals("Asset Type", StringComparison.OrdinalIgnoreCase));
                    
                    // Detect asset type from CSV headers if route parameter is not provided
                    var detectedAssetTypeFromHeaders = "";
                    if (string.IsNullOrWhiteSpace(assetType))
                    {
                        // Check for Server-specific columns
                        var hasIPAddress = headers.Any(h => h.Equals("IP Address", StringComparison.OrdinalIgnoreCase));
                        var hasServerType = headers.Any(h => h.Equals("Server Type", StringComparison.OrdinalIgnoreCase));
                        var hasProjectManager = headers.Any(h => h.Equals("Project Manager Name", StringComparison.OrdinalIgnoreCase));
                        
                        // Check for Application-specific columns
                        var hasVersion = headers.Any(h => h.Equals("Version", StringComparison.OrdinalIgnoreCase));
                        var hasCategory = headers.Any(h => h.Equals("Category", StringComparison.OrdinalIgnoreCase));
                        var hasRequiresLicense = headers.Any(h => h.Equals("Requires License", StringComparison.OrdinalIgnoreCase));
                        
                        // Check for Computer-specific columns
                        var hasAssignedTo = headers.Any(h => h.Equals("Assigned To", StringComparison.OrdinalIgnoreCase));
                        
                        if (hasIPAddress || hasServerType || hasProjectManager)
                        {
                            detectedAssetTypeFromHeaders = "Server";
                        }
                        else if (hasVersion || hasCategory || hasRequiresLicense)
                        {
                            detectedAssetTypeFromHeaders = "Application";
                        }
                        else if (hasAssignedTo)
                        {
                            detectedAssetTypeFromHeaders = "Computer";
                        }
                    }

                    // Process data lines
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        lineNumber++;
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var fields = ParseCsvLine(line);
                        
                        // Determine field indices based on whether Asset Type column exists
                        var assetTagIndex = 0;
                        var assetNameIndex = 1;
                        
                        // If Asset Type column exists and is first, adjust indices
                        if (assetTypeColumnIndex == 0)
                        {
                            assetTagIndex = 1;
                            assetNameIndex = 2;
                        }
                        
                        if (fields.Count < (assetNameIndex + 1))
                        {
                            results.Add($"Line {lineNumber}: Insufficient columns. Expected at least Asset Tag and Asset Name.");
                            errorCount++;
                            continue;
                        }

                        // Determine asset type - prioritize CSV column (most explicit), then route parameter, then header detection, then default
                        var detectedAssetType = "";
                        
                        // First, check for Asset Type column in CSV (most explicit and reliable)
                        if (assetTypeColumnIndex >= 0 && assetTypeColumnIndex < fields.Count)
                        {
                            detectedAssetType = fields[assetTypeColumnIndex]?.Trim() ?? "";
                        }
                        // Second, use the assetType parameter from the route/form
                        if (string.IsNullOrWhiteSpace(detectedAssetType) && !string.IsNullOrWhiteSpace(assetType))
                        {
                            detectedAssetType = assetType.Trim();
                        }
                        // Third, use detected type from headers
                        if (string.IsNullOrWhiteSpace(detectedAssetType) && !string.IsNullOrWhiteSpace(detectedAssetTypeFromHeaders))
                        {
                            detectedAssetType = detectedAssetTypeFromHeaders;
                        }
                        
                        // If still empty, default to Computer
                        if (string.IsNullOrWhiteSpace(detectedAssetType))
                        {
                            detectedAssetType = "Computer"; // Default
                        }

                        // Get asset tag and name using adjusted indices
                        var assetTag = fields.Count > assetTagIndex ? fields[assetTagIndex]?.Trim() ?? "" : "";
                        var assetName = fields.Count > assetNameIndex ? fields[assetNameIndex]?.Trim() ?? "" : "";

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(assetTag))
                        {
                            results.Add($"Line {lineNumber}: Asset Tag is required.");
                            errorCount++;
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(assetName))
                        {
                            results.Add($"Line {lineNumber}: Asset Name is required.");
                            errorCount++;
                            continue;
                        }

                        // Check if asset already exists
                        var existingAsset = await _context.Assets
                            .FirstOrDefaultAsync(a => a.AssetTag == assetTag);
                        if (existingAsset != null)
                        {
                            results.Add($"Line {lineNumber}: Asset with tag '{assetTag}' already exists. Skipped.");
                            errorCount++;
                            continue;
                        }

                        try
                        {
                            BaseAsset asset;

                            if (detectedAssetType.Equals("Computer", StringComparison.OrdinalIgnoreCase))
                            {
                                // Validate user assignment
                                var assignedToValue = GetFieldValue(fields, headers, "Assigned To");
                                var assignedTo = "Unassigned";
                                if (!string.IsNullOrWhiteSpace(assignedToValue))
                                {
                                    // Check if user exists by FullName or Email (case-insensitive)
                                    var assignedToLower = assignedToValue.ToLower();
                                    var userExists = await _userManager.Users
                                        .AnyAsync(u => (u.FullName != null && u.FullName.ToLower() == assignedToLower) ||
                                                       (u.Email != null && u.Email.ToLower() == assignedToLower));
                                    if (userExists)
                                    {
                                        assignedTo = assignedToValue;
                                    }
                                    else
                                    {
                                        results.Add($"Line {lineNumber}: User '{assignedToValue}' not found. Asset assigned to 'Unassigned'.");
                                    }
                                }

                                asset = new Computer
                                {
                                    AssetTag = assetTag,
                                    AssetName = assetName,
                                    AssetType = "Computer",
                                    Brand = GetFieldValue(fields, headers, "Brand"),
                                    Model = GetFieldValue(fields, headers, "Model"),
                                    SerialNumber = GetFieldValue(fields, headers, "Serial Number"),
                                    Location = GetFieldValue(fields, headers, "Location"),
                                    Status = GetFieldValue(fields, headers, "Status", "Active"),
                                    AssignedTo = assignedTo,
                                    OperatingSystem = GetFieldValue(fields, headers, "Operating System"),
                                    Processor = GetFieldValue(fields, headers, "Processor"),
                                    RAM = int.TryParse(GetFieldValue(fields, headers, "RAM (GB)"), out var ram) ? ram : 0,
                                    Storage = int.TryParse(GetFieldValue(fields, headers, "Storage (GB)"), out var storage) ? storage : 0,
                                    PurchaseDate = DateTime.TryParse(GetFieldValue(fields, headers, "Purchase Date"), out var purchaseDate) ? purchaseDate : DateTime.Now,
                                    PurchasePrice = decimal.TryParse(GetFieldValue(fields, headers, "Purchase Price"), out var price) ? price : null,
                                    Vendor = GetFieldValue(fields, headers, "Vendor"),
                                    WarrantyExpiryDate = DateTime.TryParse(GetFieldValue(fields, headers, "Warranty Expiry Date"), out var warranty) ? warranty : null,
                                    Notes = GetFieldValue(fields, headers, "Notes"),
                                    CreatedBy = currentUserName,
                                    CreatedDate = DateTime.Now
                                };
                                _context.Computers.Add((Computer)asset);
                            }
                            else if (detectedAssetType.Equals("Server", StringComparison.OrdinalIgnoreCase))
                            {
                                // Validate project manager assignment
                                var projectManagerValue = GetFieldValue(fields, headers, "Project Manager Name");
                                var projectManager = "";
                                if (!string.IsNullOrWhiteSpace(projectManagerValue))
                                {
                                    // Check if user exists by FullName or Email (case-insensitive)
                                    var projectManagerLower = projectManagerValue.ToLower();
                                    var userExists = await _userManager.Users
                                        .AnyAsync(u => (u.FullName != null && u.FullName.ToLower() == projectManagerLower) ||
                                                       (u.Email != null && u.Email.ToLower() == projectManagerLower));
                                    if (userExists)
                                    {
                                        projectManager = projectManagerValue;
                                    }
                                    else
                                    {
                                        results.Add($"Line {lineNumber}: Project Manager '{projectManagerValue}' not found. Field left empty.");
                                    }
                                }

                                asset = new Server
                                {
                                    AssetTag = assetTag,
                                    AssetName = assetName,
                                    AssetType = "Server",
                                    Brand = GetFieldValue(fields, headers, "Brand"),
                                    Model = GetFieldValue(fields, headers, "Model"),
                                    SerialNumber = GetFieldValue(fields, headers, "Serial Number"),
                                    Location = GetFieldValue(fields, headers, "Location"),
                                    Status = GetFieldValue(fields, headers, "Status", "Active"),
                                    IPAddress = GetFieldValue(fields, headers, "IP Address"),
                                    ServerType = GetFieldValue(fields, headers, "Server Type"),
                                    Purpose = GetFieldValue(fields, headers, "Purpose"),
                                    OperatingSystem = GetFieldValue(fields, headers, "Operating System"),
                                    Processor = GetFieldValue(fields, headers, "Processor"),
                                    RAM = int.TryParse(GetFieldValue(fields, headers, "RAM (GB)"), out var ram) ? ram : 0,
                                    Storage = int.TryParse(GetFieldValue(fields, headers, "Storage (GB)"), out var storage) ? storage : 0,
                                    ProjectManagerName = projectManager,
                                    BackupRequired = bool.TryParse(GetFieldValue(fields, headers, "Backup Required"), out var backup) && backup,
                                    BackupComments = GetFieldValue(fields, headers, "Backup Comments"),
                                    PurchaseDate = DateTime.TryParse(GetFieldValue(fields, headers, "Purchase Date"), out var purchaseDate) ? purchaseDate : DateTime.Now,
                                    PurchasePrice = decimal.TryParse(GetFieldValue(fields, headers, "Purchase Price"), out var price) ? price : null,
                                    Vendor = GetFieldValue(fields, headers, "Vendor"),
                                    WarrantyExpiryDate = DateTime.TryParse(GetFieldValue(fields, headers, "Warranty Expiry Date"), out var warranty) ? warranty : null,
                                    Notes = GetFieldValue(fields, headers, "Notes"),
                                    CreatedBy = currentUserName,
                                    CreatedDate = DateTime.Now
                                };
                                _context.Servers.Add((Server)asset);
                            }
                            else if (detectedAssetType.Equals("Application", StringComparison.OrdinalIgnoreCase))
                            {
                                // Validate application owner assignment
                                var applicationOwnerValue = GetFieldValue(fields, headers, "Application Owner");
                                var applicationOwner = "";
                                if (!string.IsNullOrWhiteSpace(applicationOwnerValue))
                                {
                                    // Check if user exists by FullName or Email (case-insensitive)
                                    var applicationOwnerLower = applicationOwnerValue.ToLower();
                                    var userExists = await _userManager.Users
                                        .AnyAsync(u => (u.FullName != null && u.FullName.ToLower() == applicationOwnerLower) ||
                                                       (u.Email != null && u.Email.ToLower() == applicationOwnerLower));
                                    if (userExists)
                                    {
                                        applicationOwner = applicationOwnerValue;
                                    }
                                    else
                                    {
                                        results.Add($"Line {lineNumber}: Application Owner '{applicationOwnerValue}' not found. Field left empty.");
                                    }
                                }

                                asset = new Application
                                {
                                    AssetTag = assetTag,
                                    AssetName = assetName,
                                    AssetType = "Application",
                                    Brand = GetFieldValue(fields, headers, "Brand"),
                                    Model = GetFieldValue(fields, headers, "Model"),
                                    SerialNumber = GetFieldValue(fields, headers, "Serial Number"),
                                    Location = GetFieldValue(fields, headers, "Location"),
                                    Status = GetFieldValue(fields, headers, "Status", "Active"),
                                    Version = GetFieldValue(fields, headers, "Version"),
                                    Category = GetFieldValue(fields, headers, "Category"),
                                    Description = GetFieldValue(fields, headers, "Description"),
                                    Vendor = GetFieldValue(fields, headers, "Vendor"),
                                    BusinessUnit = GetFieldValue(fields, headers, "Business Unit"),
                                    ApplicationOwner = applicationOwner,
                                    RequiresLicense = bool.TryParse(GetFieldValue(fields, headers, "Requires License"), out var requiresLicense) && requiresLicense,
                                    LicenseKey = GetFieldValue(fields, headers, "License Key"),
                                    LicenseType = GetFieldValue(fields, headers, "License Type"),
                                    TotalLicenses = int.TryParse(GetFieldValue(fields, headers, "Total Licenses"), out var total) ? total : null,
                                    UsedLicenses = int.TryParse(GetFieldValue(fields, headers, "Used Licenses"), out var used) ? used : null,
                                    LicenseExpiryDate = DateTime.TryParse(GetFieldValue(fields, headers, "License Expiry Date"), out var expiry) ? expiry : null,
                                    LicenseHolder = GetFieldValue(fields, headers, "License Holder"),
                                    PurchaseDate = DateTime.TryParse(GetFieldValue(fields, headers, "Purchase Date"), out var purchaseDate) ? purchaseDate : DateTime.Now,
                                    PurchasePrice = decimal.TryParse(GetFieldValue(fields, headers, "Purchase Price"), out var price) ? price : null,
                                    WarrantyExpiryDate = DateTime.TryParse(GetFieldValue(fields, headers, "Warranty Expiry Date"), out var warranty) ? warranty : null,
                                    Notes = GetFieldValue(fields, headers, "Notes"),
                                    CreatedBy = currentUserName,
                                    CreatedDate = DateTime.Now
                                };
                                _context.Applications.Add((Application)asset);
                            }
                            else
                            {
                                results.Add($"Line {lineNumber}: Unknown asset type '{detectedAssetType}'. Supported types: Computer, Server, Application.");
                                errorCount++;
                                continue;
                            }

                            await _context.SaveChangesAsync();

                            // Log the creation
                            _context.AuditLogs.Add(new AuditLog
                            {
                                UserName = currentUserName,
                                Action = "Import Asset",
                                EntityType = asset.AssetType,
                                EntityId = asset.Id,
                                Details = $"Imported {asset.AssetType}: {asset.AssetName} ({asset.AssetTag})",
                                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                                Timestamp = DateTime.Now
                            });

                            results.Add($"Line {lineNumber}: Successfully imported {detectedAssetType} '{assetName}' ({assetTag}).");
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            results.Add($"Line {lineNumber}: Error creating {detectedAssetType} '{assetName}': {ex.Message}");
                            errorCount++;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                ViewBag.Results = results;
                ViewBag.SuccessCount = successCount;
                ViewBag.ErrorCount = errorCount;
                ViewBag.TotalCount = successCount + errorCount;

                if (successCount > 0)
                {
                    TempData["SuccessMessage"] = $"Successfully imported {successCount} asset(s).";
                }
                if (errorCount > 0)
                {
                    TempData["WarningMessage"] = $"{errorCount} asset(s) failed to import. Check details below.";
                }

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error processing CSV file: {ex.Message}";
                return View();
            }
        }

        private List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var currentField = "";
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        currentField += '"';
                        i++; // Skip next quote
                    }
                    else
                    {
                        // Toggle quote state
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == ',' && !inQuotes)
                {
                    // End of field
                    fields.Add(currentField);
                    currentField = "";
                }
                else
                {
                    currentField += ch;
                }
            }

            // Add last field
            fields.Add(currentField);

            return fields;
        }

        private string GetFieldValue(List<string> fields, List<string> headers, string headerName, string defaultValue = "")
        {
            var index = headers.FindIndex(h => h.Equals(headerName, StringComparison.OrdinalIgnoreCase));
            if (index >= 0 && index < fields.Count)
            {
                return fields[index]?.Trim() ?? defaultValue;
            }
            return defaultValue;
        }
    }
}