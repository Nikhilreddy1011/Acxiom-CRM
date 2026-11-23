using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class DashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }

    public string DateFilter { get; set; } = "All"; // Today, ThisWeek, ThisMonth, All
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Chart Data
    public Dictionary<string, int> LeadStatusDistribution { get; set; } = new();
    public Dictionary<string, decimal> OpportunityStageDistribution { get; set; } = new();
    public Dictionary<string, decimal> MonthlySalesPerformance { get; set; } = new();

    // Lists for Quick Actions
    public List<FollowUp> UpcomingFollowUps { get; set; } = new();
    public List<Activity> RecentActivities { get; set; } = new();
    public List<Opportunity> HighValueOpportunities { get; set; } = new();

    // OmniRoute AI Insight
    public string? AiExecutiveSummary { get; set; }
}
