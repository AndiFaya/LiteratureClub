using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;

namespace LiteratureClub.Services
{
    public class TranslationService
    {
        private readonly string _apiKey;

        public TranslationService(IConfiguration configuration)
        {
            _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        }

        // TranslationService.cs
        private static readonly Dictionary<string, string> CultureToLanguage = new()
        {
            { "en", "English" },    { "en-US", "English" }, { "en-ZA", "English" },
            { "zu", "Zulu" },       { "zu-ZA", "Zulu" },    { "isiZulu", "Zulu" },
            { "xh", "Xhosa" },      { "xh-ZA", "Xhosa" },   { "isiXhosa", "Xhosa" },
            { "st", "Sotho" },      { "st-ZA", "Sotho" },
        };

        public async Task<List<string>> TranslateMultipleTextsAsync(List<string> originalTexts, string targetLanguage)
        {
            // Resolve culture code to full name if needed
            if (CultureToLanguage.TryGetValue(targetLanguage, out var resolvedName))
                targetLanguage = resolvedName;

            if (string.IsNullOrEmpty(_apiKey) || originalTexts == null || originalTexts.Count == 0)
                return originalTexts ?? new List<string>();

            if (targetLanguage.Equals("English", StringComparison.OrdinalIgnoreCase))
                return originalTexts;

            try
            {
                var client = new Client(apiKey: _apiKey);

                var systemContent = new Content
                {
                    Parts = new List<Part>
                    {
                        new Part
                        {
                            Text = $"You are a localization software engine for a South African student textbook marketplace. " +
                                   $"Translate the provided list of text items precisely into {targetLanguage}. Maintain standard conversational terminology " +
                                   $"used by university students (e.g., matching common expressions for modules, buy, or sell). " +
                                   $"Output the translations as a raw list separated strictly by newlines matching the exact order of the input rows. " +
                                   $"Do not add line numbers, explanations, notes, or wrapper quotes."
                        }
                    }
                };

                var config = new GenerateContentConfig
                {
                    SystemInstruction = systemContent,
                    Temperature = 0.1f // Kept low for reliable structural translation consistency
                };

                // Bundle all sentences together separated by unique newline breaks
                string bundledInput = string.Join("\n", originalTexts);

                var contents = new List<Content>
                {
                    new Content
                    {
                        Role = "user",
                        Parts = new List<Part> { new Part { Text = bundledInput } }
                    }
                };

                var response = await client.Models.GenerateContentAsync(
                    model: "gemini-2.5-flash",
                    contents: contents,
                    config: config
                );

                if (response != null && !string.IsNullOrWhiteSpace(response.Text))
                {
                    // Split the single returned translation block back into separate lines
                    var translatedLines = response.Text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(line => line.Trim())
                                   .ToList();

                    var results = new List<string>();
                    for (int i = 0; i < originalTexts.Count; i++)
                    {
                        // Match lines back to their corresponding indexes, fall back to English if counts mismatch
                        if (i < translatedLines.Count && !string.IsNullOrWhiteSpace(translatedLines[i]))
                        {
                            results.Add(translatedLines[i]);
                        }
                        else
                        {
                            results.Add(originalTexts[i]);
                        }
                    }
                    return results;
                }

                return originalTexts;
            }
            catch (Exception ex)
            {
                // Clear any debug screens to quietly drop back to English if the key remains rate-limited
                System.Diagnostics.Debug.WriteLine($"Bundled translation failure: {ex.Message}");
                return originalTexts;
            }
        }
    }
}