using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT_Asset_Management_System.Data;
using IT_Asset_Management_System.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace IT_Asset_Management_System.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        // List assets where AssetType == "Computer"
        public async Task<IActionResult> ComputerReport(string? searchTerm, string? statusFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");

            var query = _context.Computers
                .Where(c => !string.IsNullOrWhiteSpace(c.AssetTag) 
                    && !string.IsNullOrWhiteSpace(c.AssetName));

            // Filter by user assignment for non-Admin/IT Manager users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email;
                var userEmail = currentUser.Email;
                
                if (!string.IsNullOrWhiteSpace(userFullName) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    query = query.Where(c => 
                        c.AssignedTo != null && 
                        c.AssignedTo.ToLower() != "unassigned" &&
                        (c.AssignedTo.ToLower() == userFullName.ToLower() || 
                         c.AssignedTo.ToLower() == userEmail.ToLower()));
                }
                else
                {
                    // If user has no name/email, return empty list
                    query = query.Where(c => false);
                }
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(c => 
                    c.AssetTag.Contains(searchTerm) ||
                    c.AssetName.Contains(searchTerm) ||
                    (c.SerialNumber != null && c.SerialNumber.Contains(searchTerm)) ||
                    (c.Location != null && c.Location.Contains(searchTerm)) ||
                    (c.Brand != null && c.Brand.Contains(searchTerm)) ||
                    (c.AssignedTo != null && c.AssignedTo.Contains(searchTerm))
                );
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }

            var computers = await query
                .OrderBy(c => c.AssetTag)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.StatusFilter = statusFilter;
            
            // Get statuses from filtered query only
            var statusQuery = query.Where(c => !string.IsNullOrEmpty(c.Status));
            ViewBag.Statuses = await statusQuery
                .Select(c => c.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            return View(computers);
        }

        // List assets where AssetType == "Server"
        public async Task<IActionResult> ServerReport(string? searchTerm, string? statusFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");

            var query = _context.Servers
                .Include(s => s.ServerApplications)
                    .ThenInclude(sa => sa.Application)
                .Where(s => !string.IsNullOrWhiteSpace(s.AssetTag) 
                    && !string.IsNullOrWhiteSpace(s.AssetName));

            // Filter by user assignment for non-Admin/IT Manager users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email;
                var userEmail = currentUser.Email;
                
                if (!string.IsNullOrWhiteSpace(userFullName) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    query = query.Where(s => 
                        s.ProjectManagerName != null &&
                        (s.ProjectManagerName.ToLower() == userFullName.ToLower() || 
                         s.ProjectManagerName.ToLower() == userEmail.ToLower()));
                }
                else
                {
                    // If user has no name/email, return empty list
                    query = query.Where(s => false);
                }
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(s => 
                    s.AssetTag.Contains(searchTerm) ||
                    s.AssetName.Contains(searchTerm) ||
                    (s.SerialNumber != null && s.SerialNumber.Contains(searchTerm)) ||
                    (s.IPAddress != null && s.IPAddress.Contains(searchTerm)) ||
                    (s.Location != null && s.Location.Contains(searchTerm)) ||
                    (s.ServerType != null && s.ServerType.Contains(searchTerm))
                );
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(s => s.Status == statusFilter);
            }

            var servers = await query
                .OrderBy(s => s.AssetTag)
                .ToListAsync();

            // Get application counts for each server using ServerApplications
            var applicationCounts = servers.ToDictionary(
                s => s.Id, 
                s => s.ServerApplications?.Count ?? 0
            );

            ViewBag.ApplicationCounts = applicationCounts;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.Statuses = await _context.Servers
                .Where(s => !string.IsNullOrEmpty(s.Status))
                .Select(s => s.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            return View(servers);
        }

        // List assets where AssetType == "Application"
        public async Task<IActionResult> ApplicationReport(string? searchTerm, string? statusFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");

            var query = _context.Applications
                .Include(a => a.ServerApplications)
                    .ThenInclude(sa => sa.Server)
                .Where(a => !string.IsNullOrWhiteSpace(a.AssetTag) 
                    && !string.IsNullOrWhiteSpace(a.AssetName));

            // Filter by user assignment for non-Admin/IT Manager users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email;
                var userEmail = currentUser.Email;
                
                if (!string.IsNullOrWhiteSpace(userFullName) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    query = query.Where(a => 
                        a.ApplicationOwner != null &&
                        (a.ApplicationOwner.ToLower() == userFullName.ToLower() || 
                         a.ApplicationOwner.ToLower() == userEmail.ToLower()));
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
                    (a.Version != null && a.Version.Contains(searchTerm)) ||
                    (a.Vendor != null && a.Vendor.Contains(searchTerm)) ||
                    (a.Category != null && a.Category.Contains(searchTerm)) ||
                    (a.Location != null && a.Location.Contains(searchTerm))
                );
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            var applications = await query
                .OrderBy(a => a.AssetTag)
                .ToListAsync();

            // Get server counts for each application using ServerApplications
            var serverCounts = applications.ToDictionary(
                a => a.Id, 
                a => a.ServerApplications?.Count ?? 0
            );

            ViewBag.ServerCounts = serverCounts;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.Statuses = await _context.Applications
                .Where(a => !string.IsNullOrEmpty(a.Status))
                .Select(a => a.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            return View(applications);
        }

        // Applications requiring a license
        public async Task<IActionResult> LicenseReport()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");

            var query = _context.Applications
                .Where(a => a.RequiresLicense && !string.IsNullOrEmpty(a.AssetTag) && !string.IsNullOrEmpty(a.AssetName));

            // Filter by user assignment for non-Admin/IT Manager users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email;
                var userEmail = currentUser.Email;
                
                if (!string.IsNullOrWhiteSpace(userFullName) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    query = query.Where(a => 
                        a.ApplicationOwner != null &&
                        (a.ApplicationOwner.ToLower() == userFullName.ToLower() || 
                         a.ApplicationOwner.ToLower() == userEmail.ToLower()));
                }
                else
                {
                    // If user has no name/email, return empty list
                    query = query.Where(a => false);
                }
            }

            var applications = await query
                .OrderBy(a => a.LicenseExpiryDate)
                .ToListAsync();

            return View(applications);
        }

        public async Task<IActionResult> ExportLicenseReportToCsv()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");

            var query = _context.Applications
                .Where(a => a.RequiresLicense && !string.IsNullOrEmpty(a.AssetTag) && !string.IsNullOrEmpty(a.AssetName));

            // Filter by user assignment for non-Admin/IT Manager users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email;
                var userEmail = currentUser.Email;
                
                if (!string.IsNullOrWhiteSpace(userFullName) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    query = query.Where(a => 
                        a.ApplicationOwner != null &&
                        (a.ApplicationOwner.ToLower() == userFullName.ToLower() || 
                         a.ApplicationOwner.ToLower() == userEmail.ToLower()));
                }
                else
                {
                    // If user has no name/email, return empty list
                    query = query.Where(a => false);
                }
            }

            var applications = await query
                .OrderBy(a => a.LicenseExpiryDate)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Asset Tag,Application Name,Vendor,Business Unit,Application Owner,License Holder,License Type,Total Licenses,Used Licenses,Available Licenses,Expiry Date,Days Until Expiry,Status");

            foreach (var item in applications)
            {
                var daysUntilExpiry = item.LicenseExpiryDate.HasValue ? 
                    (item.LicenseExpiryDate.Value - DateTime.Now).Days : 0;
                var available = (item.TotalLicenses ?? 0) - (item.UsedLicenses ?? 0);
                
                csv.AppendLine($"{EscapeCsvField(item.AssetTag)},{EscapeCsvField(item.AssetName)},{EscapeCsvField(item.Vendor)},{EscapeCsvField(item.BusinessUnit)},{EscapeCsvField(item.ApplicationOwner)},{EscapeCsvField(item.LicenseHolder)},{EscapeCsvField(item.LicenseType)},{item.TotalLicenses?.ToString() ?? "—"},{item.UsedLicenses?.ToString() ?? "—"},{available},{item.LicenseExpiryDate?.ToString("yyyy-MM-dd") ?? "N/A"},{daysUntilExpiry},{EscapeCsvField(item.Status)}");
            }

            var fileName = $"LicenseReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        public async Task<IActionResult> ExportLicenseReportToPdf()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");

            var query = _context.Applications
                .Where(a => a.RequiresLicense && !string.IsNullOrEmpty(a.AssetTag) && !string.IsNullOrEmpty(a.AssetName));

            // Filter by user assignment for non-Admin/IT Manager users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email;
                var userEmail = currentUser.Email;
                
                if (!string.IsNullOrWhiteSpace(userFullName) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    query = query.Where(a => 
                        a.ApplicationOwner != null &&
                        (a.ApplicationOwner.ToLower() == userFullName.ToLower() || 
                         a.ApplicationOwner.ToLower() == userEmail.ToLower()));
                }
                else
                {
                    // If user has no name/email, return empty list
                    query = query.Where(a => false);
                }
            }

            var applications = await query
                .OrderBy(a => a.LicenseExpiryDate)
                .ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Header()
                        .Text("License Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);
                            column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(8).FontColor(Colors.Grey.Medium);
                            column.Item().Text($"Total Licensed Applications: {applications.Count}").FontSize(8).FontColor(Colors.Grey.Medium);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.3f);
                                    columns.RelativeColumn(1.2f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Asset Tag");
                                    header.Cell().Element(CellStyle).Text("Application Name");
                                    header.Cell().Element(CellStyle).Text("Vendor");
                                    header.Cell().Element(CellStyle).Text("Business Unit");
                                    header.Cell().Element(CellStyle).Text("App Owner");
                                    header.Cell().Element(CellStyle).Text("License Holder");
                                    header.Cell().Element(CellStyle).Text("License Type");
                                    header.Cell().Element(CellStyle).Text("Total");
                                    header.Cell().Element(CellStyle).Text("Used");
                                    header.Cell().Element(CellStyle).Text("Available");
                                    header.Cell().Element(CellStyle).Text("Expiry Date");
                                    header.Cell().Element(CellStyle).Text("Status");
                                });

                                foreach (var item in applications)
                                {
                                    var daysUntilExpiry = item.LicenseExpiryDate.HasValue ? 
                                        (item.LicenseExpiryDate.Value - DateTime.Now).Days : 0;
                                    var available = (item.TotalLicenses ?? 0) - (item.UsedLicenses ?? 0);
                                    
                                    table.Cell().Element(CellStyle).Text(item.AssetTag ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.AssetName ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.Vendor ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.BusinessUnit ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.ApplicationOwner ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.LicenseHolder ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.LicenseType ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.TotalLicenses?.ToString() ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.UsedLicenses?.ToString() ?? "—");
                                    table.Cell().Element(CellStyle).Text(available.ToString());
                                    table.Cell().Element(CellStyle).Text(item.LicenseExpiryDate?.ToString("yyyy-MM-dd") ?? "N/A");
                                    table.Cell().Element(CellStyle).Text(item.Status ?? "—");
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
            var fileName = $"LicenseReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
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

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> ExportAuditLogToCsv()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(500)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,User,Action,Entity Type,Entity ID,Details,IP Address");

            foreach (var log in logs)
            {
                csv.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss},{EscapeCsvField(log.UserName)},{EscapeCsvField(log.Action)},{EscapeCsvField(log.EntityType)},{log.EntityId?.ToString() ?? "—"},{EscapeCsvField(log.Details)},{EscapeCsvField(log.IPAddress)}");
            }

            var fileName = $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> ExportAuditLogToPdf()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(500)
                .ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Header()
                        .Text("Audit Log Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);
                            column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(8).FontColor(Colors.Grey.Medium);
                            column.Item().Text($"Total Records: {logs.Count}").FontSize(8).FontColor(Colors.Grey.Medium);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2.5f);
                                    columns.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Timestamp");
                                    header.Cell().Element(CellStyle).Text("User");
                                    header.Cell().Element(CellStyle).Text("Action");
                                    header.Cell().Element(CellStyle).Text("Entity Type");
                                    header.Cell().Element(CellStyle).Text("Entity ID");
                                    header.Cell().Element(CellStyle).Text("Details");
                                    header.Cell().Element(CellStyle).Text("IP Address");
                                });

                                foreach (var log in logs)
                                {
                                    table.Cell().Element(CellStyle).Text(log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                                    table.Cell().Element(CellStyle).Text(log.UserName ?? "—");
                                    table.Cell().Element(CellStyle).Text(log.Action ?? "—");
                                    table.Cell().Element(CellStyle).Text(log.EntityType ?? "—");
                                    table.Cell().Element(CellStyle).Text(log.EntityId?.ToString() ?? "—");
                                    table.Cell().Element(CellStyle).Text(log.Details ?? "—");
                                    table.Cell().Element(CellStyle).Text(log.IPAddress ?? "—");
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
            var fileName = $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
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
        public async Task<IActionResult> AssetSummary(string? assetTypeFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");

            var computerQuery = _context.Computers.AsQueryable();
            var serverQuery = _context.Servers.AsQueryable();
            var applicationQuery = _context.Applications.AsQueryable();

            // Filter by user assignment for non-Admin/IT Manager users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email;
                var userEmail = currentUser.Email;
                
                computerQuery = computerQuery.Where(c => 
                    (c.AssignedTo == userFullName || c.AssignedTo == userEmail) 
                    && c.AssignedTo.ToLower() != "unassigned");
                
                serverQuery = serverQuery.Where(s => 
                    s.ProjectManagerName == userFullName || s.ProjectManagerName == userEmail);
                
                applicationQuery = applicationQuery.Where(a => 
                    a.ApplicationOwner == userFullName || a.ApplicationOwner == userEmail);
            }

            // Apply asset type filter if provided
            if (!string.IsNullOrWhiteSpace(assetTypeFilter))
            {
                if (assetTypeFilter == "Computer")
                {
                    serverQuery = serverQuery.Where(s => false); // Empty query
                    applicationQuery = applicationQuery.Where(a => false);
                }
                else if (assetTypeFilter == "Server")
                {
                    computerQuery = computerQuery.Where(c => false);
                    applicationQuery = applicationQuery.Where(a => false);
                }
                else if (assetTypeFilter == "Application")
                {
                    computerQuery = computerQuery.Where(c => false);
                    serverQuery = serverQuery.Where(s => false);
                }
            }

            ViewBag.TotalComputers = await computerQuery.CountAsync();
            ViewBag.TotalServers = await serverQuery.CountAsync();
            ViewBag.TotalApplications = await applicationQuery.CountAsync();

            ViewBag.ComputersByStatus = await computerQuery
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.ServersByStatus = await serverQuery
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.ApplicationsByStatus = await applicationQuery
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.AssetTypeFilter = assetTypeFilter;
            return View();
        }

        // CSV Export Methods
        public async Task<IActionResult> ExportComputerReportToCsv(string? searchTerm, string? statusFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");

            var query = _context.Computers
                .Where(c => !string.IsNullOrWhiteSpace(c.AssetTag) 
                    && !string.IsNullOrWhiteSpace(c.AssetName));

            // Filter by user assignment for non-Admin/IT Manager users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email;
                var userEmail = currentUser.Email;
                
                query = query.Where(c => 
                    (c.AssignedTo == userFullName || c.AssignedTo == userEmail) 
                    && c.AssignedTo.ToLower() != "unassigned");
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(c => 
                    c.AssetTag.Contains(searchTerm) ||
                    c.AssetName.Contains(searchTerm) ||
                    (c.SerialNumber != null && c.SerialNumber.Contains(searchTerm)) ||
                    (c.Location != null && c.Location.Contains(searchTerm)) ||
                    (c.Brand != null && c.Brand.Contains(searchTerm)) ||
                    (c.AssignedTo != null && c.AssignedTo.Contains(searchTerm))
                );
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }

            var computers = await query.OrderBy(c => c.AssetName).ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Asset Tag,Asset Name,Serial Number,Location,Assigned To,Operating System,Status,Purchase Date");

            foreach (var item in computers)
            {
                csv.AppendLine($"{EscapeCsvField(item.AssetTag)},{EscapeCsvField(item.AssetName)},{EscapeCsvField(item.SerialNumber)},{EscapeCsvField(item.Location)},{EscapeCsvField(item.AssignedTo)},{EscapeCsvField(item.OperatingSystem)},{EscapeCsvField(item.Status)},{item.PurchaseDate:yyyy-MM-dd}");
            }

            var fileName = $"ComputerReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        public async Task<IActionResult> ExportServerReportToCsv(string? searchTerm, string? statusFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");

            var query = _context.Servers
                .Include(s => s.ServerApplications)
                .Where(s => !string.IsNullOrWhiteSpace(s.AssetTag) 
                    && !string.IsNullOrWhiteSpace(s.AssetName));

            // Filter by user assignment for non-Admin/IT Manager users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email;
                var userEmail = currentUser.Email;
                
                if (!string.IsNullOrWhiteSpace(userFullName) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    query = query.Where(s => 
                        s.ProjectManagerName != null &&
                        (s.ProjectManagerName.ToLower() == userFullName.ToLower() || 
                         s.ProjectManagerName.ToLower() == userEmail.ToLower()));
                }
                else
                {
                    // If user has no name/email, return empty list
                    query = query.Where(s => false);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(s => 
                    s.AssetTag.Contains(searchTerm) ||
                    s.AssetName.Contains(searchTerm) ||
                    (s.SerialNumber != null && s.SerialNumber.Contains(searchTerm)) ||
                    (s.IPAddress != null && s.IPAddress.Contains(searchTerm)) ||
                    (s.Location != null && s.Location.Contains(searchTerm)) ||
                    (s.ServerType != null && s.ServerType.Contains(searchTerm))
                );
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(s => s.Status == statusFilter);
            }

            var servers = await query.OrderBy(s => s.AssetName).ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Asset Tag,Server Name,Serial Number,IP Address,Server Type,Location,Status,Operating System,Applications Installed");

            foreach (var item in servers)
            {
                var appCount = item.ServerApplications?.Count ?? 0;
                csv.AppendLine($"{EscapeCsvField(item.AssetTag)},{EscapeCsvField(item.AssetName)},{EscapeCsvField(item.SerialNumber)},{EscapeCsvField(item.IPAddress)},{EscapeCsvField(item.ServerType)},{EscapeCsvField(item.Location)},{EscapeCsvField(item.Status)},{EscapeCsvField(item.OperatingSystem)},{appCount}");
            }

            var fileName = $"ServerReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        public async Task<IActionResult> ExportApplicationReportToCsv(string? searchTerm, string? statusFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");

            var query = _context.Applications
                .Include(a => a.ServerApplications)
                .Where(a => !string.IsNullOrWhiteSpace(a.AssetTag) 
                    && !string.IsNullOrWhiteSpace(a.AssetName));

            // Filter by user assignment for non-Admin/IT Manager users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email;
                var userEmail = currentUser.Email;
                
                if (!string.IsNullOrWhiteSpace(userFullName) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    query = query.Where(a => 
                        a.ApplicationOwner != null &&
                        (a.ApplicationOwner.ToLower() == userFullName.ToLower() || 
                         a.ApplicationOwner.ToLower() == userEmail.ToLower()));
                }
                else
                {
                    // If user has no name/email, return empty list
                    query = query.Where(a => false);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(a => 
                    a.AssetTag.Contains(searchTerm) ||
                    a.AssetName.Contains(searchTerm) ||
                    (a.Version != null && a.Version.Contains(searchTerm)) ||
                    (a.Vendor != null && a.Vendor.Contains(searchTerm)) ||
                    (a.Category != null && a.Category.Contains(searchTerm)) ||
                    (a.Location != null && a.Location.Contains(searchTerm))
                );
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            var applications = await query.OrderBy(a => a.AssetTag).ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Asset Tag,Application Name,Version,Category,Vendor,Status,Requires License,License Type,License Expiry,Servers Using");

            foreach (var item in applications)
            {
                var serverCount = item.ServerApplications?.Count ?? 0;
                csv.AppendLine($"{EscapeCsvField(item.AssetTag)},{EscapeCsvField(item.AssetName)},{EscapeCsvField(item.Version)},{EscapeCsvField(item.Category)},{EscapeCsvField(item.Vendor)},{EscapeCsvField(item.Status)},{(item.RequiresLicense ? "Yes" : "No")},{EscapeCsvField(item.LicenseType)},{item.LicenseExpiryDate?.ToString("yyyy-MM-dd") ?? "N/A"},{serverCount}");
            }

            var fileName = $"ApplicationReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        public async Task<IActionResult> ExportAssetSummaryToCsv(string? assetTypeFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");

            var csv = new StringBuilder();
            csv.AppendLine("Report Type,Asset Type,Status,Count");

            var computerQuery = _context.Computers.AsQueryable();
            var serverQuery = _context.Servers.AsQueryable();
            var applicationQuery = _context.Applications.AsQueryable();

            // Filter by user assignment for non-Admin/IT Manager users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email ?? "";
                var userEmail = currentUser.Email ?? "";
                
                if (!string.IsNullOrWhiteSpace(userFullName) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    computerQuery = computerQuery.Where(c => 
                        c.AssignedTo != null &&
                        c.AssignedTo.ToLower() != "unassigned" &&
                        (c.AssignedTo.ToLower() == userFullName.ToLower() || 
                         c.AssignedTo.ToLower() == userEmail.ToLower()));
                    
                    serverQuery = serverQuery.Where(s => 
                        s.ProjectManagerName != null &&
                        (s.ProjectManagerName.ToLower() == userFullName.ToLower() || 
                         s.ProjectManagerName.ToLower() == userEmail.ToLower()));
                    
                    applicationQuery = applicationQuery.Where(a => 
                        a.ApplicationOwner != null &&
                        (a.ApplicationOwner.ToLower() == userFullName.ToLower() || 
                         a.ApplicationOwner.ToLower() == userEmail.ToLower()));
                }
                else
                {
                    // If user has no name/email, return empty lists
                    computerQuery = computerQuery.Where(c => false);
                    serverQuery = serverQuery.Where(s => false);
                    applicationQuery = applicationQuery.Where(a => false);
                }
            }

            if (!string.IsNullOrWhiteSpace(assetTypeFilter))
            {
                if (assetTypeFilter == "Computer")
                {
                    serverQuery = serverQuery.Where(s => false);
                    applicationQuery = applicationQuery.Where(a => false);
                }
                else if (assetTypeFilter == "Server")
                {
                    computerQuery = computerQuery.Where(c => false);
                    applicationQuery = applicationQuery.Where(a => false);
                }
                else if (assetTypeFilter == "Application")
                {
                    computerQuery = computerQuery.Where(c => false);
                    serverQuery = serverQuery.Where(s => false);
                }
            }

            // Computers by status
            var computersByStatus = await computerQuery
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            
            foreach (var item in computersByStatus)
            {
                csv.AppendLine($"Asset Summary,Computer,{EscapeCsvField(item.Status)},{item.Count}");
            }

            // Servers by status
            var serversByStatus = await serverQuery
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            
            foreach (var item in serversByStatus)
            {
                csv.AppendLine($"Asset Summary,Server,{EscapeCsvField(item.Status)},{item.Count}");
            }

            // Applications by status
            var applicationsByStatus = await applicationQuery
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            
            foreach (var item in applicationsByStatus)
            {
                csv.AppendLine($"Asset Summary,Application,{EscapeCsvField(item.Status)},{item.Count}");
            }

            var fileName = $"AssetSummaryReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        // PDF Export Methods
        public async Task<IActionResult> ExportComputerReportToPdf(string? searchTerm, string? statusFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");

            var query = _context.Computers
                .Where(c => !string.IsNullOrWhiteSpace(c.AssetTag) 
                    && !string.IsNullOrWhiteSpace(c.AssetName));

            // Filter by user assignment for non-Admin/IT Manager users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email;
                var userEmail = currentUser.Email;
                
                query = query.Where(c => 
                    (c.AssignedTo == userFullName || c.AssignedTo == userEmail) 
                    && c.AssignedTo.ToLower() != "unassigned");
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(c => 
                    c.AssetTag.Contains(searchTerm) ||
                    c.AssetName.Contains(searchTerm) ||
                    (c.SerialNumber != null && c.SerialNumber.Contains(searchTerm)) ||
                    (c.Location != null && c.Location.Contains(searchTerm)) ||
                    (c.Brand != null && c.Brand.Contains(searchTerm)) ||
                    (c.AssignedTo != null && c.AssignedTo.Contains(searchTerm))
                );
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }

            var computers = await query.OrderBy(c => c.AssetName).ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Text("Computer Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);
                            column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(8).FontColor(Colors.Grey.Medium);
                            column.Item().Text($"Total Records: {computers.Count}").FontSize(8).FontColor(Colors.Grey.Medium);
                            
                            if (!string.IsNullOrWhiteSpace(searchTerm))
                                column.Item().Text($"Search Term: {searchTerm}").FontSize(8).FontColor(Colors.Grey.Medium);
                            if (!string.IsNullOrWhiteSpace(statusFilter))
                                column.Item().Text($"Status Filter: {statusFilter}").FontSize(8).FontColor(Colors.Grey.Medium);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Asset Tag");
                                    header.Cell().Element(CellStyle).Text("Asset Name");
                                    header.Cell().Element(CellStyle).Text("Serial Number");
                                    header.Cell().Element(CellStyle).Text("Location");
                                    header.Cell().Element(CellStyle).Text("Assigned To");
                                    header.Cell().Element(CellStyle).Text("Operating System");
                                    header.Cell().Element(CellStyle).Text("Status");
                                    header.Cell().Element(CellStyle).Text("Purchase Date");
                                });

                                foreach (var item in computers)
                                {
                                    table.Cell().Element(CellStyle).Text(item.AssetTag ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.AssetName ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.SerialNumber ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.Location ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.AssignedTo ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.OperatingSystem ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.Status ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.PurchaseDate.ToString("yyyy-MM-dd"));
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
            var fileName = $"ComputerReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        public async Task<IActionResult> ExportServerReportToPdf(string? searchTerm, string? statusFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");

            var query = _context.Servers
                .Include(s => s.ServerApplications)
                .Where(s => !string.IsNullOrWhiteSpace(s.AssetTag) 
                    && !string.IsNullOrWhiteSpace(s.AssetName));

            // Filter by user assignment for non-Admin/IT Manager users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email;
                var userEmail = currentUser.Email;
                
                if (!string.IsNullOrWhiteSpace(userFullName) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    query = query.Where(s => 
                        s.ProjectManagerName != null &&
                        (s.ProjectManagerName.ToLower() == userFullName.ToLower() || 
                         s.ProjectManagerName.ToLower() == userEmail.ToLower()));
                }
                else
                {
                    // If user has no name/email, return empty list
                    query = query.Where(s => false);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(s => 
                    s.AssetTag.Contains(searchTerm) ||
                    s.AssetName.Contains(searchTerm) ||
                    (s.SerialNumber != null && s.SerialNumber.Contains(searchTerm)) ||
                    (s.IPAddress != null && s.IPAddress.Contains(searchTerm)) ||
                    (s.Location != null && s.Location.Contains(searchTerm)) ||
                    (s.ServerType != null && s.ServerType.Contains(searchTerm))
                );
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(s => s.Status == statusFilter);
            }

            var servers = await query.OrderBy(s => s.AssetTag).ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header()
                        .Text("Server Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);
                            column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(8).FontColor(Colors.Grey.Medium);
                            column.Item().Text($"Total Records: {servers.Count}").FontSize(8).FontColor(Colors.Grey.Medium);
                            
                            if (!string.IsNullOrWhiteSpace(searchTerm))
                                column.Item().Text($"Search Term: {searchTerm}").FontSize(8).FontColor(Colors.Grey.Medium);
                            if (!string.IsNullOrWhiteSpace(statusFilter))
                                column.Item().Text($"Status Filter: {statusFilter}").FontSize(8).FontColor(Colors.Grey.Medium);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Asset Tag");
                                    header.Cell().Element(CellStyle).Text("Server Name");
                                    header.Cell().Element(CellStyle).Text("Serial Number");
                                    header.Cell().Element(CellStyle).Text("IP Address");
                                    header.Cell().Element(CellStyle).Text("Server Type");
                                    header.Cell().Element(CellStyle).Text("Location");
                                    header.Cell().Element(CellStyle).Text("Status");
                                    header.Cell().Element(CellStyle).Text("Operating System");
                                    header.Cell().Element(CellStyle).Text("Applications");
                                });

                                foreach (var item in servers)
                                {
                                    var appCount = item.ServerApplications?.Count ?? 0;
                                    table.Cell().Element(CellStyle).Text(item.AssetTag ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.AssetName ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.SerialNumber ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.IPAddress ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.ServerType ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.Location ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.Status ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.OperatingSystem ?? "—");
                                    table.Cell().Element(CellStyle).Text(appCount.ToString());
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
            var fileName = $"ServerReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        public async Task<IActionResult> ExportApplicationReportToPdf(string? searchTerm, string? statusFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");

            var query = _context.Applications
                .Include(a => a.ServerApplications)
                .Where(a => !string.IsNullOrWhiteSpace(a.AssetTag) 
                    && !string.IsNullOrWhiteSpace(a.AssetName));

            // Filter by user assignment for non-Admin/IT Manager users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email;
                var userEmail = currentUser.Email;
                
                if (!string.IsNullOrWhiteSpace(userFullName) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    query = query.Where(a => 
                        a.ApplicationOwner != null &&
                        (a.ApplicationOwner.ToLower() == userFullName.ToLower() || 
                         a.ApplicationOwner.ToLower() == userEmail.ToLower()));
                }
                else
                {
                    // If user has no name/email, return empty list
                    query = query.Where(a => false);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(a => 
                    a.AssetTag.Contains(searchTerm) ||
                    a.AssetName.Contains(searchTerm) ||
                    (a.Version != null && a.Version.Contains(searchTerm)) ||
                    (a.Vendor != null && a.Vendor.Contains(searchTerm)) ||
                    (a.Category != null && a.Category.Contains(searchTerm)) ||
                    (a.Location != null && a.Location.Contains(searchTerm))
                );
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            var applications = await query.OrderBy(a => a.AssetName).ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header()
                        .Text("Application Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);
                            column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(8).FontColor(Colors.Grey.Medium);
                            column.Item().Text($"Total Records: {applications.Count}").FontSize(8).FontColor(Colors.Grey.Medium);
                            
                            if (!string.IsNullOrWhiteSpace(searchTerm))
                                column.Item().Text($"Search Term: {searchTerm}").FontSize(8).FontColor(Colors.Grey.Medium);
                            if (!string.IsNullOrWhiteSpace(statusFilter))
                                column.Item().Text($"Status Filter: {statusFilter}").FontSize(8).FontColor(Colors.Grey.Medium);

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
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Asset Tag");
                                    header.Cell().Element(CellStyle).Text("Application Name");
                                    header.Cell().Element(CellStyle).Text("Version");
                                    header.Cell().Element(CellStyle).Text("Category");
                                    header.Cell().Element(CellStyle).Text("Vendor");
                                    header.Cell().Element(CellStyle).Text("Status");
                                    header.Cell().Element(CellStyle).Text("Requires License");
                                    header.Cell().Element(CellStyle).Text("License Type");
                                    header.Cell().Element(CellStyle).Text("License Expiry");
                                    header.Cell().Element(CellStyle).Text("Servers Using");
                                });

                                foreach (var item in applications)
                                {
                                    var serverCount = item.ServerApplications?.Count ?? 0;
                                    table.Cell().Element(CellStyle).Text(item.AssetTag ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.AssetName ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.Version ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.Category ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.Vendor ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.Status ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.RequiresLicense ? "Yes" : "No");
                                    table.Cell().Element(CellStyle).Text(item.LicenseType ?? "—");
                                    table.Cell().Element(CellStyle).Text(item.LicenseExpiryDate?.ToString("yyyy-MM-dd") ?? "N/A");
                                    table.Cell().Element(CellStyle).Text(serverCount.ToString());
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
            var fileName = $"ApplicationReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        public async Task<IActionResult> ExportAssetSummaryToPdf(string? assetTypeFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminOrITManager = userRoles.Any(r => r == "Admin" || r == "IT Manager");

            var computerQuery = _context.Computers.AsQueryable();
            var serverQuery = _context.Servers.AsQueryable();
            var applicationQuery = _context.Applications.AsQueryable();

            // Filter by user assignment for non-Admin/IT Manager users
            if (!isAdminOrITManager)
            {
                var userFullName = currentUser.FullName ?? currentUser.UserName ?? currentUser.Email;
                var userEmail = currentUser.Email;
                
                if (!string.IsNullOrWhiteSpace(userFullName) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    computerQuery = computerQuery.Where(c => 
                        c.AssignedTo != null &&
                        c.AssignedTo.ToLower() != "unassigned" &&
                        (c.AssignedTo.ToLower() == userFullName.ToLower() || 
                         c.AssignedTo.ToLower() == userEmail.ToLower()));
                    
                    serverQuery = serverQuery.Where(s => 
                        s.ProjectManagerName != null &&
                        (s.ProjectManagerName.ToLower() == userFullName.ToLower() || 
                         s.ProjectManagerName.ToLower() == userEmail.ToLower()));
                    
                    applicationQuery = applicationQuery.Where(a => 
                        a.ApplicationOwner != null &&
                        (a.ApplicationOwner.ToLower() == userFullName.ToLower() || 
                         a.ApplicationOwner.ToLower() == userEmail.ToLower()));
                }
                else
                {
                    // If user has no name/email, return empty lists
                    computerQuery = computerQuery.Where(c => false);
                    serverQuery = serverQuery.Where(s => false);
                    applicationQuery = applicationQuery.Where(a => false);
                }
            }

            if (!string.IsNullOrWhiteSpace(assetTypeFilter))
            {
                if (assetTypeFilter == "Computer")
                {
                    serverQuery = serverQuery.Where(s => false);
                    applicationQuery = applicationQuery.Where(a => false);
                }
                else if (assetTypeFilter == "Server")
                {
                    computerQuery = computerQuery.Where(c => false);
                    applicationQuery = applicationQuery.Where(a => false);
                }
                else if (assetTypeFilter == "Application")
                {
                    computerQuery = computerQuery.Where(c => false);
                    serverQuery = serverQuery.Where(s => false);
                }
            }

            var totalComputers = await computerQuery.CountAsync();
            var totalServers = await serverQuery.CountAsync();
            var totalApplications = await applicationQuery.CountAsync();

            var computersByStatus = await computerQuery
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var serversByStatus = await serverQuery
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var applicationsByStatus = await applicationQuery
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Text("Asset Summary Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(15);
                            column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(8).FontColor(Colors.Grey.Medium);
                            
                            if (!string.IsNullOrWhiteSpace(assetTypeFilter))
                                column.Item().Text($"Asset Type Filter: {assetTypeFilter}").FontSize(8).FontColor(Colors.Grey.Medium);

                            // Summary totals
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Element(BoxStyle).Text($"Total Computers: {totalComputers}").SemiBold();
                                row.RelativeItem().Element(BoxStyle).Text($"Total Servers: {totalServers}").SemiBold();
                                row.RelativeItem().Element(BoxStyle).Text($"Total Applications: {totalApplications}").SemiBold();
                            });

                            // Computers by status
                            if (computersByStatus.Any())
                            {
                                column.Item().Text("Computers by Status").SemiBold().FontSize(12);
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Status");
                                        header.Cell().Element(CellStyle).Text("Count");
                                    });

                                    foreach (var item in computersByStatus)
                                    {
                                        table.Cell().Element(CellStyle).Text(item.Status ?? "—");
                                        table.Cell().Element(CellStyle).Text(item.Count.ToString());
                                    }
                                });
                            }

                            // Servers by status
                            if (serversByStatus.Any())
                            {
                                column.Item().Text("Servers by Status").SemiBold().FontSize(12);
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Status");
                                        header.Cell().Element(CellStyle).Text("Count");
                                    });

                                    foreach (var item in serversByStatus)
                                    {
                                        table.Cell().Element(CellStyle).Text(item.Status ?? "—");
                                        table.Cell().Element(CellStyle).Text(item.Count.ToString());
                                    }
                                });
                            }

                            // Applications by status
                            if (applicationsByStatus.Any())
                            {
                                column.Item().Text("Applications by Status").SemiBold().FontSize(12);
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Status");
                                        header.Cell().Element(CellStyle).Text("Count");
                                    });

                                    foreach (var item in applicationsByStatus)
                                    {
                                        table.Cell().Element(CellStyle).Text(item.Status ?? "—");
                                        table.Cell().Element(CellStyle).Text(item.Count.ToString());
                                    }
                                });
                            }
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
            var fileName = $"AssetSummaryReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // Helper methods
        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(5)
                .PaddingHorizontal(5);
        }

        private static IContainer BoxStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Blue.Medium)
                .Padding(10)
                .Background(Colors.Blue.Lighten5);
        }
    }
}
