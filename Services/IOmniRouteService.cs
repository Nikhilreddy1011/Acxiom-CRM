namespace AcxiomCRM.Services;

public interface IOmniRouteService
{
    Task<string> SummarizeCustomerAsync(string customerName, string company, int oppCount, decimal totalValue, int followUpCount, string recentActivity);
    Task<string> SummarizeLeadAsync(string leadName, string company, string status, string priority, decimal expectedValue, string source);
    Task<string> SuggestFollowUpMessageAsync(string clientName, string context, string followUpType);
    Task<string> SummarizePipelineAsync(int totalDeals, decimal totalValue, decimal weightedValue, int wonCount, int lostCount);
}
