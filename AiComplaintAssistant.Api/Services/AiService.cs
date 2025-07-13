using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AiComplaintAssistant.Api.Models;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;

namespace AiComplaintAssistant.Api.Services;

public class AIService
{
    private readonly Kernel _kernel;
    private readonly ILogger<AIService> _logger;

    public AIService(Kernel kernel, ILogger<AIService> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }
    
    private static async Task<Dictionary<string, string>> LoadClassificationTreeAsync()
    {
        try
        {
            await using var stream = File.OpenRead("classifications.json");
            return await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream)
                   ?? throw new InvalidOperationException("Classification tree could not be deserialized.");
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Failed to load classification tree from file.", ex);
        }
    }

    public async Task<List<Classification>> GetClasses(string? parentId)
    {
        try
        {
            _logger.LogDebug("Getting classes for parentId={ParentId}", parentId);
            var tree = await LoadClassificationTreeAsync();
            var result = tree
                .Where(kvp =>
                    string.IsNullOrWhiteSpace(parentId)
                        ? kvp.Key.Length == 1
                        : kvp.Key.StartsWith(parentId) && (kvp.Key.Length == parentId.Length + 1 || parentId.Length == 2 && kvp.Key.Length > 2))
                .Select(kvp => new Classification(kvp.Key, kvp.Value))
                .ToList();
            _logger.LogInformation("Retrieved {Count} classifications for parentId={ParentId}", result.Count, parentId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get classes for parentId={ParentId}", parentId);
            throw;
        }
    }

    public async Task<List<Classification>> RunMultiStepClassification(EmailRequest input)
    {
        var result = new List<Classification>();
        var currentPrefix = "";

        try
        {
            _logger.LogInformation("Starting multi-step classification for email: {Subject}", input.Subject);

            for (var round = 0; round < 3; round++)
            {
                var levelName = round switch
                {
                    0 => "top-level",
                    1 => "second-level",
                    2 => "detailed",
                    _ => "unknown"
                };

                var options = (await GetClasses(currentPrefix)).Select(m => (m.Id, m.Name)).ToList();
                if (options.Count == 0)
                {
                    _logger.LogWarning("No options available for level: {Level}, stopping classification", levelName);
                    break;
                }

                var previousPath = string.Join(" > ", result.Select(r => r.Name));
                var prompt = GeneratePrompt(input.Subject, input.Content, levelName, previousPath, options);

                _logger.LogDebug("Generated prompt for level {Level}: {PromptSnippet}", levelName, prompt[..Math.Min(200, prompt.Length)]);

                var completion = await _kernel.InvokePromptAsync(prompt);
                var selectedId = completion.ToString().Trim();
                _logger.LogDebug("Response from AI: {Response}", completion);

                var tree = await LoadClassificationTreeAsync();
                if (!tree.TryGetValue(selectedId, out var selectedName))
                {
                    _logger.LogWarning("Invalid classification ID returned: {SelectedId}", selectedId);
                    break;
                }

                result.Add(new Classification(selectedId, selectedName));
                currentPrefix = selectedId;

                _logger.LogInformation("Step {Round}: selected classification {Id} - {Name}", round + 1, selectedId, selectedName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during multi-step classification.");
            throw;
        }

        return result;
    }

    private static string GeneratePrompt(string subject, string content, string levelName, string contextPath, List<(string Id, string Name)> options)
    {
        var optionsText = string.Join("\n", options.Select(opt => $"- {opt.Id}: {opt.Name}"));
        var contextInfo = string.IsNullOrWhiteSpace(contextPath) ? "" : $"Prethodne klasifikacije: {contextPath}\n\n";

        return $"""
                Na osnovu sadržaja mejla ispod, izaberi **najrelevantniju kategoriju** za nivo **{levelName}** iz ponuđene liste.

                Vrati **isključivo** ID kategorije kao **ceo broj** (npr. `12`). Nemoj objašnjavati izbor, nemoj dodavati ništa osim tog broja.

                Ako nisi potpuno siguran, izaberi onu kategoriju koja **najviše odgovara** temi mejla.

                {(string.IsNullOrWhiteSpace(contextInfo) ? "" : $"Kontekst prethodnih izbora:\n{contextInfo}\n")}

                ------------------------
                **Naslov:**  
                {subject}

                **Sadržaj mejla:**  
                {content}

                ------------------------
                **Moguće kategorije (ID – Naziv):**  
                {optionsText}
                """;
    }

    public async Task<string> GenerateInitialResponseAsync(EmailRequest input, List<Classification> classifications)
    {
        try
        {
            var context = string.Join(" > ", classifications.Select(c => c.Name));
            var prompt = $"""
                          Ti si službenik korisničke podrške u OTP banci Srbija. Tvoj zadatak je da napišeš jasan, profesionalan i empatičan odgovor na žalbu klijenta.

                          Klijent je poslao sledeću poruku:

                          Naslov: {input.Subject}  
                          Sadržaj:  
                          {input.Content}

                          Na osnovu prethodne klasifikacije problema: {context}

                          Molimo te da napišeš odgovor koji:
                          - Jasno adresira problem klijenta
                          - Ima empatičan ton, ali ostaje profesionalan i formalan
                          - Je u skladu sa standardima komunikacije u regulisanom finansijskom sektoru
                          - Ne sadrži generičke fraze već konkretno odgovara na upit

                          Odgovor treba da počinje sa "Poštovani/Poštovana," i da bude spreman za slanje bez dodatne obrade.
                          """;

            _logger.LogDebug("Generating initial response for context: {Context}", context);
            var result = await _kernel.InvokePromptAsync(prompt);
            return result.ToString().Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate initial response.");
            throw;
        }
    }

    public async Task<string> RefineResponseAsync(string existingResponse, string officerInstruction)
    {
        try
        {
            var prompt = $"""
                          Sledeći tekst je nacrt odgovora na žalbu klijenta:

                          "{existingResponse}"

                          Izmeni ovaj odgovor na osnovu sledećeg uputstva: {officerInstruction}

                          Vrati samo izmenjeni tekst odgovora, celovit i spreman za slanje klijentu.
                          """;

            _logger.LogDebug("Refining response with instruction: {Instruction}", officerInstruction);
            var result = await _kernel.InvokePromptAsync(prompt);
            return result.ToString().Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refine response.");
            throw;
        }
    }
}