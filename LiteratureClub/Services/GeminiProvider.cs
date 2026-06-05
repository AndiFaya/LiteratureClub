using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LiteratureClub.Models;

// Alias the namespace to cleanly access Google's simplified class names
using Gemini = Google.GenAI;

namespace LiteratureClub.Services
{
    public class GeminiProvider : IAIProvider
    {
        private readonly string _apiKey;
        public string Name => "Gemini";

        public GeminiProvider(string apiKey) => _apiKey = apiKey;

        public async Task<ChatResponse> GetReplyAsync(
            string systemInstruction,
            string userMessage,
            List<LiteratureClub.Models.ChatMessage>? history,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_apiKey))
                return new ChatResponse { Success = false, Error = "Gemini API key is missing." };

            try
            {
                // Instantiate the Client with your API Key parameter explicitly
                var client = new Gemini.Client(apiKey: _apiKey);

                // Combine system prompt and user input safely
                var fullPrompt = $"{systemInstruction}\n\nUser: {userMessage}";

                // FIX: Use explicit named arguments 'model' and 'contents' to prevent parameter mismatch
                var response = await client.Models.GenerateContentAsync(
                    model: "gemini-2.5-flash",
                    contents: fullPrompt
                );

                // FIX: Extract text value out of the official Candidate block safely
                var replyText = response.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "";

                return new ChatResponse { Success = true, Message = replyText };
            }
            catch (Exception ex)
            {
                return new ChatResponse { Success = false, Error = ex.Message };
            }
        }
    }
}