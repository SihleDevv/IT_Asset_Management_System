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

            // Remove password validation errors if password is not provided (optional during edit)
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.Remove(nameof(model.Password));
                ModelState.Remove(nameof(model.ConfirmPassword));
            }

            // Validate other fields
            if (!ModelState.IsValid)
            {
                ViewBag.UserId = id;
                ViewBag.IsEdit = true;
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

                // Update password if provided
                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                    if (!passwordResult.Succeeded)
                    {
                        foreach (var error in passwordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        ViewBag.UserId = id;
                        ViewBag.IsEdit = true;
                        return View(model);
                    }
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

            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault() ?? "No Role";

            // Check if user is linked to any assets
            var userFullName = user.FullName ?? user.UserName ?? user.Email;
            var userEmail = user.Email;
            
            // Check Computers (exclude "Unassigned")
            var computersCount = await _context.Computers
                .Where(c => (c.AssignedTo == userFullName || c.AssignedTo == userEmail) 
                    && !c.AssignedTo.Equals("Unassigned", StringComparison.OrdinalIgnoreCase))
                .CountAsync();
            
            // Check Servers (ProjectManagerName)
            var serversCount = await _context.Servers
                .Where(s => s.ProjectManagerName == userFullName || s.ProjectManagerName == userEmail)
                .CountAsync();
            
            // Check Applications (ApplicationOwner)
            var applicationsCount = await _context.Applications
                .Where(a => a.ApplicationOwner == userFullName || a.ApplicationOwner == userEmail)
                .CountAsync();

            ViewBag.UserRole = currentRole;
            ViewBag.ComputersCount = computersCount;
            ViewBag.ServersCount = serversCount;
            ViewBag.ApplicationsCount = applicationsCount;
            ViewBag.HasDependencies = computersCount > 0 || serversCount > 0 || applicationsCount > 0;

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
                    && !c.AssignedTo.Equals("Unassigned", StringComparison.OrdinalIgnoreCase))
                .CountAsync();
            
            // Check Servers (ProjectManagerName)
            var serversCount = await _context.Servers
                .Where(s => s.ProjectManagerName == userFullName || s.ProjectManagerName == userEmail)
                .CountAsync();
            
            // Check Applications (ApplicationOwner)
            var applicationsCount = await _context.Applications
                .Where(a => a.ApplicationOwner == userFullName || a.ApplicationOwner == userEmail)
                .CountAsync();

            if (computersCount > 0 || serversCount > 0 || applicationsCount > 0)
            {
                var dependencies = new List<string>();
                if (computersCount > 0) dependencies.Add($"{computersCount} computer(s)");
                if (serversCount > 0) dependencies.Add($"{serversCount} server(s)");
                if (applicationsCount > 0) dependencies.Add($"{applicationsCount} application(s)");
                
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
    }
}