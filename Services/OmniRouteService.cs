using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AcxiomCRM.Services;

public class OmniRouteService : IOmniRouteService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OmniRouteService> _logger;

    public OmniRouteService(HttpClient httpClient, IConfiguration configuration, ILogger<OmniRouteService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetBaseUrl() => _configuration["OmniRoute:BaseUrl"] ?? "http://localhost:20128/v1";
    private string GetApiKey() => Environment.GetEnvironmentVariable("OMNIROUTE_API_KEY") ?? _configuration["OmniRoute:ApiKey"] ?? string.Empty;
    private string GetModel() => _configuration["OmniRoute:Model"] ?? "auto";

    private async Task<string?> CallChatCompletionAsync(string prompt)
    {
        try
        {
            var baseUrl = GetBaseUrl().TrimEnd('/');
            var apiKey = GetApiKey();
            var model = GetModel();

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "You are an intelligent executive assistant for AcxiomCRM. Provide concise, professional sales insights and recommendations." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 300,
                temperature = 0.5
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            request.Content = jsonContent;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var response = await _httpClient.SendAsync(request, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cts.Token);
                using var doc = JsonDocument.Parse(responseJson);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    return content.Trim();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "OmniRoute AI gateway call unavailable, falling back to rule-based insight.");
        }
        return null;
    }

    public async Task<string> SummarizeCustomerAsync(string customerName, string company, int oppCount, decimal totalValue, int followUpCount, string recentActivity)
    {
        var prompt = $"Summarize customer relationship for {customerName} at {company}. Opportunities: {oppCount}, Pipeline Value: ${totalValue:N2}, Follow-ups: {followUpCount}, Recent activity: {recentActivity}. Provide 2 bullet points on health and recommended next steps.";
        var aiResult = await CallChatCompletionAsync(prompt);
        if (!string.IsNullOrEmpty(aiResult)) return aiResult;

        return $"• Account Status: {customerName} ({company}) has {oppCount} active opportunity(ies) valued at ${totalValue:N2}.\n" +
               $"• Engagement: {followUpCount} scheduled follow-up(s). Recent touchpoint: {(string.IsNullOrEmpty(recentActivity) ? "Standard check-in" : recentActivity)}.\n" +
               $"• Recommendation: Maintain weekly cadence and confirm upcoming milestone requirements.";
    }

    public async Task<string> SummarizeLeadAsync(string leadName, string company, string status, string priority, decimal expectedValue, string source)
    {
        var prompt = $"Summarize sales lead for {leadName} at {company}. Status: {status}, Priority: {priority}, Expected Value: ${expectedValue:N2}, Source: {source}.";
        var aiResult = await CallChatCompletionAsync(prompt);
        if (!string.IsNullOrEmpty(aiResult)) return aiResult;

        return $"• Lead Profile: {leadName} ({company}) acquired via {source}. Priority level: {priority}.\n" +
               $"• Current Stage: Status is '{status}' with an estimated deal value of ${expectedValue:N2}.\n" +
               $"• Recommendation: {(status == "Qualified" ? "Proceed with Customer & Opportunity conversion." : "Conduct discovery call to confirm budget and timeline.")}";
    }

    public async Task<string> SuggestFollowUpMessageAsync(string clientName, string context, string followUpType)
    {
        var prompt = $"Draft a professional {followUpType} follow-up message to {clientName} regarding: {context}. Keep it under 60 words.";
        var aiResult = await CallChatCompletionAsync(prompt);
        if (!string.IsNullOrEmpty(aiResult)) return aiResult;

        return $"Dear {clientName},\n\nI hope you are having a productive week. Following up on our recent discussion regarding {context}, I would like to confirm our progress and address any pending questions. Please let me know your availability for a quick update.\n\nBest regards,\nAcxiomCRM Sales Team";
    }

    public async Task<string> SummarizePipelineAsync(int totalDeals, decimal totalValue, decimal weightedValue, int wonCount, int lostCount)
    {
        var prompt = $"Summarize CRM pipeline: Total Deals: {totalDeals}, Total Pipeline: ${totalValue:N2}, Weighted Value: ${weightedValue:N2}, Closed Won: {wonCount}, Closed Lost: {lostCount}.";
        var aiResult = await CallChatCompletionAsync(prompt);
        if (!string.IsNullOrEmpty(aiResult)) return aiResult;

        var winRate = (wonCount + lostCount) > 0 ? (wonCount * 100.0 / (wonCount + lostCount)) : 0;
        return $"• Pipeline Overview: {totalDeals} active opportunities totaling ${totalValue:N2} with a weighted forecast of ${weightedValue:N2}.\n" +
               $"• Deal Conversion: {wonCount} won vs {lostCount} lost (Win rate: {winRate:F1}%).\n" +
               $"• Focus Area: Accelerate mid-stage negotiation deals to lock in quarterly revenue.";
    }
}
