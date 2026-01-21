using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IT_Asset_Management_System.Models;
using IT_Asset_Management_System.ViewModels;
using IT_Asset_Management_System.Data;

namespace IT_Asset_Management_System.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // If user is already logged in, redirect to home
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var loginResult = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (loginResult.Succeeded)
                {
                    var currentUser = await _userManager.FindByEmailAsync(model.Email);
                    if (currentUser != null)
                    {
                        // Check if password has expired (30 days)
                        bool passwordExpired = false;
                        if (currentUser.PasswordChangedDate.HasValue)
                        {
                            var daysSinceChange = (DateTime.Now - currentUser.PasswordChangedDate.Value).TotalDays;
                            if (daysSinceChange >= 30)
                            {
                                passwordExpired = true;
                                currentUser.MustChangePassword = true;
                                await _userManager.UpdateAsync(currentUser);
                            }
                        }
                        else
                        {
                            // If PasswordChangedDate is null, set it to now and mark as expired
                            currentUser.PasswordChangedDate = DateTime.Now;
                            currentUser.MustChangePassword = true;
                            await _userManager.UpdateAsync(currentUser);
                            passwordExpired = true;
                        }

                        if (passwordExpired || currentUser.MustChangePassword)
                        {
                            // Store user ID in session for password change
                            HttpContext.Session.SetString("MustChangePasswordUserId", currentUser.Id);
                            TempData["InfoMessage"] = "Your password has expired. Please change your password.";
                            return RedirectToAction("ChangeExpiredPassword", "Account");
                        }

                        _context.AuditLogs.Add(new AuditLog
                        {
                            UserName = currentUser.UserName ?? "",
                            Action = "Login",
                            EntityType = "Account",
                            Details = "User logged in successfully",
                            IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                        });
                        await _context.SaveChangesAsync();
                    }

                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpGet]
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Check if user is IT Manager trying to create users
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                var roles = await _userManager.GetRolesAsync(currentUser);
                if (roles.Contains("IT Manager") && !roles.Contains("Admin"))
                {
                    TempData["ErrorMessage"] = "IT Managers cannot create new users. Only Admins can create users.";
                    return RedirectToAction("Index", "Users");
                }
            }

            if (ModelState.IsValid)
            {
                var newUser = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    Department = model.Department,
                    PasswordChangedDate = DateTime.Now,
                    MustChangePassword = false
                };

                var createResult = await _userManager.CreateAsync(newUser, model.Password);

                if (createResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(newUser, model.Role);

                    _context.AuditLogs.Add(new AuditLog
                    {
                        UserName = User.Identity?.Name ?? "",
                        Action = "Create User",
                        EntityType = "Account",
                        Details = $"Created new user: {model.Email} with role: {model.Role}",
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                    });
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"User '{model.Email}' has been created successfully.";
                    return RedirectToAction("Index", "Users");
                }

                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var currentUserName = User.Identity?.Name ?? "";
            await _signInManager.SignOutAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserName = currentUserName,
                Action = "Logout",
                EntityType = "Account",
                Details = "User logged out",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
            });
            await _context.SaveChangesAsync();

            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault() ?? "";

            var viewModel = new RegisterViewModel
            {
                Email = user.Email ?? "",
                FullName = user.FullName ?? "",
                Department = user.Department ?? "",
                Role = currentRole
            };

            var isAdminOrITManager = roles.Any(r => r == "Admin" || r == "IT Manager");
            
            ViewBag.IsProfile = true;
            ViewBag.IsAdminOrITManager = isAdminOrITManager;
            ViewBag.IsReadOnlyOrEmployee = roles.Any(r => r == "Read Only" || r == "Employee");
            ViewBag.PasswordChangedDate = user.PasswordChangedDate;
            return View(viewModel);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault() ?? "";

            var viewModel = new RegisterViewModel
            {
                Email = user.Email ?? "",
                FullName = user.FullName ?? "",
                Department = user.Department ?? "",
                Role = currentRole
            };

            var isAdminOrITManager = roles.Any(r => r == "Admin" || r == "IT Manager");
            
            ViewBag.IsProfile = true;
            ViewBag.IsAdminOrITManager = isAdminOrITManager;
            ViewBag.IsReadOnlyOrEmployee = roles.Any(r => r == "Read Only" || r == "Employee");
            return View("EditProfile", viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(RegisterViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Remove password validation errors if password is not provided (optional during profile update)
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.Remove(nameof(model.Password));
                ModelState.Remove(nameof(model.ConfirmPassword));
            }

            // Check user role permissions
            var roles = await _userManager.GetRolesAsync(user);
            var isAdminOrITManager = roles.Any(r => r == "Admin" || r == "IT Manager");
            var isReadOnlyOrEmployee = roles.Any(r => r == "Read Only" || r == "Employee");

            // Admin and IT Manager can change Role and Department
            if (!isAdminOrITManager)
            {
                // Remove role validation - non-admin users cannot change their own role
                ModelState.Remove(nameof(model.Role));
                
                // Remove Department validation for Read Only and Employee users
                if (isReadOnlyOrEmployee)
                {
                    ModelState.Remove(nameof(model.Department));
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.IsProfile = true;
                ViewBag.IsAdminOrITManager = isAdminOrITManager;
                ViewBag.IsReadOnlyOrEmployee = isReadOnlyOrEmployee;
                return View("EditProfile", model);
            }

            // Update user properties
            user.Email = model.Email;
            user.UserName = model.Email;
            user.FullName = model.FullName;
            
            // Admin and IT Manager can change Department and Role
            if (isAdminOrITManager)
            {
                user.Department = model.Department;
                
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
            }
            else
            {
                // Only update Department if user is not Read Only or Employee
                if (!isReadOnlyOrEmployee)
                {
                    user.Department = model.Department;
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ViewBag.IsProfile = true;
                ViewBag.IsAdminOrITManager = isAdminOrITManager;
                ViewBag.IsReadOnlyOrEmployee = isReadOnlyOrEmployee;
                return View("EditProfile", model);
            }

            // Log the profile update
            _context.AuditLogs.Add(new AuditLog
            {
                UserName = User.Identity?.Name ?? "",
                Action = "Update Profile",
                EntityType = "User",
                EntityId = null,
                Details = $"User updated their own profile",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your profile has been updated successfully.";
            return RedirectToAction("Profile", "Account");
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Validate inputs
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                ModelState.AddModelError(string.Empty, "Current password is required.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError(string.Empty, "New password is required.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "New password and confirmation password do not match.");
                return View();
            }

            // Validate password meets requirements
            if (newPassword.Length < 8 || newPassword.Length > 30)
            {
                ModelState.AddModelError(string.Empty, "Password must be between 8 and 30 characters.");
                return View();
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(newPassword, @"\d"))
            {
                ModelState.AddModelError(string.Empty, "Password must include at least one digit.");
                return View();
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(newPassword, @"[~!@#$^&*?><]"))
            {
                ModelState.AddModelError(string.Empty, "Password must include at least one special character [~!@#$^&*?><].");
                return View();
            }

            // Verify current password
            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, currentPassword, lockoutOnFailure: false);
            if (!passwordCheck.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Current password is incorrect.");
                return View();
            }

            // Change password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                // Update password changed date and clear must change flag
                user.PasswordChangedDate = DateTime.Now;
                user.MustChangePassword = false;
                await _userManager.UpdateAsync(user);

                // Log the password change
                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "",
                    Action = "Change Password",
                    EntityType = "Account",
                    EntityId = null,
                    Details = "User changed their password",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your password has been changed successfully.";
                return RedirectToAction("Profile", "Account");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ChangeExpiredPassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Verify user must change password
            if (!user.MustChangePassword)
            {
                // Check if password has expired (30 days)
                bool passwordExpired = false;
                if (user.PasswordChangedDate.HasValue)
                {
                    var daysSinceChange = (DateTime.Now - user.PasswordChangedDate.Value).TotalDays;
                    if (daysSinceChange >= 30)
                    {
                        passwordExpired = true;
                    }
                }
                else
                {
                    passwordExpired = true;
                }

                if (!passwordExpired)
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeExpiredPassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Validate inputs
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                ModelState.AddModelError(string.Empty, "Current password is required.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError(string.Empty, "New password is required.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "New password and confirmation password do not match.");
                return View();
            }

            // Validate password meets requirements
            if (newPassword.Length < 8 || newPassword.Length > 30)
            {
                ModelState.AddModelError(string.Empty, "Password must be between 8 and 30 characters.");
                return View();
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(newPassword, @"\d"))
            {
                ModelState.AddModelError(string.Empty, "Password must include at least one digit.");
                return View();
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(newPassword, @"[~!@#$^&*?><]"))
            {
                ModelState.AddModelError(string.Empty, "Password must include at least one special character [~!@#$^&*?><].");
                return View();
            }

            // Verify current password
            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, currentPassword, lockoutOnFailure: false);
            if (!passwordCheck.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Current password is incorrect.");
                return View();
            }

            // Change password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                // Update password changed date and clear must change flag
                user.PasswordChangedDate = DateTime.Now;
                user.MustChangePassword = false;
                await _userManager.UpdateAsync(user);

                // Log the password change
                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "",
                    Action = "Change Expired Password",
                    EntityType = "Account",
                    EntityId = null,
                    Details = "User changed expired password",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your password has been changed successfully. You can now access the system.";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View();
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}