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
public class ActivitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public ActivitiesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? type = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isSales = User.IsInRole("SalesExecutive");
        var query = _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.Opportunity)
            .Include(a => a.AssignedUser)
            .AsQueryable();

        if (isSales)
        {
            query = query.Where(a => a.AssignedTo == user.Id);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(a => a.ActivityType == type);
        }

        var activities = await query.OrderByDescending(a => a.ActivityDate).ToListAsync();
        ViewBag.CurrentType = type;

        return View(activities);
    }

    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
    {
        await PopulateDropdownsAsync(customerId, leadId, opportunityId);
        return View(new Activity
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
            ActivityDate = DateTime.UtcNow
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Activity activity)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(activity.CustomerId, activity.LeadId, activity.OpportunityId, activity.AssignedTo);
            return View(activity);
        }

        if (User.IsInRole("SalesExecutive"))
        {
            activity.AssignedTo = user.Id;
        }

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Activity Logged", "Activity", activity.ActivityId.ToString(), null, $"Logged {activity.ActivityType}: {activity.Subject}");
        TempData["Success"] = "Activity logged successfully.";

        if (activity.CustomerId.HasValue)
        {
            return RedirectToAction("Details", "Customers", new { id = activity.CustomerId.Value });
        }
        if (activity.LeadId.HasValue)
        {
            return RedirectToAction("Details", "Leads", new { id = activity.LeadId.Value });
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(int? customerId = null, int? leadId = null, int? opportunityId = null, string? userId = null)
    {
        var customers = await _context.Customers.Where(c => c.Status == "Active").OrderBy(c => c.CustomerName).ToListAsync();
        var leads = await _context.Leads.Where(l => l.Status != "Converted").OrderBy(l => l.LeadName).ToListAsync();
        var opps = await _context.Opportunities.Where(o => o.Status == "Open").OrderBy(o => o.OpportunityName).ToListAsync();
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

        ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", customerId);
        ViewBag.Leads = new SelectList(leads, "LeadId", "LeadName", leadId);
        ViewBag.Opportunities = new SelectList(opps, "OpportunityId", "OpportunityName", opportunityId);
        ViewBag.Users = new SelectList(users, "Id", "FullName", userId);
    }
}
