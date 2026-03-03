using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT_Asset_Management_System.Models;
using IT_Asset_Management_System.Data;

namespace IT_Asset_Management_System.Controllers
{
    [Authorize(Policy = "RequireAdmin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchTerm)
        {
            var query = _userManager.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(u => 
                    (u.Email != null && u.Email.Contains(searchTerm)) ||
                    (u.FullName != null && u.FullName.Contains(searchTerm)) ||
                    (u.Department != null && u.Department.Contains(searchTerm)) ||
                    (u.UserName != null && u.UserName.Contains(searchTerm))
                );
            }

            var users = await query.OrderBy(u => u.FullName).ToListAsync();
            var userRoles = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "No Role";
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentUserId = _userManager.GetUserId(User);
            return View(users);
        }

        public async Task<IActionResult> ExportToCsv(string? searchTerm)
        {
            var query = _userManager.Users.AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(u => 
                    (u.Email != null && u.Email.Contains(searchTerm)) ||
                    (u.FullName != null && u.FullName.Contains(searchTerm)) ||
                    (u.Department != null && u.Department.Contains(searchTerm)) ||
                    (u.UserName != null && u.UserName.Contains(searchTerm))
                );
            }

            var users = await query.OrderBy(u => u.FullName).ToListAsync();
            var userRoles = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "No Role";
            }

            // Generate CSV content
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Full Name,Email,Department,Role,Status,Created Date");

            foreach (var user in users)
            {
                var role = userRoles.ContainsKey(user.Id) ? userRoles[user.Id] : "No Role";
                var status = user.IsActive == true ? "Active" : "Inactive";
                var createdDate = user.CreatedDate.ToString("yyyy-MM-dd");
                
                csv.AppendLine($"{EscapeCsvField(user.FullName)},{EscapeCsvField(user.Email)},{EscapeCsvField(user.Department)},{EscapeCsvField(role)},{EscapeCsvField(status)},{createdDate}");
            }

            var fileName = $"Users_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // If field contains comma, quote, or newline, wrap it in quotes and escape quotes
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault() ?? "";

            var viewModel = new IT_Asset_Management_System.ViewModels.RegisterViewModel
            {
                Email = user.Email ?? "",
                FullName = user.FullName ?? "",
                Department = user.Department ?? "",
                Role = currentRole
            };

            ViewBag.UserId = user.Id;
            ViewBag.IsEdit = true;
            // Password fields should be disabled when Admin/Manager edits a user
            // Password can only be changed through Password Management or My Profile
            ViewBag.DisablePasswordFields = true;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, IT_Asset_Management_System.ViewModels.RegisterViewModel model)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Password cannot be changed from this Edit action.
            // It must be changed via Password Management (admin) or My Profile -> Change Password.
            ModelState.Remove(nameof(model.Password));
            ModelState.Remove(nameof(model.ConfirmPassword));

            // Validate other fields
            if (!ModelState.IsValid)
            {
                ViewBag.UserId = id;
                ViewBag.IsEdit = true;
                ViewBag.DisablePasswordFields = true;
                return View(model);
            }

            // Update user properties
                user.Email = model.Email;
                user.UserName = model.Email;
                user.FullName = model.FullName;
                user.Department = model.Department;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    ViewBag.UserId = id;
                    ViewBag.IsEdit = true;
                    ViewBag.DisablePasswordFields = true;
                    return View(model);
                }

                // Update role if changed
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }
                if (!string.IsNullOrEmpty(model.Role))
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                // Log the update
                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "",
                    Action = "Update User",
                    EntityType = "User",
                    EntityId = null, // User IDs are strings, not integers
                    Details = $"Updated user: {user.Email} (ID: {user.Id})",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent deleting yourself
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault() ?? "No Role";

            // Check if user is linked to any assets
            var userFullName = user.FullName ?? user.UserName ?? user.Email;
            var userEmail = user.Email;
            
            // Check Computers (exclude "Unassigned")
            var computersCount = await _context.Computers
                .Where(c => (c.AssignedTo == userFullName || c.AssignedTo == userEmail) 
                    && c.AssignedTo.ToLower() != "unassigned")
                .CountAsync();
            
            // Check Servers (ProjectManagerName)
            var serversCount = await _context.Servers
                .Where(s => s.ProjectManagerName == userFullName || s.ProjectManagerName == userEmail)
                .CountAsync();
            
            // Check Applications (ApplicationOwner)
            var applicationsCount = await _context.Applications
                .Where(a => a.ApplicationOwner == userFullName || a.ApplicationOwner == userEmail)
                .CountAsync();
            
            // Check IT Support Tickets (ReportedByUserId, AssignedToUserId, StatusChangedByUserId)
            var itSupportTicketsCount = await _context.ITSupportTickets
                .Where(t => t.ReportedByUserId == user.Id || 
                           t.AssignedToUserId == user.Id || 
                           t.StatusChangedByUserId == user.Id)
                .CountAsync();

            ViewBag.UserRole = currentRole;
            ViewBag.ComputersCount = computersCount;
            ViewBag.ServersCount = serversCount;
            ViewBag.ApplicationsCount = applicationsCount;
            ViewBag.ITSupportTicketsCount = itSupportTicketsCount;
            ViewBag.HasDependencies = computersCount > 0 || serversCount > 0 || applicationsCount > 0 || itSupportTicketsCount > 0;

            return View(user);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent deleting yourself
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            // Check if user is linked to any assets
            var userFullName = user.FullName ?? user.UserName ?? user.Email;
            var userEmail = user.Email;
            
            // Check Computers (exclude "Unassigned")
            var computersCount = await _context.Computers
                .Where(c => (c.AssignedTo == userFullName || c.AssignedTo == userEmail) 
                    && c.AssignedTo.ToLower() != "unassigned")
                .CountAsync();
            
            // Check Servers (ProjectManagerName)
            var serversCount = await _context.Servers
                .Where(s => s.ProjectManagerName == userFullName || s.ProjectManagerName == userEmail)
                .CountAsync();
            
            // Check Applications (ApplicationOwner)
            var applicationsCount = await _context.Applications
                .Where(a => a.ApplicationOwner == userFullName || a.ApplicationOwner == userEmail)
                .CountAsync();
            
            // Check IT Support Tickets (ReportedByUserId, AssignedToUserId, StatusChangedByUserId)
            var itSupportTicketsCount = await _context.ITSupportTickets
                .Where(t => t.ReportedByUserId == user.Id || 
                           t.AssignedToUserId == user.Id || 
                           t.StatusChangedByUserId == user.Id)
                .CountAsync();

            if (computersCount > 0 || serversCount > 0 || applicationsCount > 0 || itSupportTicketsCount > 0)
            {
                var dependencies = new List<string>();
                if (computersCount > 0) dependencies.Add($"{computersCount} computer(s)");
                if (serversCount > 0) dependencies.Add($"{serversCount} server(s)");
                if (applicationsCount > 0) dependencies.Add($"{applicationsCount} application(s)");
                if (itSupportTicketsCount > 0) dependencies.Add($"{itSupportTicketsCount} IT Support ticket(s)");
                
                TempData["ErrorMessage"] = $"Cannot delete user '{userFullName}'. User is linked to: {string.Join(", ", dependencies)}. Please reassign or remove these dependencies before deleting the user.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            // Log the deletion before deleting
            _context.AuditLogs.Add(new AuditLog
            {
                UserName = User.Identity?.Name ?? "",
                Action = "Delete User",
                EntityType = "User",
                EntityId = null,
                Details = $"Deleted user: {user.Email} (ID: {user.Id})",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                Timestamp = DateTime.Now
            });

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error deleting user: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> BulkDelete(List<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                TempData["ErrorMessage"] = "No records selected for deletion.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = _userManager.GetUserId(User);
            var deletedCount = 0;
            var failedCount = 0;
            var errors = new List<string>();

            foreach (var id in ids)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    failedCount++;
                    errors.Add($"User with ID {id} not found.");
                    continue;
                }

                // Prevent deleting yourself
                if (user.Id == currentUserId)
                {
                    failedCount++;
                    errors.Add("You cannot delete your own account.");
                    continue;
                }

                var userFullName = user.FullName ?? user.UserName ?? user.Email;
                var userEmail = user.Email;

                // Check dependencies
                var computersCount = await _context.Computers
                    .Where(c => (c.AssignedTo == userFullName || c.AssignedTo == userEmail) 
                        && c.AssignedTo.ToLower() != "unassigned")
                    .CountAsync();
                
                var serversCount = await _context.Servers
                    .Where(s => s.ProjectManagerName == userFullName || s.ProjectManagerName == userEmail)
                    .CountAsync();
                
                var applicationsCount = await _context.Applications
                    .Where(a => a.ApplicationOwner == userFullName || a.ApplicationOwner == userEmail)
                    .CountAsync();
                
                var itSupportTicketsCount = await _context.ITSupportTickets
                    .Where(t => t.ReportedByUserId == id || t.AssignedToUserId == id || t.StatusChangedByUserId == id)
                    .CountAsync();

                if (computersCount > 0 || serversCount > 0 || applicationsCount > 0 || itSupportTicketsCount > 0)
                {
                    var dependencies = new List<string>();
                    if (computersCount > 0) dependencies.Add($"{computersCount} computer(s)");
                    if (serversCount > 0) dependencies.Add($"{serversCount} server(s)");
                    if (applicationsCount > 0) dependencies.Add($"{applicationsCount} application(s)");
                    if (itSupportTicketsCount > 0) dependencies.Add($"{itSupportTicketsCount} IT Support ticket(s)");
                    
                    failedCount++;
                    errors.Add($"User '{userFullName}' is linked to: {string.Join(", ", dependencies)}.");
                    continue;
                }

                // Log the deletion
                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "",
                    Action = "Bulk Delete User",
                    EntityType = "User",
                    EntityId = null,
                    Details = $"Bulk deleted user: {user.Email} (ID: {user.Id})",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    Timestamp = DateTime.Now
                });

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    deletedCount++;
                }
                else
                {
                    failedCount++;
                    errors.Add($"Failed to delete user '{userFullName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }

            await _context.SaveChangesAsync();

            if (deletedCount > 0)
            {
                TempData["SuccessMessage"] = $"Successfully deleted {deletedCount} user(s).";
            }
            if (failedCount > 0)
            {
                TempData["WarningMessage"] = $"{failedCount} user(s) could not be deleted. " + string.Join(" ", errors.Take(5));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> PasswordManagement(string? searchTerm)
        {
            var query = _userManager.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(u => 
                    (u.Email != null && u.Email.Contains(searchTerm)) ||
                    (u.FullName != null && u.FullName.Contains(searchTerm)) ||
                    (u.Department != null && u.Department.Contains(searchTerm)) ||
                    (u.UserName != null && u.UserName.Contains(searchTerm))
                );
            }

            var users = await query.OrderBy(u => u.FullName).ToListAsync();
            var userRoles = new Dictionary<string, string>();
            var passwordStatus = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "No Role";
                
                // Check password expiration status
                if (user.PasswordChangedDate.HasValue)
                {
                    var daysSinceChange = (DateTime.Now - user.PasswordChangedDate.Value).TotalDays;
                    if (daysSinceChange >= 30)
                    {
                        passwordStatus[user.Id] = "Expired";
                    }
                    else if (daysSinceChange >= 25)
                    {
                        passwordStatus[user.Id] = "Expiring Soon";
                    }
                    else
                    {
                        passwordStatus[user.Id] = "Valid";
                    }
                }
                else
                {
                    passwordStatus[user.Id] = "Never Changed";
                }
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.PasswordStatus = passwordStatus;
            ViewBag.SearchTerm = searchTerm;
            return View(users);
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId, string newPassword)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["ErrorMessage"] = "User ID and new password are required.";
                return RedirectToAction(nameof(PasswordManagement));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(PasswordManagement));
            }

            // Validate password meets requirements
            if (newPassword.Length < 8 || newPassword.Length > 30)
            {
                TempData["ErrorMessage"] = "Password must be between 8 and 30 characters.";
                return RedirectToAction(nameof(PasswordManagement));
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(newPassword, @"\d"))
            {
                TempData["ErrorMessage"] = "Password must include at least one digit.";
                return RedirectToAction(nameof(PasswordManagement));
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(newPassword, @"[~!@#$^&*?><]"))
            {
                TempData["ErrorMessage"] = "Password must include at least one special character [~!@#$^&*?><].";
                return RedirectToAction(nameof(PasswordManagement));
            }

            // Reset password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                // Update password changed date
                user.PasswordChangedDate = DateTime.Now;
                user.MustChangePassword = false;
                await _userManager.UpdateAsync(user);

                // Log the password reset
                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "",
                    Action = "Reset User Password",
                    EntityType = "User",
                    EntityId = null,
                    Details = $"Admin reset password for user: {user.Email}",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();

                var userFullName = user.FullName ?? user.UserName ?? user.Email ?? "User";
                TempData["SuccessMessage"] = $"Password has been reset successfully for {userFullName}.";
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Error resetting password: {errors}";
            }

            return RedirectToAction(nameof(PasswordManagement));
        }

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Full Name,Email,Department,Role,IsActive");
            // Use generic sample data (no real names)
            csv.AppendLine("Sample User One,sample.user1@example.com,IT,Employee,true");
            csv.AppendLine("Sample User Two,sample.user2@example.com,HR,Employee,true");

            var fileName = "UserImportTemplate.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        [HttpGet]
        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile csvFile)
        {
            var results = new List<string>();
            var successCount = 0;
            var errorCount = 0;

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

            // Default password for all imported users
            const string defaultPassword = "TempPass123!@#";

            try
            {
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

                    // Process data lines
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        lineNumber++;
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var fields = ParseCsvLine(line);
                        if (fields.Count < 2)
                        {
                            results.Add($"Line {lineNumber}: Insufficient columns. Expected at least 2 (Full Name, Email).");
                            errorCount++;
                            continue;
                        }

                        var fullName = fields[0]?.Trim() ?? "";
                        var email = fields.Count > 1 ? fields[1]?.Trim() ?? "" : "";
                        var department = fields.Count > 2 ? fields[2]?.Trim() ?? "" : "";
                        var role = fields.Count > 3 ? (fields[3]?.Trim() ?? "Employee") : "Employee";
                        var isActive = true;
                        if (fields.Count > 4 && bool.TryParse(fields[4]?.Trim(), out var active))
                        {
                            isActive = active;
                        }

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(fullName))
                        {
                            results.Add($"Line {lineNumber}: Full Name is required.");
                            errorCount++;
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                        {
                            results.Add($"Line {lineNumber}: Valid Email is required.");
                            errorCount++;
                            continue;
                        }

                        // Check if user already exists
                        var existingUser = await _userManager.FindByEmailAsync(email);
                        if (existingUser != null)
                        {
                            results.Add($"Line {lineNumber}: User with email '{email}' already exists. Skipped.");
                            errorCount++;
                            continue;
                        }

                        // Create user with default password
                        var user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            FullName = fullName,
                            Department = department,
                            IsActive = isActive,
                            CreatedDate = DateTime.Now,
                            EmailConfirmed = true,
                            // Force password change on first login
                            MustChangePassword = true,
                            PasswordChangedDate = null // Null means password has never been changed
                        };

                        var createResult = await _userManager.CreateAsync(user, defaultPassword);
                        if (createResult.Succeeded)
                        {
                            // Assign role
                            if (!string.IsNullOrWhiteSpace(role))
                            {
                                var roleExists = await _roleManager.RoleExistsAsync(role);
                                if (roleExists)
                                {
                                    await _userManager.AddToRoleAsync(user, role);
                                }
                                else
                                {
                                    results.Add($"Line {lineNumber}: Role '{role}' does not exist. User created without role.");
                                }
                            }

                            // Log the creation
                            _context.AuditLogs.Add(new AuditLog
                            {
                                UserName = User.Identity?.Name ?? "",
                                Action = "Import User",
                                EntityType = "User",
                                EntityId = null,
                                Details = $"Imported user: {email} (Default password: {defaultPassword})",
                                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                                Timestamp = DateTime.Now
                            });

                            results.Add($"Line {lineNumber}: Successfully imported user '{fullName}' ({email}). Default password: {defaultPassword}");
                            successCount++;
                        }
                        else
                        {
                            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                            results.Add($"Line {lineNumber}: Failed to create user '{email}': {errors}");
                            errorCount++;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                ViewBag.Results = results;
                ViewBag.SuccessCount = successCount;
                ViewBag.ErrorCount = errorCount;
                ViewBag.TotalCount = successCount + errorCount;
                ViewBag.DefaultPassword = defaultPassword;

                if (successCount > 0)
                {
                    TempData["SuccessMessage"] = $"Successfully imported {successCount} user(s). All users have default password: {defaultPassword} and must change it on first login.";
                }
                if (errorCount > 0)
                {
                    TempData["WarningMessage"] = $"{errorCount} user(s) failed to import. Check details below.";
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
    }
}