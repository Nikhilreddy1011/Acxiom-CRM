namespace AcxiomCRM.ViewModels;

public class ReportFilterViewModel
{
    public string ReportType { get; set; } = "Pipeline";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public string? AssignedTo { get; set; }
    public string? SearchTerm { get; set; }
}

public class PipelineReportItem
{
    public string OpportunityName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Probability { get; set; }
    public decimal WeightedValue { get; set; }
    public DateTime ExpectedCloseDate { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
}

public class ConversionReportItem
{
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal ExpectedValue { get; set; }
    public DateTime CreatedDate { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
}

public class UserActivityReportItem
{
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int TotalCalls { get; set; }
    public int TotalMeetings { get; set; }
    public int TotalEmails { get; set; }
    public int TotalTasks { get; set; }
    public int TotalActivities { get; set; }
    public int OpportunitiesManaged { get; set; }
}
