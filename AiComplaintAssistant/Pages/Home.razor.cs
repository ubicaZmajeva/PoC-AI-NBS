using AiComplaintAssistant.Models;
using AiComplaintAssistant.Services;
using Microsoft.AspNetCore.Components;

namespace AiComplaintAssistant.Pages;

public partial class Home
{
    private bool _loading = false;
    
    [Inject]
    private AiComplaintAssistantService AiService { get; set; } = null!;

    private int _currentStep = 0;
    private MudForm form;
    private bool _isValid;

    private EmailComplaint _emailComplaint = new();

    private List<Classification> _level1Options = new();
    private List<Classification> _level2Options = new();
    private List<Classification> _level3Options = new();
    private string? _selectedLevel1Id;
    private string? _selectedLevel2Id;
    private string? _selectedLevel3Id;
    
    List<Classification> aiResponse = new();
    
    private string? _responseDraft;
    private string? _userPrompt;
        
    private async Task AnalyzeEmail()
    {

        try
        {
            _loading = true;


            await form.Validate();

            if (!form.IsValid)
                return;

            aiResponse = await AiService.ClassifyEmailAsync(new EmailRequest(
            _emailComplaint.EmailAddress,
            _emailComplaint.Subject,
            _emailComplaint.Content));


            _level1Options = await AiService.GetClassificationsAsync();
            if (aiResponse.Count > 0)
            {
                _selectedLevel1Id = aiResponse[0].Id;
                await Value1Changed([_selectedLevel1Id]);

                if (aiResponse.Count > 1)
                {
                    _selectedLevel2Id = aiResponse[1].Id;
                    await Value2Changed([_selectedLevel2Id]);
                }
                
                if (aiResponse.Count > 2)
                {
                    _selectedLevel3Id = aiResponse[2].Id;
                }
            }

            _currentStep = 1;
        }
        finally
        {
            if (!_disposed)
                _loading = false;
        }
    }


    private async Task Value1Changed(IEnumerable<string?>? values)
    {
        if (values == null)
        {   
            _level2Options.Clear();
            _level3Options.Clear();
            return;
        }

        if ((_selectedLevel1Id ?? "") != (values.FirstOrDefault() ?? ""))
        {
            _selectedLevel1Id = values.FirstOrDefault();
            _selectedLevel2Id = null;
            _selectedLevel3Id = null;
            _level3Options.Clear();
        }
        _level2Options = await AiService.GetClassificationsAsync(_selectedLevel1Id);
    }
    
    private async Task Value2Changed(IEnumerable<string?>? values)
    {
        if (values == null)
        {
            _level3Options.Clear();
            return;       
        }

        if ((_selectedLevel2Id ?? "") != (values.FirstOrDefault() ?? ""))
        {
            _selectedLevel2Id = values.FirstOrDefault();
            _selectedLevel3Id = null;
        }
        _level3Options = await AiService.GetClassificationsAsync(_selectedLevel2Id);
    }
    
    private async Task ResetWizard()
    {
        try
        {
            _loading = true;
            _selectedLevel1Id = null;
            _selectedLevel2Id = null;
            _selectedLevel3Id = null;

            _level1Options = await AiService.GetClassificationsAsync();
            _level2Options.Clear();
            _level3Options.Clear();
            aiResponse.Clear();

            _emailComplaint = new();

            _currentStep = 0;
        }
        finally
        {
            if (!_disposed)
                _loading = false;
        }
    }

    private async Task GenerateDraft()
    {
        try
        {
            _loading = true;
            _responseDraft = await AiService.GenerateDraftAsync(
            new DraftRequest(new EmailRequest(
            _emailComplaint.EmailAddress,
            _emailComplaint.Subject,
            _emailComplaint.Content
            ), aiResponse)
            );
            _currentStep = 2;
        }
        finally
        {
            if (!_disposed)
                _loading = false;
        }

    }
    
    private async Task ApplySuggestion()
    {
        try
        {
            _loading = true;
            _responseDraft = await AiService.RefineDraftAsync(
            new RefineRequest(
            _responseDraft,
            _userPrompt
            ));
            _userPrompt = "";
        }
        finally
        {
            if (!_disposed)
                _loading = false;
        }
    }
    
    public class EmailComplaint
    {
        public string EmailAddress { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
    }
    
    private bool _disposed;

    public void Dispose()
    {
        _disposed = true;
    }
}
