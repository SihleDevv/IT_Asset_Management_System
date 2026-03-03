using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT_Asset_Management_System.Data;
using IT_Asset_Management_System.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IT_Asset_Management_System.Controllers
{
    [Authorize]
    public class ITSupportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ITSupportController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ITSupport
        public async Task<IActionResult> Index(string? status, string? priority)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            IQueryable<ITSupportTicket> tickets = _context.ITSupportTickets
                .Include(t => t.ReportedByUser)
                .Include(t => t.AssignedToUser)
                .OrderByDescending(t => t.CreatedDate);

            // Filter by role
            if (User.IsInRole("Admin") || User.IsInRole("IT Manager") || 
                User.IsInRole("IT Support Supervisor") || User.IsInRole("IT Support"))
            {
                // Admin, IT Manager, and IT Support roles see all tickets
                // IT Support see tickets assigned to them or unassigned
                if (User.IsInRole("IT Support") && !User.IsInRole("IT Support Supervisor") && 
                    !User.IsInRole("Admin") && !User.IsInRole("IT Manager"))
                {
                    tickets = tickets.Where(t => t.AssignedToUserId == currentUser.Id || t.AssignedToUserId == null);
                }
            }
            else
            {
                // Regular users only see their own tickets
                tickets = tickets.Where(t => t.ReportedByUserId == currentUser.Id);
            }

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                tickets = tickets.Where(t => t.Status == status);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                tickets = tickets.Where(t => t.Priority == priority);
            }

            ViewBag.StatusFilter = status;
            ViewBag.PriorityFilter = priority;
            ViewBag.IsAdminOrITManager = User.IsInRole("Admin") || User.IsInRole("IT Manager");
            ViewBag.IsITSupportSupervisor = User.IsInRole("IT Support Supervisor") || User.IsInRole("Admin") || User.IsInRole("IT Manager");
            ViewBag.IsITSupport = User.IsInRole("IT Support") || User.IsInRole("IT Support Supervisor") || User.IsInRole("Admin") || User.IsInRole("IT Manager");

            return View(await tickets.ToListAsync());
        }

        // GET: ITSupport/ExportToCsv
        public async Task<IActionResult> ExportToCsv(string? status, string? priority)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            IQueryable<ITSupportTicket> tickets = _context.ITSupportTickets
                .Include(t => t.ReportedByUser)
                .Include(t => t.AssignedToUser)
                .Include(t => t.StatusChangedByUser)
                .OrderByDescending(t => t.CreatedDate);

            // Apply the same role-based filtering as Index
            if (User.IsInRole("Admin") || User.IsInRole("IT Manager") || 
                User.IsInRole("IT Support Supervisor") || User.IsInRole("IT Support"))
            {
                // Admin, IT Manager, and IT Support roles see all tickets
                // IT Support see tickets assigned to them or unassigned
                if (User.IsInRole("IT Support") && !User.IsInRole("IT Support Supervisor") && 
                    !User.IsInRole("Admin") && !User.IsInRole("IT Manager"))
                {
                    tickets = tickets.Where(t => t.AssignedToUserId == currentUser.Id || t.AssignedToUserId == null);
                }
            }
            else
            {
                // Regular users only see their own tickets
                tickets = tickets.Where(t => t.ReportedByUserId == currentUser.Id);
            }

            // Apply filters (same as Index)
            if (!string.IsNullOrEmpty(status))
            {
                tickets = tickets.Where(t => t.Status == status);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                tickets = tickets.Where(t => t.Priority == priority);
            }

            var ticketsList = await tickets.ToListAsync();

            // Generate CSV content (reduced columns for smaller file size)
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Ticket ID,Subject,Status,Priority,Asset Type,Related Asset,Reported By,Assigned To,Created Date,Last Action Date,Resolved Date");

            foreach (var ticket in ticketsList)
            {
                var reportedBy = ticket.ReportedByUser?.FullName ?? ticket.ReportedByUser?.Email ?? "N/A";
                var assignedTo = ticket.AssignedToUser?.FullName ?? ticket.AssignedToUser?.Email ?? "Unassigned";
                var relatedAsset = !string.IsNullOrEmpty(ticket.RelatedAssetName) 
                    ? $"{ticket.AssetType}: {ticket.RelatedAssetName}" 
                    : "N/A";

                csv.AppendLine($"{ticket.Id}," +
                    $"{EscapeCsvField(ticket.Subject)}," +
                    $"{EscapeCsvField(ticket.Status)}," +
                    $"{EscapeCsvField(ticket.Priority)}," +
                    $"{EscapeCsvField(ticket.AssetType)}," +
                    $"{EscapeCsvField(relatedAsset)}," +
                    $"{EscapeCsvField(reportedBy)}," +
                    $"{EscapeCsvField(assignedTo)}," +
                    $"{ticket.CreatedDate:yyyy-MM-dd HH:mm}," +
                    $"{(ticket.LastActionDate.HasValue ? ticket.LastActionDate.Value.ToString("yyyy-MM-dd HH:mm") : "")}," +
                    $"{(ticket.ResolvedDate.HasValue ? ticket.ResolvedDate.Value.ToString("yyyy-MM-dd HH:mm") : "")}");
            }

            var fileName = $"ITSupportTickets_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        public async Task<IActionResult> ExportToPdf(string? status, string? priority)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            IQueryable<ITSupportTicket> tickets = _context.ITSupportTickets
                .Include(t => t.ReportedByUser)
                .Include(t => t.AssignedToUser)
                .OrderByDescending(t => t.CreatedDate);

            // Apply the same role-based filtering as Index
            if (User.IsInRole("Admin") || User.IsInRole("IT Manager") || 
                User.IsInRole("IT Support Supervisor") || User.IsInRole("IT Support"))
            {
                // Admin, IT Manager, and IT Support roles see all tickets
                // IT Support see tickets assigned to them or unassigned
                if (User.IsInRole("IT Support") && !User.IsInRole("IT Support Supervisor") && 
                    !User.IsInRole("Admin") && !User.IsInRole("IT Manager"))
                {
                    tickets = tickets.Where(t => t.AssignedToUserId == currentUser.Id || t.AssignedToUserId == null);
                }
            }
            else
            {
                // Regular users only see their own tickets
                tickets = tickets.Where(t => t.ReportedByUserId == currentUser.Id);
            }

            // Apply filters (same as Index)
            if (!string.IsNullOrEmpty(status))
            {
                tickets = tickets.Where(t => t.Status == status);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                tickets = tickets.Where(t => t.Priority == priority);
            }

            var ticketsList = await tickets.ToListAsync();

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
                        .Text("IT Support Tickets Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);
                            column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(8).FontColor(Colors.Grey.Medium);
                            column.Item().Text($"Total Tickets: {ticketsList.Count}").FontSize(8).FontColor(Colors.Grey.Medium);
                            
                            if (!string.IsNullOrWhiteSpace(status))
                                column.Item().Text($"Status Filter: {status}").FontSize(8).FontColor(Colors.Grey.Medium);
                            if (!string.IsNullOrWhiteSpace(priority))
                                column.Item().Text($"Priority Filter: {priority}").FontSize(8).FontColor(Colors.Grey.Medium);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(0.8f);
                                    columns.RelativeColumn(2.5f);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("ID");
                                    header.Cell().Element(CellStyle).Text("Subject");
                                    header.Cell().Element(CellStyle).Text("Status");
                                    header.Cell().Element(CellStyle).Text("Priority");
                                    header.Cell().Element(CellStyle).Text("Reported By");
                                    header.Cell().Element(CellStyle).Text("Related Asset");
                                    header.Cell().Element(CellStyle).Text("Created Date");
                                });

                                foreach (var ticket in ticketsList)
                                {
                                    var reportedBy = ticket.ReportedByUser?.FullName ?? ticket.ReportedByUser?.Email ?? "N/A";
                                    var relatedAsset = !string.IsNullOrEmpty(ticket.RelatedAssetName) 
                                        ? $"{ticket.AssetType}: {ticket.RelatedAssetName}" 
                                        : "N/A";
                                    
                                    table.Cell().Element(CellStyle).Text($"#{ticket.Id}");
                                    table.Cell().Element(CellStyle).Text(ticket.Subject ?? "—");
                                    table.Cell().Element(CellStyle).Text(ticket.Status ?? "—");
                                    table.Cell().Element(CellStyle).Text(ticket.Priority ?? "—");
                                    table.Cell().Element(CellStyle).Text(reportedBy);
                                    table.Cell().Element(CellStyle).Text(relatedAsset);
                                    table.Cell().Element(CellStyle).Text(ticket.CreatedDate.ToString("yyyy-MM-dd HH:mm"));
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
            var fileName = $"ITSupportTickets_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
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
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,IT Manager")]
        public async Task<IActionResult> BulkDelete(List<int> ids, string? status, string? priority)
        {
            if (ids == null || ids.Count == 0)
            {
                TempData["ErrorMessage"] = "No records selected for deletion.";
                return RedirectToAction(nameof(Index), new { status, priority });
            }

            var currentUserName = User.Identity?.Name ?? "System";
            var deletedCount = 0;
            var failedCount = 0;
            var errors = new List<string>();

            foreach (var id in ids)
            {
                var ticket = await _context.ITSupportTickets.FindAsync(id);
                if (ticket == null)
                {
                    failedCount++;
                    errors.Add($"Ticket with ID {id} not found.");
                    continue;
                }

                // Log the deletion
                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = currentUserName,
                    Action = "Bulk Delete",
                    EntityType = "IT Support Ticket",
                    EntityId = id,
                    Details = $"Bulk deleted ticket: {ticket.Subject} (ID: {ticket.Id})",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    Timestamp = DateTime.Now
                });

                _context.ITSupportTickets.Remove(ticket);
                deletedCount++;
            }

            await _context.SaveChangesAsync();

            if (deletedCount > 0)
            {
                TempData["SuccessMessage"] = $"Successfully deleted {deletedCount} ticket(s).";
            }
            if (failedCount > 0)
            {
                TempData["WarningMessage"] = $"{failedCount} ticket(s) could not be deleted. " + string.Join(" ", errors.Take(5));
            }

            return RedirectToAction(nameof(Index), new { status, priority });
        }

        // GET: ITSupport/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.ITSupportTickets
                .Include(t => t.ReportedByUser)
                .Include(t => t.AssignedToUser)
                .Include(t => t.StatusChangedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            // Check authorization
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            bool canView = User.IsInRole("Admin") || User.IsInRole("IT Manager") || 
                          User.IsInRole("IT Support Supervisor") || 
                          (User.IsInRole("IT Support") && (ticket.AssignedToUserId == currentUser.Id || ticket.AssignedToUserId == null)) ||
                          ticket.ReportedByUserId == currentUser.Id;

            if (!canView)
            {
                return Forbid();
            }

            ViewBag.IsAdminOrITManager = User.IsInRole("Admin") || User.IsInRole("IT Manager");
            ViewBag.IsITSupportSupervisor = User.IsInRole("IT Support Supervisor") || User.IsInRole("Admin") || User.IsInRole("IT Manager");
            ViewBag.IsITSupport = User.IsInRole("IT Support") || User.IsInRole("IT Support Supervisor") || User.IsInRole("Admin") || User.IsInRole("IT Manager");
            ViewBag.CanEdit = ViewBag.IsITSupportSupervisor || ViewBag.IsITSupport;

            return View(ticket);
        }

        // GET: ITSupport/Create
        public async Task<IActionResult> Create(string? assetType, int? assetId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var ticket = new ITSupportTicket
            {
                ReportedByUserId = currentUser.Id
            };

            // If asset is specified, populate related asset info
            if (!string.IsNullOrEmpty(assetType) && assetId.HasValue)
            {
                ticket.AssetType = assetType;
                ticket.RelatedAssetId = assetId.Value;

                // Get asset name
                if (assetType == "Computer")
                {
                    var computer = await _context.Computers.FindAsync(assetId.Value);
                    if (computer != null)
                    {
                        ticket.RelatedAssetName = computer.AssetName;
                    }
                }
                else if (assetType == "Server")
                {
                    var server = await _context.Servers.FindAsync(assetId.Value);
                    if (server != null)
                    {
                        ticket.RelatedAssetName = server.AssetName;
                    }
                }
                else if (assetType == "Application")
                {
                    var application = await _context.Applications.FindAsync(assetId.Value);
                    if (application != null)
                    {
                        ticket.RelatedAssetName = application.AssetName;
                    }
                }
            }

            // Get user's assets for dropdown (only for Admin/IT Manager)
            var isAdminOrITManager = User.IsInRole("Admin") || User.IsInRole("IT Manager");
            ViewBag.IsAdminOrITManager = isAdminOrITManager;
            
            if (isAdminOrITManager)
            {
                ViewBag.UserComputers = await _context.Computers
                    .Where(c => !string.IsNullOrEmpty(c.AssetName))
                    .Select(c => new { c.Id, c.AssetName })
                    .ToListAsync();
                ViewBag.UserServers = await _context.Servers
                    .Where(s => !string.IsNullOrEmpty(s.AssetName))
                    .Select(s => new { s.Id, s.AssetName })
                    .ToListAsync();
                ViewBag.UserApplications = await _context.Applications
                    .Where(a => !string.IsNullOrEmpty(a.AssetName))
                    .Select(a => new { a.Id, a.AssetName })
                    .ToListAsync();
            }
            else
            {
                // For non-Admin/IT Manager, don't show asset dropdowns
                ViewBag.UserComputers = new List<object>();
                ViewBag.UserServers = new List<object>();
                ViewBag.UserApplications = new List<object>();
            }

            return View(ticket);
        }

        // POST: ITSupport/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Subject,Description,Priority,AssetType,RelatedAssetId,RelatedAssetName")] ITSupportTicket ticket)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            // Set ReportedByUserId before validation (it's required but not in the form)
            ticket.ReportedByUserId = currentUser.Id;
            ModelState.Remove("ReportedByUserId"); // Remove validation error since we set it programmatically

            // Validate asset selection is required only for Admin/IT Manager
            var isAdminOrITManager = User.IsInRole("Admin") || User.IsInRole("IT Manager");
            if (isAdminOrITManager)
            {
                if (string.IsNullOrWhiteSpace(ticket.AssetType))
                {
                    ModelState.AddModelError("AssetType", "Please select an asset type for this ticket.");
                }
                
                if (ticket.RelatedAssetId <= 0)
                {
                    ModelState.AddModelError("RelatedAssetId", "Please select an asset for this ticket.");
                }
            }
            else
            {
                // For non-Admin/IT Manager, set asset fields to empty/default
                ticket.AssetType = "";
                ticket.RelatedAssetId = 0;
                ticket.RelatedAssetName = "";
                ModelState.Remove("AssetType");
                ModelState.Remove("RelatedAssetId");
                ModelState.Remove("RelatedAssetName");
            }
            
            // Additional validation: verify the asset exists and get asset name (only for Admin/IT Manager)
            if (isAdminOrITManager && !string.IsNullOrWhiteSpace(ticket.AssetType) && ticket.RelatedAssetId > 0)
            {
                bool assetExists = false;
                if (ticket.AssetType == "Computer")
                {
                    var computer = await _context.Computers.FindAsync(ticket.RelatedAssetId);
                    if (computer != null)
                    {
                        assetExists = true;
                        if (string.IsNullOrEmpty(ticket.RelatedAssetName))
                        {
                            ticket.RelatedAssetName = computer.AssetName;
                        }
                    }
                }
                else if (ticket.AssetType == "Server")
                {
                    var server = await _context.Servers.FindAsync(ticket.RelatedAssetId);
                    if (server != null)
                    {
                        assetExists = true;
                        if (string.IsNullOrEmpty(ticket.RelatedAssetName))
                        {
                            ticket.RelatedAssetName = server.AssetName;
                        }
                    }
                }
                else if (ticket.AssetType == "Application")
                {
                    var application = await _context.Applications.FindAsync(ticket.RelatedAssetId);
                    if (application != null)
                    {
                        assetExists = true;
                        if (string.IsNullOrEmpty(ticket.RelatedAssetName))
                        {
                            ticket.RelatedAssetName = application.AssetName;
                        }
                    }
                }
                
                if (!assetExists)
                {
                    ModelState.AddModelError("RelatedAssetId", "The selected asset does not exist or is not available.");
                }
            }

            if (ModelState.IsValid)
            {
                // ReportedByUserId is already set above
                ticket.Status = "Pending";
                ticket.CreatedDate = DateTime.Now;
                ticket.UpdatedDate = DateTime.Now;
                ticket.LastActionDate = DateTime.Now;

                // Auto-assign to IT Support Supervisor
                var supervisors = await _userManager.GetUsersInRoleAsync("IT Support Supervisor");
                if (supervisors != null && supervisors.Any())
                {
                    // Assign to first active supervisor
                    var activeSupervisor = supervisors.FirstOrDefault(s => s.IsActive);
                    if (activeSupervisor != null)
                    {
                        ticket.AssignedToUserId = activeSupervisor.Id;
                    }
                }

                _context.Add(ticket);
                await _context.SaveChangesAsync();

                // Audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "",
                    Action = "Create",
                    EntityType = "IT Support Ticket",
                    EntityId = ticket.Id,
                    Details = $"Created support ticket: {ticket.Subject}",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                });
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Support ticket created successfully. We will respond soon.";
                return RedirectToAction(nameof(Index));
            }

            // Rebuild ViewBag for error display
            if (!User.IsInRole("Admin") && !User.IsInRole("IT Manager"))
            {
                ViewBag.UserComputers = await _context.Computers
                    .Where(c => (c.AssignedTo.ToLower() == currentUser.FullName!.ToLower() || 
                                c.AssignedTo.ToLower() == currentUser.Email!.ToLower()) &&
                                !string.IsNullOrEmpty(c.AssetName))
                    .Select(c => new { c.Id, c.AssetName })
                    .ToListAsync();
                ViewBag.UserServers = new List<object>();
                ViewBag.UserApplications = new List<object>();
            }
            else
            {
                ViewBag.UserComputers = await _context.Computers
                    .Where(c => !string.IsNullOrEmpty(c.AssetName))
                    .Select(c => new { c.Id, c.AssetName })
                    .ToListAsync();
                ViewBag.UserServers = await _context.Servers
                    .Where(s => !string.IsNullOrEmpty(s.AssetName))
                    .Select(s => new { s.Id, s.AssetName })
                    .ToListAsync();
                ViewBag.UserApplications = await _context.Applications
                    .Where(a => !string.IsNullOrEmpty(a.AssetName))
                    .Select(a => new { a.Id, a.AssetName })
                    .ToListAsync();
            }

            return View(ticket);
        }

        // GET: ITSupport/Edit/5
        [Authorize(Policy = "RequireITSupport")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.ITSupportTickets
                .Include(t => t.ReportedByUser)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            // Check authorization
            bool canEdit = User.IsInRole("Admin") || User.IsInRole("IT Manager") || 
                          User.IsInRole("IT Support Supervisor") ||
                          (User.IsInRole("IT Support") && ticket.AssignedToUserId == currentUser.Id);

            if (!canEdit)
            {
                return Forbid();
            }

            // Get users for assignment dropdown (only for supervisors/admins)
            if (User.IsInRole("IT Support Supervisor") || User.IsInRole("Admin") || User.IsInRole("IT Manager"))
            {
                var allUsers = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
                var supportUsers = new List<dynamic>();
                
                foreach (var user in allUsers)
                {
                    var isITSupport = await _userManager.IsInRoleAsync(user, "IT Support");
                    var isITSupportSupervisor = await _userManager.IsInRoleAsync(user, "IT Support Supervisor");
                    
                    if (isITSupport || isITSupportSupervisor)
                    {
                        supportUsers.Add(new { user.Id, user.FullName, user.Email });
                    }
                }
                
                ViewBag.Users = supportUsers;
            }
            else
            {
                ViewBag.Users = new List<dynamic>();
            }

            ViewBag.IsITSupportSupervisor = User.IsInRole("IT Support Supervisor") || User.IsInRole("Admin") || User.IsInRole("IT Manager");
            ViewBag.IsITSupport = User.IsInRole("IT Support");
            ViewBag.CurrentStatus = ticket.Status;

            return View(ticket);
        }

        // POST: ITSupport/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireITSupport")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Subject,Description,Status,Priority,AssignedToUserId,AdminResponse,ResolutionNotes,TechnicianNotes,ReplacementRequested,ReplacementReason")] ITSupportTicket ticket)
        {
            if (id != ticket.Id)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            // Check authorization
            var existingTicket = await _context.ITSupportTickets
                .Include(t => t.ReportedByUser)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (existingTicket == null)
            {
                return NotFound();
            }

            bool canEdit = User.IsInRole("Admin") || User.IsInRole("IT Manager") || 
                          User.IsInRole("IT Support Supervisor") ||
                          (User.IsInRole("IT Support") && existingTicket.AssignedToUserId == currentUser.Id);

            if (!canEdit)
            {
                return Forbid();
            }

            // Remove validation errors for fields not in the form (they're read-only)
            ModelState.Remove("AssetType");
            ModelState.Remove("RelatedAssetId");
            ModelState.Remove("RelatedAssetName");
            ModelState.Remove("ReportedByUserId");

            // Validate status transitions for IT Support technicians BEFORE ModelState check
            if (User.IsInRole("IT Support") && !User.IsInRole("IT Support Supervisor") && 
                !User.IsInRole("Admin") && !User.IsInRole("IT Manager"))
            {
                // Technicians cannot skip from Pending to Resolved
                if (existingTicket.Status == "Pending" && ticket.Status == "Resolved")
                {
                    ModelState.AddModelError("Status", "Cannot change status from Pending to Resolved. Please change to In Progress first.");
                }
                
                // Require ResolutionNotes when resolving
                if (ticket.Status == "Resolved" && string.IsNullOrWhiteSpace(ticket.ResolutionNotes))
                {
                    ModelState.AddModelError("ResolutionNotes", "Resolution Notes are required when resolving a ticket.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingTicket.Subject = ticket.Subject;
                    existingTicket.Description = ticket.Description;
                    
                    // Track status changes with timestamp
                    if (existingTicket.Status != ticket.Status)
                    {
                        existingTicket.Status = ticket.Status;
                        existingTicket.StatusChangedDate = DateTime.Now;
                        existingTicket.StatusChangedByUserId = currentUser.Id;
                    }
                    
                    existingTicket.Priority = ticket.Priority;
                    existingTicket.UpdatedDate = DateTime.Now;
                    existingTicket.LastActionDate = DateTime.Now;
                    
                    // Only supervisors/admins can assign tickets
                    if (User.IsInRole("IT Support Supervisor") || User.IsInRole("Admin") || User.IsInRole("IT Manager"))
                    {
                        existingTicket.AssignedToUserId = ticket.AssignedToUserId;
                    }
                    
                    // Only admins/IT managers can add admin response
                    if (User.IsInRole("Admin") || User.IsInRole("IT Manager"))
                    {
                        existingTicket.AdminResponse = ticket.AdminResponse;
                    }
                    
                    // Only IT Support technicians can add technician notes and resolution notes
                    if (User.IsInRole("IT Support"))
                    {
                        // Track technician notes changes with timestamp
                        if (existingTicket.TechnicianNotes != ticket.TechnicianNotes && !string.IsNullOrWhiteSpace(ticket.TechnicianNotes))
                        {
                            existingTicket.TechnicianNotes = ticket.TechnicianNotes;
                            existingTicket.TechnicianNotesDate = DateTime.Now;
                        }
                        else if (!string.IsNullOrWhiteSpace(ticket.TechnicianNotes))
                        {
                            existingTicket.TechnicianNotes = ticket.TechnicianNotes;
                        }
                        
                        // Only set resolution notes if status is Resolved
                        if (ticket.Status == "Resolved")
                        {
                            existingTicket.ResolutionNotes = ticket.ResolutionNotes;
                        }
                    }
                    
                    // Handle replacement request (only for IT Support)
                    if (User.IsInRole("IT Support"))
                    {
                        existingTicket.ReplacementRequested = ticket.ReplacementRequested;
                        existingTicket.ReplacementReason = ticket.ReplacementReason;
                    }

                    // Set resolved date if status changed to Resolved or Closed
                    if ((ticket.Status == "Resolved" || ticket.Status == "Closed") && existingTicket.ResolvedDate == null)
                    {
                        existingTicket.ResolvedDate = DateTime.Now;
                    }
                    else if (ticket.Status != "Resolved" && ticket.Status != "Closed")
                    {
                        existingTicket.ResolvedDate = null;
                    }

                    await _context.SaveChangesAsync();

                    // Audit log
                    _context.AuditLogs.Add(new AuditLog
                    {
                        UserName = User.Identity?.Name ?? "",
                        Action = "Update",
                        EntityType = "IT Support Ticket",
                        EntityId = ticket.Id,
                        Details = $"Updated support ticket: {ticket.Subject}",
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                    });
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Support ticket updated successfully.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(ticket.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Rebuild ViewBag for error display
            if (User.IsInRole("IT Support Supervisor") || User.IsInRole("Admin") || User.IsInRole("IT Manager"))
            {
                var allUsers = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
                var supportUsers = new List<dynamic>();
                foreach (var user in allUsers)
                {
                    var isITSupport = await _userManager.IsInRoleAsync(user, "IT Support");
                    var isITSupportSupervisor = await _userManager.IsInRoleAsync(user, "IT Support Supervisor");
                    if (isITSupport || isITSupportSupervisor)
                    {
                        supportUsers.Add(new { user.Id, user.FullName, user.Email });
                    }
                }
                ViewBag.Users = supportUsers;
            }
            else
            {
                ViewBag.Users = new List<dynamic>();
            }
            ViewBag.IsITSupportSupervisor = User.IsInRole("IT Support Supervisor") || User.IsInRole("Admin") || User.IsInRole("IT Manager");
            ViewBag.IsITSupport = User.IsInRole("IT Support");
            ViewBag.CurrentStatus = existingTicket.Status;

            // Return the existing ticket but update form-bound fields for display
            existingTicket.Subject = ticket.Subject;
            existingTicket.Description = ticket.Description;
            existingTicket.Status = ticket.Status;
            existingTicket.Priority = ticket.Priority;
            if (User.IsInRole("IT Support Supervisor") || User.IsInRole("Admin") || User.IsInRole("IT Manager"))
            {
                existingTicket.AssignedToUserId = ticket.AssignedToUserId;
            }
            if (User.IsInRole("Admin") || User.IsInRole("IT Manager"))
            {
                existingTicket.AdminResponse = ticket.AdminResponse;
            }
            if (User.IsInRole("IT Support"))
            {
                existingTicket.TechnicianNotes = ticket.TechnicianNotes;
                existingTicket.ResolutionNotes = ticket.ResolutionNotes;
                existingTicket.ReplacementRequested = ticket.ReplacementRequested;
                existingTicket.ReplacementReason = ticket.ReplacementReason;
            }

            return View(existingTicket);
        }

        // POST: ITSupport/RequestReplacement/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireITSupport")]
        public async Task<IActionResult> RequestReplacement(int id, string replacementReason)
        {
            var ticket = await _context.ITSupportTickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            // Only assigned technicians can request replacement
            if (User.IsInRole("IT Support") && !User.IsInRole("IT Support Supervisor") && 
                !User.IsInRole("Admin") && !User.IsInRole("IT Manager"))
            {
                if (ticket.AssignedToUserId != currentUser.Id)
                {
                    return Forbid();
                }
            }

            ticket.ReplacementRequested = true;
            ticket.ReplacementReason = replacementReason;
            ticket.UpdatedDate = DateTime.Now;
            ticket.LastActionDate = DateTime.Now;

            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserName = User.Identity?.Name ?? "",
                Action = "Request Replacement",
                EntityType = "IT Support Ticket",
                EntityId = ticket.Id,
                Details = $"Requested replacement for asset: {ticket.RelatedAssetName}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Replacement request submitted to Admin for approval.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: ITSupport/ApproveReplacement/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,IT Manager")]
        public async Task<IActionResult> ApproveReplacement(int id, bool approved, string? adminResponse)
        {
            var ticket = await _context.ITSupportTickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            ticket.ReplacementApproved = approved;
            ticket.ReplacementAdminResponse = adminResponse;
            ticket.UpdatedDate = DateTime.Now;
            ticket.LastActionDate = DateTime.Now;

            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserName = User.Identity?.Name ?? "",
                Action = approved ? "Approve Replacement" : "Reject Replacement",
                EntityType = "IT Support Ticket",
                EntityId = ticket.Id,
                Details = $"{(approved ? "Approved" : "Rejected")} replacement request for asset: {ticket.RelatedAssetName}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Replacement request {(approved ? "approved" : "rejected")}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: ITSupport/FollowUp/5
        public async Task<IActionResult> FollowUp(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.ITSupportTickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            // Only the ticket owner can add follow-up
            if (ticket.ReportedByUserId != currentUser.Id)
            {
                return Forbid();
            }

            // Cannot add follow-up to closed tickets
            if (ticket.Status == "Closed")
            {
                TempData["ErrorMessage"] = "Cannot add follow-up to closed tickets.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(ticket);
        }

        // POST: ITSupport/FollowUp/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FollowUp(int id, string userFollowUp)
        {
            var ticket = await _context.ITSupportTickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            // Only the ticket owner can add follow-up
            if (ticket.ReportedByUserId != currentUser.Id)
            {
                return Forbid();
            }

            // Cannot add follow-up to closed tickets
            if (ticket.Status == "Closed")
            {
                TempData["ErrorMessage"] = "Cannot add follow-up to closed tickets.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(userFollowUp))
            {
                ModelState.AddModelError("UserFollowUp", "Please enter your follow-up message.");
                return View(ticket);
            }

            ticket.UserFollowUp = userFollowUp;
            ticket.FollowUpDate = DateTime.Now;
            ticket.UpdatedDate = DateTime.Now;
            ticket.LastActionDate = DateTime.Now;

            await _context.SaveChangesAsync();

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                UserName = User.Identity?.Name ?? "",
                Action = "Follow Up",
                EntityType = "IT Support Ticket",
                EntityId = ticket.Id,
                Details = $"User added follow-up to ticket: {ticket.Subject}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Follow-up added successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: ITSupport/Close/5
        [Authorize(Roles = "Admin,IT Manager")]
        public async Task<IActionResult> Close(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.ITSupportTickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            ticket.Status = "Closed";
            ticket.UpdatedDate = DateTime.Now;
            ticket.LastActionDate = DateTime.Now;
            if (ticket.ResolvedDate == null)
            {
                ticket.ResolvedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserName = User.Identity?.Name ?? "",
                Action = "Close",
                EntityType = "IT Support Ticket",
                EntityId = ticket.Id,
                Details = $"Closed support ticket: {ticket.Subject}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Support ticket closed successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CheckAndNotifyExpiredTickets()
        {
            // Get tickets that are not resolved/closed and are over 3 days old
            var expiredTickets = await _context.ITSupportTickets
                .Include(t => t.ReportedByUser)
                .Include(t => t.AssignedToUser)
                .Where(t => (t.Status == "Pending" || t.Status == "In Progress") && 
                            (DateTime.Now - t.CreatedDate).TotalDays > 3)
                .ToListAsync();

            if (expiredTickets.Any())
            {
                // Get all IT Support Supervisors
                var supervisors = await _userManager.GetUsersInRoleAsync("IT Support Supervisor");
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                var itManagers = await _userManager.GetUsersInRoleAsync("IT Manager");

                // Combine all supervisors, admins, and IT managers
                var allSupervisors = new List<ApplicationUser>();
                if (supervisors != null) allSupervisors.AddRange(supervisors);
                if (admins != null) allSupervisors.AddRange(admins);
                if (itManagers != null) allSupervisors.AddRange(itManagers);

                // Set notification flag (you can extend this to send emails)
                foreach (var ticket in expiredTickets)
                {
                    // You can add email notification here or use TempData to show alerts
                    // For now, we'll just track it in the system
                }

                // Store expired ticket count in ViewBag for display
                ViewBag.ExpiredTicketsCount = expiredTickets.Count;
                ViewBag.ExpiredTickets = expiredTickets;
            }
        }

        private bool TicketExists(int id)
        {
            return _context.ITSupportTickets.Any(e => e.Id == id);
        }
    }
}
