using System.Net.Http.Json;
using System.Text.Json;
using AiComplaintAssistant.Models;

namespace AiComplaintAssistant.Services;

public class AiComplaintAssistantService
{
    private readonly HttpClient _httpClient;

    public AiComplaintAssistantService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Classification>> GetClassificationsAsync(string? parentId = null)
    {
        var url = "/api/classfications";
        if (!string.IsNullOrWhiteSpace(parentId))
            url += $"?parentId={Uri.EscapeDataString(parentId)}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Classification>>() ?? new();
    }

    public async Task<List<Classification>> ClassifyEmailAsync(EmailRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/classify-email", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Classification>>() ?? new();
    }

    public async Task<string?> GenerateDraftAsync(DraftRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/generate-draft", request);
        response.EnsureSuccessStatusCode();
        
        return JsonSerializer.Deserialize<string>(await response.Content.ReadAsStringAsync());
    }

    public async Task<string?> RefineDraftAsync(RefineRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/refine-draft", request);
        response.EnsureSuccessStatusCode();
        
        return JsonSerializer.Deserialize<string>(await response.Content.ReadAsStringAsync());
    }
    
}
