using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT_Asset_Management_System.Data;
using IT_Asset_Management_System.Models;

namespace IT_Asset_Management_System.Controllers
{
    [Authorize]
    public class ComputersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ComputersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var computers = await _context.Computers
                .Where(c => c.AssetTag != null 
                    && c.AssetName != null 
                    && !string.IsNullOrWhiteSpace(c.AssetTag) 
                    && !string.IsNullOrWhiteSpace(c.AssetName))
                .ToListAsync();
            return View(computers);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var computer = await _context.Computers.FirstOrDefaultAsync(m => m.Id == id);
            if (computer == null)
            {
                return NotFound();
            }

            return View(computer);
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Create()
        {
            // Load users for dropdown
            var users = await _userManager.Users
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.FullName ?? u.UserName ?? u.Email)
                .ToListAsync();
            ViewBag.Users = users;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Create([Bind("AssetTag,AssetName,AssetType,Brand,Model,SerialNumber,Processor,RAM,Storage,OperatingSystem,PurchaseDate,PurchasePrice,Vendor,WarrantyExpiryDate,Location,AssignedTo,Status,Notes")] Computer computer)
        {
            if (ModelState.IsValid)
            {
                computer.AssetType = "Computer";
                computer.CreatedBy = User.Identity?.Name;
                computer.CreatedDate = DateTime.Now;
                _context.Add(computer);
                await _context.SaveChangesAsync();

                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "",
                    Action = "Create",
                    EntityType = "Computer",
                    EntityId = computer.Id,
                    Details = $"Created computer: {computer.AssetName}",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                });
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(computer);
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var computer = await _context.Computers.FindAsync(id);
            if (computer == null)
            {
                return NotFound();
            }

            // Load users for dropdown
            var users = await _userManager.Users
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.FullName ?? u.UserName ?? u.Email)
                .ToListAsync();
            ViewBag.Users = users;

            return View(computer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AssetTag,AssetName,AssetType,Brand,Model,SerialNumber,Processor,RAM,Storage,OperatingSystem,PurchaseDate,PurchasePrice,Vendor,WarrantyExpiryDate,Location,AssignedTo,Status,Notes")] Computer computer)
        {
            if (id != computer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingComputer = await _context.Computers.FindAsync(id);
                    if (existingComputer != null)
                    {
                        existingComputer.AssetTag = computer.AssetTag;
                        existingComputer.AssetName = computer.AssetName;
                        existingComputer.AssetType = "Computer";
                        existingComputer.Brand = computer.Brand;
                        existingComputer.Model = computer.Model;
                        existingComputer.SerialNumber = computer.SerialNumber;
                        existingComputer.Processor = computer.Processor;
                        existingComputer.RAM = computer.RAM;
                        existingComputer.Storage = computer.Storage;
                        existingComputer.OperatingSystem = computer.OperatingSystem;
                        existingComputer.PurchaseDate = computer.PurchaseDate;
                        existingComputer.PurchasePrice = computer.PurchasePrice;
                        existingComputer.Vendor = computer.Vendor;
                        existingComputer.WarrantyExpiryDate = computer.WarrantyExpiryDate;
                        existingComputer.Location = computer.Location;
                        existingComputer.AssignedTo = computer.AssignedTo;
                        existingComputer.Status = computer.Status;
                        existingComputer.Notes = computer.Notes;
                        existingComputer.ModifiedBy = User.Identity?.Name;
                        existingComputer.ModifiedDate = DateTime.Now;

                        await _context.SaveChangesAsync();

                        _context.AuditLogs.Add(new AuditLog
                        {
                            UserName = User.Identity?.Name ?? "",
                            Action = "Update",
                            EntityType = "Computer",
                            EntityId = computer.Id,
                            Details = $"Updated computer: {computer.AssetName}",
                            IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                        });
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ComputerExists(computer.Id))
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
            return View(computer);
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var computer = await _context.Computers.FirstOrDefaultAsync(m => m.Id == id);
            if (computer == null)
            {
                return NotFound();
            }

            return View(computer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var computer = await _context.Computers.FindAsync(id);
            if (computer != null)
            {
                _context.Computers.Remove(computer);
                await _context.SaveChangesAsync();

                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "",
                    Action = "Delete",
                    EntityType = "Computer",
                    EntityId = id,
                    Details = $"Deleted computer: {computer.AssetName}",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ComputerExists(int id)
        {
            return _context.Computers.Any(e => e.Id == id);
        }
    }
}