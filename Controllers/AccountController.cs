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
            if (ModelState.IsValid)
            {
                var newUser = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    Department = model.Department
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

                    return RedirectToAction("Index", "Home");
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