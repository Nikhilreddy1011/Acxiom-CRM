using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly IOmniRouteService _aiService;

    public CustomersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService,
        IOmniRouteService aiService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
        _aiService = aiService;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isSales = User.IsInRole("SalesExecutive");
        var query = _context.Customers.Include(c => c.AssignedUser).AsQueryable();

        if (isSales)
        {
            query = query.Where(c => c.AssignedTo == user.Id);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c => c.CustomerName.Contains(search) ||
                                     c.Email.Contains(search) ||
                                     c.Phone.Contains(search) ||
                                     (c.CompanyName != null && c.CompanyName.Contains(search)) ||
                                     c.CustomerCode.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        int pageSize = 8;
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var customers = await query
            .OrderByDescending(c => c.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;

        return View(customers);
    }

    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var customer = await _context.Customers
            .Include(c => c.AssignedUser)
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
            .Include(c => c.Activities)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && customer.AssignedTo != user.Id)
        {
            return Forbid();
        }

        // Customer audit history
        var history = await _context.AuditLogs
            .Where(a => a.EntityName == "Customer" && a.RecordId == customer.CustomerCode)
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();
        ViewBag.AuditHistory = history;

        // AI summary
        var totalVal = customer.Opportunities.Sum(o => o.Amount);
        var recentAct = customer.Activities.OrderByDescending(a => a.ActivityDate).FirstOrDefault()?.Subject ?? "No recent activity";
        ViewBag.AiSummary = await _aiService.SummarizeCustomerAsync(customer.CustomerName, customer.CompanyName ?? "N/A", customer.Opportunities.Count, totalVal, customer.FollowUps.Count, recentAct);

        return View(customer);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateUsersDropdownAsync();
        return View(new Customer());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Unique email & phone check
        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == customer.Email.ToLower()))
        {
            ModelState.AddModelError("Email", "A customer with this email address already exists.");
        }

        if (await _context.Customers.AnyAsync(c => c.Phone == customer.Phone))
        {
            ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync(customer.AssignedTo);
            return View(customer);
        }

        // Auto-generate CustomerCode
        var count = await _context.Customers.CountAsync() + 1001;
        customer.CustomerCode = $"CUST-{count}";
        customer.CreatedDate = DateTime.UtcNow;
        customer.CreatedBy = user.Id;

        if (User.IsInRole("SalesExecutive"))
        {
            customer.AssignedTo = user.Id;
        }

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Customer Created", "Customer", customer.CustomerCode, null, $"Customer {customer.CustomerName} ({customer.Email}) created");

        TempData["Success"] = $"Customer '{customer.CustomerName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && customer.AssignedTo != user.Id)
        {
            return Forbid();
        }

        await PopulateUsersDropdownAsync(customer.AssignedTo);
        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer customer)
    {
        if (id != customer.CustomerId) return BadRequest();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var existing = await _context.Customers.FindAsync(id);
        if (existing == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && existing.AssignedTo != user.Id)
        {
            return Forbid();
        }

        // Unique email check excluding self
        if (await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Email.ToLower() == customer.Email.ToLower()))
        {
            ModelState.AddModelError("Email", "Another customer with this email address already exists.");
        }

        // Unique phone check excluding self
        if (await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Phone == customer.Phone))
        {
            ModelState.AddModelError("Phone", "Another customer with this phone number already exists.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync(customer.AssignedTo);
            return View(customer);
        }

        var oldValue = $"Name: {existing.CustomerName}, Phone: {existing.Phone}, Status: {existing.Status}";

        existing.CustomerName = customer.CustomerName;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.CompanyName = customer.CompanyName;
        existing.Address = customer.Address;
        existing.City = customer.City;
        existing.State = customer.State;
        existing.Status = customer.Status;

        if (!User.IsInRole("SalesExecutive"))
        {
            existing.AssignedTo = customer.AssignedTo;
        }

        var newValue = $"Name: {existing.CustomerName}, Phone: {existing.Phone}, Status: {existing.Status}";

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Customer Updated", "Customer", existing.CustomerCode, oldValue, newValue);

        TempData["Success"] = $"Customer '{existing.CustomerName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && customer.AssignedTo != user.Id)
        {
            return Forbid();
        }

        customer.Status = customer.Status == "Active" ? "Inactive" : "Active";
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Customer Status Changed", "Customer", customer.CustomerCode, null, $"Status changed to {customer.Status}");
        TempData["Success"] = $"Customer status updated to {customer.Status}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateUsersDropdownAsync(string? selectedId = null)
    {
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        ViewBag.Users = new SelectList(users, "Id", "FullName", selectedId);
    }
}
