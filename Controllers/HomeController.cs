using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOmniRouteService _aiService;

    public HomeController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IOmniRouteService aiService)
    {
        _context = context;
        _userManager = userManager;
        _aiService = aiService;
    }

    public async Task<IActionResult> Index(string dateFilter = "All")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isSales = User.IsInRole("SalesExecutive");
        var currentUserId = user.Id;

        // Base queries scoped by role
        var customersQuery = _context.Customers.AsQueryable();
        var leadsQuery = _context.Leads.AsQueryable();
        var oppsQuery = _context.Opportunities.AsQueryable();
        var followUpsQuery = _context.FollowUps.AsQueryable();
        var activitiesQuery = _context.Activities.AsQueryable();

        if (isSales)
        {
            customersQuery = customersQuery.Where(c => c.AssignedTo == currentUserId);
            leadsQuery = leadsQuery.Where(l => l.AssignedTo == currentUserId);
            oppsQuery = oppsQuery.Where(o => o.AssignedTo == currentUserId);
            followUpsQuery = followUpsQuery.Where(f => f.AssignedTo == currentUserId);
            activitiesQuery = activitiesQuery.Where(a => a.AssignedTo == currentUserId);
        }

        // Apply Date Filter
        DateTime? filterStart = null;
        DateTime now = DateTime.UtcNow;

        switch (dateFilter.ToLower())
        {
            case "today":
                filterStart = now.Date;
                break;
            case "thisweek":
                int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                filterStart = now.Date.AddDays(-1 * diff);
                break;
            case "thismonth":
                filterStart = new DateTime(now.Year, now.Month, 1);
                break;
            default:
                dateFilter = "All";
                break;
        }

        if (filterStart.HasValue)
        {
            customersQuery = customersQuery.Where(c => c.CreatedDate >= filterStart.Value);
            leadsQuery = leadsQuery.Where(l => l.CreatedDate >= filterStart.Value);
            oppsQuery = oppsQuery.Where(o => o.CreatedDate >= filterStart.Value);
        }

        var totalCustomers = await customersQuery.CountAsync();
        var totalLeads = await leadsQuery.CountAsync();
        var openLeads = await leadsQuery.Where(l => l.Status == "New" || l.Status == "Contacted" || l.Status == "Qualified").CountAsync();
        var totalOpps = await oppsQuery.CountAsync();
        var openOpps = await oppsQuery.Where(o => o.Status == "Open").CountAsync();
        var wonOpps = await oppsQuery.Where(o => o.Status == "Won").CountAsync();
        var lostOpps = await oppsQuery.Where(o => o.Status == "Lost").CountAsync();

        var oppsList = await oppsQuery.ToListAsync();
        var totalPipeline = oppsList.Where(o => o.Status == "Open").Sum(o => o.Amount);
        var weightedPipeline = oppsList.Where(o => o.Status == "Open").Sum(o => o.WeightedPipeline);

        // Chart 1: Lead Status Distribution
        var leadStatuses = await _context.Leads
            .Where(l => !isSales || l.AssignedTo == currentUserId)
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(k => k.Status, v => v.Count);

        // Chart 2: Opportunity Stage Distribution & Chart 3: Monthly Sales Performance
        var allOpps = await _context.Opportunities
            .Where(o => !isSales || o.AssignedTo == currentUserId)
            .ToListAsync();

        var oppStages = allOpps
            .GroupBy(o => o.Stage)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var monthlyPerformance = new Dictionary<string, decimal>();
        for (int i = 5; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            var monthKey = monthDate.ToString("MMM yyyy");
            var monthTotal = allOpps
                .Where(o => o.Status == "Won" && o.CreatedDate.Month == monthDate.Month && o.CreatedDate.Year == monthDate.Year)
                .Sum(o => o.Amount);
            monthlyPerformance[monthKey] = monthTotal;
        }

        // Upcoming Follow-ups
        var upcomingFollowUps = await _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Where(f => (!isSales || f.AssignedTo == currentUserId) && (f.Status == "Planned" || f.Status == "Missed"))
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToListAsync();

        // Recent Activities
        var recentActivities = await _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedUser)
            .Where(a => !isSales || a.AssignedTo == currentUserId)
            .OrderByDescending(a => a.ActivityDate)
            .Take(5)
            .ToListAsync();

        // AI Summary
        var aiSummary = await _aiService.SummarizePipelineAsync(openOpps, totalPipeline, weightedPipeline, wonOpps, lostOpps);

        var viewModel = new DashboardViewModel
        {
            TotalCustomers = totalCustomers,
            TotalLeads = totalLeads,
            OpenLeads = openLeads,
            TotalOpportunities = totalOpps,
            OpenOpportunities = openOpps,
            WonOpportunities = wonOpps,
            LostOpportunities = lostOpps,
            TotalPipelineValue = totalPipeline,
            WeightedPipelineValue = weightedPipeline,
            DateFilter = dateFilter,
            LeadStatusDistribution = leadStatuses,
            OpportunityStageDistribution = oppStages,
            MonthlySalesPerformance = monthlyPerformance,
            UpcomingFollowUps = upcomingFollowUps,
            RecentActivities = recentActivities,
            AiExecutiveSummary = aiSummary
        };

        return View(viewModel);
    }
}
