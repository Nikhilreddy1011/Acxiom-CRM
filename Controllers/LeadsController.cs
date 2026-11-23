using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class LeadsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly IOmniRouteService _aiService;

    public LeadsController(
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

    public async Task<IActionResult> Index(string? search, string? status, string? priority, int page = 1)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isSales = User.IsInRole("SalesExecutive");
        var query = _context.Leads.Include(l => l.AssignedUser).AsQueryable();

        if (isSales)
        {
            query = query.Where(l => l.AssignedTo == user.Id);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(l => l.LeadName.Contains(search) ||
                                     l.Email.Contains(search) ||
                                     l.Phone.Contains(search) ||
                                     (l.CompanyName != null && l.CompanyName.Contains(search)) ||
                                     l.LeadCode.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(l => l.Priority == priority);
        }

        int pageSize = 8;
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var leads = await query
            .OrderByDescending(l => l.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Priority = priority;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;

        return View(leads);
    }

    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var lead = await _context.Leads
            .Include(l => l.AssignedUser)
            .Include(l => l.Opportunities)
            .Include(l => l.FollowUps)
            .Include(l => l.Activities)
            .FirstOrDefaultAsync(l => l.LeadId == id);

        if (lead == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && lead.AssignedTo != user.Id)
        {
            return Forbid();
        }

        // AI lead summary
        ViewBag.AiSummary = await _aiService.SummarizeLeadAsync(lead.LeadName, lead.CompanyName ?? "N/A", lead.Status, lead.Priority, lead.ExpectedValue, lead.Source);

        return View(lead);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateUsersDropdownAsync();
        return View(new Lead());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lead lead)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync(lead.AssignedTo);
            return View(lead);
        }

        var count = await _context.Leads.CountAsync() + 2001;
        lead.LeadCode = $"LEAD-{count}";
        lead.CreatedDate = DateTime.UtcNow;

        if (User.IsInRole("SalesExecutive"))
        {
            lead.AssignedTo = user.Id;
        }

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Lead Created", "Lead", lead.LeadCode, null, $"Lead {lead.LeadName} ({lead.Email}) created");
        TempData["Success"] = $"Lead '{lead.LeadName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && lead.AssignedTo != user.Id)
        {
            return Forbid();
        }

        await PopulateUsersDropdownAsync(lead.AssignedTo);
        return View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Lead lead)
    {
        if (id != lead.LeadId) return BadRequest();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var existing = await _context.Leads.FindAsync(id);
        if (existing == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && existing.AssignedTo != user.Id)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync(lead.AssignedTo);
            return View(lead);
        }

        var oldValue = $"Status: {existing.Status}, Priority: {existing.Priority}, Value: {existing.ExpectedValue}";

        existing.LeadName = lead.LeadName;
        existing.Email = lead.Email;
        existing.Phone = lead.Phone;
        existing.CompanyName = lead.CompanyName;
        existing.Source = lead.Source;
        existing.Status = lead.Status;
        existing.Priority = lead.Priority;
        existing.ExpectedValue = lead.ExpectedValue;

        if (!User.IsInRole("SalesExecutive"))
        {
            existing.AssignedTo = lead.AssignedTo;
        }

        var newValue = $"Status: {existing.Status}, Priority: {existing.Priority}, Value: {existing.ExpectedValue}";

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Lead Updated", "Lead", existing.LeadCode, oldValue, newValue);

        TempData["Success"] = $"Lead '{existing.LeadName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Convert(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && lead.AssignedTo != user.Id)
        {
            return Forbid();
        }

        if (lead.Status == "Converted")
        {
            TempData["Error"] = "This lead has already been converted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new ConvertLeadViewModel
        {
            LeadId = lead.LeadId,
            LeadName = lead.LeadName,
            CompanyName = lead.CompanyName,
            Email = lead.Email,
            Phone = lead.Phone,
            CustomerName = lead.CompanyName ?? lead.LeadName,
            OpportunityName = $"{lead.LeadName} - Expansion Deal",
            OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 25000,
            Stage = "Qualification",
            Probability = 30,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(ConvertLeadViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var lead = await _context.Leads.FindAsync(model.LeadId);
        if (lead == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && lead.AssignedTo != user.Id)
        {
            return Forbid();
        }

        // Check customer email collision
        var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == lead.Email.ToLower());
        Customer customer;

        if (existingCustomer != null)
        {
            customer = existingCustomer;
        }
        else
        {
            var custCount = await _context.Customers.CountAsync() + 1001;
            customer = new Customer
            {
                CustomerCode = $"CUST-{custCount}",
                CustomerName = model.CustomerName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Status = "Active",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = user.Id,
                AssignedTo = lead.AssignedTo ?? user.Id
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Customer Created via Lead Conversion", "Customer", customer.CustomerCode, null, $"Created customer from lead {lead.LeadCode}");
        }

        // Create opportunity if requested
        Opportunity? opportunity = null;
        if (model.CreateOpportunity && !string.IsNullOrWhiteSpace(model.OpportunityName))
        {
            opportunity = new Opportunity
            {
                OpportunityName = model.OpportunityName,
                CustomerId = customer.CustomerId,
                LeadId = lead.LeadId,
                Amount = model.OpportunityAmount > 0 ? model.OpportunityAmount : 10000,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = "Open",
                CreatedDate = DateTime.UtcNow,
                AssignedTo = lead.AssignedTo ?? user.Id
            };
            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Opportunity Created via Lead Conversion", "Opportunity", opportunity.OpportunityName, null, $"Created opportunity from lead {lead.LeadCode}");
        }

        // Update Lead status
        lead.Status = "Converted";
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Lead Converted", "Lead", lead.LeadCode, "Status: Qualified/New", $"Converted to Customer: {customer.CustomerCode}{(opportunity != null ? $", Opportunity: {opportunity.OpportunityName}" : "")}");

        TempData["Success"] = $"Lead successfully converted into Customer '{customer.CustomerName}'!";
        return RedirectToAction("Details", "Customers", new { id = customer.CustomerId });
    }

    private async Task PopulateUsersDropdownAsync(string? selectedId = null)
    {
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        ViewBag.Users = new SelectList(users, "Id", "FullName", selectedId);
    }
}
