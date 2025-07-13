using System.Threading;
using System.Threading.Tasks;
using AiComplaintAssistant.Api.Models;
using AiComplaintAssistant.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AiComplaintAssistant.Api.Extensions;

internal static class WebApplicationExtensions
{
    internal static WebApplication MapApi(this WebApplication app)
    {
        var api = app.MapGroup("api");
        api.MapGet("/classfications", OnGetClassifications);
        api.MapPost("/classify-email", OnPostClassifyEmail);
        app.MapPost("/generate-draft", OnPostGenerateDraft);
        app.MapPost("/refine-draft", OnPostRefineDraft);
        return app;
    }
    private static async Task<IResult> OnGetClassifications(
        [FromQuery] string? parentId,
        AIService aiService,
        CancellationToken cancellationToken)
    {
        var classifications  = await aiService.GetClasses(parentId);
        return Results.Ok(classifications);
    }

    private static async Task<IResult> OnPostClassifyEmail(
        EmailRequest request,
        AIService aiService,
        CancellationToken cancellationToken)
    {
        
        if (string.IsNullOrEmpty(request.Content))
            return Results.BadRequest();

        var response = await aiService.RunMultiStepClassification(request);
        return TypedResults.Ok(response);
    }
    
    private static async Task<IResult> OnPostGenerateDraft(
        DraftRequest input,
        AIService aiService) 
    {
        var draft = await aiService.GenerateInitialResponseAsync(input.EmailRequest, input.Classifications);
        return Results.Ok(draft);
    }
    
    private static async Task<IResult> OnPostRefineDraft(
        RefineRequest request,
        AIService aiService) 
    {
        var updated = await aiService.RefineResponseAsync(request.ExistingResponse, request.Instruction);
        return Results.Ok(updated);
    }
}