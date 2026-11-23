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
public class OpportunitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public OpportunitiesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search, string? stage, string? status, int page = 1)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isSales = User.IsInRole("SalesExecutive");
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedUser)
            .AsQueryable();

        if (isSales)
        {
            query = query.Where(o => o.AssignedTo == user.Id);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(o => o.OpportunityName.Contains(search) ||
                                     (o.Customer != null && o.Customer.CustomerName.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(stage))
        {
            query = query.Where(o => o.Stage == stage);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        int pageSize = 8;
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var opps = await query
            .OrderByDescending(o => o.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPipeline = await query.Where(o => o.Status == "Open").SumAsync(o => (decimal?)o.Amount) ?? 0;
        var weightedPipeline = opps.Where(o => o.Status == "Open").Sum(o => o.WeightedPipeline);

        ViewBag.Search = search;
        ViewBag.Stage = stage;
        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPipeline = totalPipeline;
        ViewBag.WeightedPipeline = weightedPipeline;

        return View(opps);
    }

    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var opp = await _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedUser)
            .Include(o => o.Activities)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opp == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && opp.AssignedTo != user.Id)
        {
            return Forbid();
        }

        return View(opp);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View(new Opportunity { ExpectedCloseDate = DateTime.UtcNow.AddDays(30) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Opportunity opportunity)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Business Validation
        if (opportunity.Status == "Open" && opportunity.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "Amount must be greater than zero for active opportunities.");
        }

        if (opportunity.Probability < 0 || opportunity.Probability > 100)
        {
            ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
        }

        if (opportunity.Status == "Open" && opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError("ExpectedCloseDate", "Expected close date cannot be in the past for active opportunities.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(opportunity.CustomerId, opportunity.LeadId, opportunity.AssignedTo);
            return View(opportunity);
        }

        opportunity.CreatedDate = DateTime.UtcNow;

        if (User.IsInRole("SalesExecutive"))
        {
            opportunity.AssignedTo = user.Id;
        }

        // Sync stage with status if won or lost
        if (opportunity.Stage == "Won")
        {
            opportunity.Status = "Won";
            opportunity.Probability = 100;
        }
        else if (opportunity.Stage == "Lost")
        {
            opportunity.Status = "Lost";
            opportunity.Probability = 0;
        }

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Opportunity Created", "Opportunity", opportunity.OpportunityName, null, $"Amount: ${opportunity.Amount:N2}, Stage: {opportunity.Stage}");
        TempData["Success"] = $"Opportunity '{opportunity.OpportunityName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var opp = await _context.Opportunities.FindAsync(id);
        if (opp == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && opp.AssignedTo != user.Id)
        {
            return Forbid();
        }

        await PopulateDropdownsAsync(opp.CustomerId, opp.LeadId, opp.AssignedTo);
        return View(opp);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Opportunity opportunity)
    {
        if (id != opportunity.OpportunityId) return BadRequest();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var existing = await _context.Opportunities.FindAsync(id);
        if (existing == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && existing.AssignedTo != user.Id)
        {
            return Forbid();
        }

        // Business Validation
        if (opportunity.Status == "Open" && opportunity.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "Amount must be greater than zero for active opportunities.");
        }

        if (opportunity.Probability < 0 || opportunity.Probability > 100)
        {
            ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
        }

        if (opportunity.Status == "Open" && opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError("ExpectedCloseDate", "Expected close date cannot be in the past for active opportunities.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(opportunity.CustomerId, opportunity.LeadId, opportunity.AssignedTo);
            return View(opportunity);
        }

        var oldValue = $"Stage: {existing.Stage}, Amount: {existing.Amount}, Probability: {existing.Probability}%";

        existing.OpportunityName = opportunity.OpportunityName;
        existing.CustomerId = opportunity.CustomerId;
        existing.LeadId = opportunity.LeadId;
        existing.Amount = opportunity.Amount;
        existing.Stage = opportunity.Stage;
        existing.Probability = opportunity.Probability;
        existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
        existing.Status = opportunity.Status;

        if (existing.Stage == "Won")
        {
            existing.Status = "Won";
            existing.Probability = 100;
        }
        else if (existing.Stage == "Lost")
        {
            existing.Status = "Lost";
            existing.Probability = 0;
        }

        if (!User.IsInRole("SalesExecutive"))
        {
            existing.AssignedTo = opportunity.AssignedTo;
        }

        var newValue = $"Stage: {existing.Stage}, Amount: {existing.Amount}, Probability: {existing.Probability}%";

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Opportunity Updated", "Opportunity", existing.OpportunityName, oldValue, newValue);

        TempData["Success"] = $"Opportunity '{existing.OpportunityName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(int? customerId = null, int? leadId = null, string? userId = null)
    {
        var customers = await _context.Customers.Where(c => c.Status == "Active").OrderBy(c => c.CustomerName).ToListAsync();
        var leads = await _context.Leads.Where(l => l.Status != "Converted").OrderBy(l => l.LeadName).ToListAsync();
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

        ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", customerId);
        ViewBag.Leads = new SelectList(leads, "LeadId", "LeadName", leadId);
        ViewBag.Users = new SelectList(users, "Id", "FullName", userId);
    }
}
