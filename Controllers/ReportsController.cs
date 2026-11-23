using System.Text;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string type = "Pipeline", string? status = null, string? search = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isSales = User.IsInRole("SalesExecutive");
        var isAdmin = User.IsInRole("Admin");

        // Audit report is admin-only
        if (type == "Audit" && !isAdmin)
        {
            return Forbid();
        }

        ViewBag.ReportType = type;
        ViewBag.Status = status;
        ViewBag.Search = search;

        switch (type)
        {
            case "Customer":
                var custQ = _context.Customers.Include(c => c.AssignedUser).AsQueryable();
                if (isSales) custQ = custQ.Where(c => c.AssignedTo == user.Id);
                if (!string.IsNullOrWhiteSpace(status)) custQ = custQ.Where(c => c.Status == status);
                if (!string.IsNullOrWhiteSpace(search)) custQ = custQ.Where(c => c.CustomerName.Contains(search) || c.Email.Contains(search));
                ViewBag.ReportData = await custQ.OrderBy(c => c.CustomerName).ToListAsync();
                break;

            case "Lead":
                var leadQ = _context.Leads.Include(l => l.AssignedUser).AsQueryable();
                if (isSales) leadQ = leadQ.Where(l => l.AssignedTo == user.Id);
                if (!string.IsNullOrWhiteSpace(status)) leadQ = leadQ.Where(l => l.Status == status);
                if (!string.IsNullOrWhiteSpace(search)) leadQ = leadQ.Where(l => l.LeadName.Contains(search) || l.Email.Contains(search));
                ViewBag.ReportData = await leadQ.OrderByDescending(l => l.CreatedDate).ToListAsync();
                break;

            case "FollowUp":
                var followQ = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).Include(f => f.AssignedUser).AsQueryable();
                if (isSales) followQ = followQ.Where(f => f.AssignedTo == user.Id);
                if (!string.IsNullOrWhiteSpace(status)) followQ = followQ.Where(f => f.Status == status);
                ViewBag.ReportData = await followQ.OrderBy(f => f.FollowUpDate).ToListAsync();
                break;

            case "Opportunity":
            case "Pipeline":
                var oppQ = _context.Opportunities.Include(o => o.Customer).Include(o => o.AssignedUser).AsQueryable();
                if (isSales) oppQ = oppQ.Where(o => o.AssignedTo == user.Id);
                if (!string.IsNullOrWhiteSpace(status)) oppQ = oppQ.Where(o => o.Stage == status || o.Status == status);
                ViewBag.ReportData = await oppQ.OrderByDescending(o => o.Amount).ToListAsync();
                break;

            case "Conversion":
                var convQ = _context.Leads.Include(l => l.AssignedUser).AsQueryable();
                if (isSales) convQ = convQ.Where(l => l.AssignedTo == user.Id);
                ViewBag.ReportData = await convQ.OrderByDescending(l => l.CreatedDate).ToListAsync();
                break;

            case "UserActivity":
                if (isSales) return Forbid(); // Sales executives don't see team activity
                var users = await _userManager.Users.ToListAsync();
                var activityReport = new List<UserActivityReportItem>();
                foreach (var u in users)
                {
                    var acts = await _context.Activities.Where(a => a.AssignedTo == u.Id).ToListAsync();
                    var opps = await _context.Opportunities.Where(o => o.AssignedTo == u.Id).CountAsync();
                    activityReport.Add(new UserActivityReportItem
                    {
                        UserName = u.FullName,
                        Role = u.Role,
                        TotalCalls = acts.Count(a => a.ActivityType == "Call"),
                        TotalMeetings = acts.Count(a => a.ActivityType == "Meeting"),
                        TotalEmails = acts.Count(a => a.ActivityType == "Email"),
                        TotalTasks = acts.Count(a => a.ActivityType == "Task"),
                        TotalActivities = acts.Count,
                        OpportunitiesManaged = opps
                    });
                }
                ViewBag.ReportData = activityReport;
                break;

            case "Audit":
                var auditQ = _context.AuditLogs.AsQueryable();
                if (!string.IsNullOrWhiteSpace(search)) auditQ = auditQ.Where(a => a.Action.Contains(search) || a.EntityName.Contains(search));
                ViewBag.ReportData = await auditQ.OrderByDescending(a => a.CreatedDate).Take(100).ToListAsync();
                break;
        }

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string type = "Pipeline")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isSales = User.IsInRole("SalesExecutive");
        var isAdmin = User.IsInRole("Admin");

        if (type == "Audit" && !isAdmin) return Forbid();

        var csv = new StringBuilder();

        switch (type)
        {
            case "Customer":
                csv.AppendLine("CustomerCode,CustomerName,Email,Phone,Company,City,State,Status,CreatedDate");
                var custQ = _context.Customers.AsQueryable();
                if (isSales) custQ = custQ.Where(c => c.AssignedTo == user.Id);
                foreach (var c in await custQ.ToListAsync())
                {
                    csv.AppendLine($"\"{c.CustomerCode}\",\"{c.CustomerName}\",\"{c.Email}\",\"{c.Phone}\",\"{c.CompanyName}\",\"{c.City}\",\"{c.State}\",\"{c.Status}\",\"{c.CreatedDate:yyyy-MM-dd}\"");
                }
                break;

            case "Lead":
                csv.AppendLine("LeadCode,LeadName,Email,Phone,Company,Source,Status,Priority,ExpectedValue,CreatedDate");
                var leadQ = _context.Leads.AsQueryable();
                if (isSales) leadQ = leadQ.Where(l => l.AssignedTo == user.Id);
                foreach (var l in await leadQ.ToListAsync())
                {
                    csv.AppendLine($"\"{l.LeadCode}\",\"{l.LeadName}\",\"{l.Email}\",\"{l.Phone}\",\"{l.CompanyName}\",\"{l.Source}\",\"{l.Status}\",\"{l.Priority}\",{l.ExpectedValue},\"{l.CreatedDate:yyyy-MM-dd}\"");
                }
                break;

            case "Opportunity":
            case "Pipeline":
                csv.AppendLine("OpportunityName,Amount,Stage,Probability,WeightedPipeline,ExpectedCloseDate,Status");
                var oppQ = _context.Opportunities.AsQueryable();
                if (isSales) oppQ = oppQ.Where(o => o.AssignedTo == user.Id);
                foreach (var o in await oppQ.ToListAsync())
                {
                    csv.AppendLine($"\"{o.OpportunityName}\",{o.Amount},\"{o.Stage}\",{o.Probability}%,{o.WeightedPipeline},\"{o.ExpectedCloseDate:yyyy-MM-dd}\",\"{o.Status}\"");
                }
                break;

            case "FollowUp":
                csv.AppendLine("FollowUpDate,FollowUpType,Subject,Status,Remarks");
                var followQ = _context.FollowUps.AsQueryable();
                if (isSales) followQ = followQ.Where(f => f.AssignedTo == user.Id);
                foreach (var f in await followQ.ToListAsync())
                {
                    csv.AppendLine($"\"{f.FollowUpDate:yyyy-MM-dd HH:mm}\",\"{f.FollowUpType}\",\"{f.Subject}\",\"{f.Status}\",\"{f.Remarks}\"");
                }
                break;

            default:
                csv.AppendLine("Report,Status,Timestamp");
                csv.AppendLine($"\"{type}\",\"Generated\",\"{DateTime.UtcNow}\"");
                break;
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"AcxiomCRM_{type}_Report_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }
}
