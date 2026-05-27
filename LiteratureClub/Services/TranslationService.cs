using System;
using System.Collections.Generic;
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

        public async Task<string> TranslateUiTextAsync(string originalText, string targetLanguage)
        {
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrWhiteSpace(originalText))
                return originalText;

            // If the user selects English, return the original text directly without calling the API
            if (targetLanguage.Equals("English", StringComparison.OrdinalIgnoreCase))
                return originalText;

            try
            {
                var client = new Client(apiKey: _apiKey);

                var systemInstruction = new Content();
                systemInstruction.Parts.Add(new Part
                {
                    Text = $"You are a localization software engine for a South African student textbook marketplace. " +
                           $"Translate the provided text precisely into {targetLanguage}. Maintain standard conversational terminology " +
                           $"used by university students (e.g., matching common expressions for modules, buy, or sell). " +
                           $"Output ONLY the translated text. Do not add explanations, notes, or wrapper quotes."
                });

                var config = new GenerateContentConfig
                {
                    SystemInstruction = systemInstruction,
                    Temperature = 0.1f // Near-deterministic for reliable UI consistency
                };

                var contents = new List<Content>
                {
                    new Content { Role = "user", Parts = { new Part { Text = originalText } } }
                };

                var response = await client.Models.GenerateContentAsync(
                    model: "gemini-2.5-flash",
                    contents: contents,
                    config: config
                );

                if (response?.Candidates != null && response.Candidates.Count > 0)
                {
                    var translatedText = response.Candidates[0].Content?.Parts?[0]?.Text;
                    if (!string.IsNullOrWhiteSpace(translatedText))
                    {
                        return translatedText.Trim();
                    }
                }

                return originalText;
            }
            catch
            {
                // Fallback gracefully to original English text if the API fails
                return originalText;
            }
        }
    }
}