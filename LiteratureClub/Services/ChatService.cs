using LiteratureClub.Models;
using Microsoft.Extensions.Configuration;

namespace LiteratureClub.Services
{
    /// <summary>
    /// Sends the user's message to AI providers in order: Gemini → Claude → OpenAI.
    /// If a provider fails (quota exceeded, network error, empty response), the next
    /// one is tried automatically. Only if all three fail is an error returned to the UI.
    /// </summary>
    public class ChatService
    {
        private readonly List<IAIProvider> _providers;
        private readonly string _systemInstruction;

        public ChatService(IConfiguration configuration)
        {
            _systemInstruction =
                "You are the official AI campus assistant for 'LiteratureClub', a web-based, student-to-student textbook marketplace. " +
                "Your primary job is to help university students navigate the platform, explain features, and assist them with marketplace rules.\n\n" +

                "KEY PLATFORM INFORMATION:\n" +
                "- WHAT IT IS: LiteratureClub is a dedicated online marketplace designed specifically for university students to buy and sell used academic textbooks safely and affordably.\n" +
                "- THE PROBLEM IT SOLVES: It helps students avoid high campus bookstore prices by allowing them to purchase cheaper second-hand books directly from peers who have completed those modules.\n" +
                "- CORE FUNCTIONALITIES:\n" +
                "  1. Search & Browse: Students can search for textbooks by title, author, edition, or university course module codes.\n" +
                "  2. Sell/List Books: Registered students can upload their old textbooks, set a price, describe the condition (e.g., Brand New, Good, Highlighted), and upload photos.\n" +
                "  3. Secure Payments: Integrated with PayFast for secure digital checkout transactions.\n" +
                "  4. Campus Delivery/Handover: Allows students to arrange safe on-campus meetups or coordinate textbook drop-offs.\n\n" +

                "BEHAVIORAL GUIDELINES:\n" +
                "- Keep responses friendly, encouraging, clear, and professional. You are talking to fellow students.\n" +
                "- If a user asks how to do something (like selling a book), guide them through the steps (e.g., 'Log in, click on List a Book, fill in your module details, and set your price!').\n" +
                "- Protect Privacy: Do not share specific user personal details or database keys.\n" +
                "- Boundary Rule: If a user asks a question completely unrelated to textbooks, academia, or the LiteratureClub application, politely pivot the conversation back to the platform.";

            // Build the provider chain. Providers with missing API keys are skipped at
            // call time (each returns a failed ChatResponse rather than throwing).
            _providers = new List<IAIProvider>();

            // Only add providers to the active pool if their key is genuinely present
            var geminiKey = configuration["Gemini:ApiKey"];
            if (!string.IsNullOrEmpty(geminiKey))
                _providers.Add(new GeminiProvider(geminiKey));

            var anthropicKey = configuration["Anthropic:ApiKey"];
            if (!string.IsNullOrEmpty(anthropicKey))
                _providers.Add(new ClaudeProvider(anthropicKey));

            var openAiKey = configuration["OpenAI:ApiKey"];
            if (!string.IsNullOrEmpty(openAiKey))
                _providers.Add(new OpenAIProvider(openAiKey));
        }

        public async Task<ChatResponse> GetReplyAsync(
            string userMessage,
            List<ChatMessage>? history,
            CancellationToken ct = default)
        {
            var errors = new List<string>();

            foreach (var provider in _providers)
            {
                try
                {
                    var result = await provider.GetReplyAsync(_systemInstruction, userMessage, history, ct);

                    if (result.Success)
                        return result; // Happy path — return immediately

                    // Provider returned a soft failure (quota, empty response, missing key)
                    errors.Add($"{provider.Name}: {result.Error}");
                }
                catch (Exception ex)
                {
                    // Provider threw unexpectedly — log and continue to the next one
                    errors.Add($"{provider.Name}: {ex.Message}");
                }
            }

            // All three providers failed
            return new ChatResponse
            {
                Success = false,
                Error = "All AI providers are currently unavailable. Please try again later.\n" +
                        string.Join("\n", errors)
            };
        }
    }
}