using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using LiteratureClub.Models;

namespace LiteratureClub.Services
{
    /// <summary>
    /// Second (final) fallback provider. Uses gpt-4o-mini via the official OpenAI NuGet package.
    /// Install: dotnet add package OpenAI
    /// Config key: OpenAI:ApiKey
    /// </summary>
    public class OpenAIProvider : IAIProvider
    {
        private readonly string _apiKey;

        public string Name => "OpenAI";

        public OpenAIProvider(string apiKey) => _apiKey = apiKey;

        public async Task<ChatResponse> GetReplyAsync(
            string systemInstruction,
            string userMessage,
            List<LiteratureClub.Models.ChatMessage>? history,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_apiKey))
                return new ChatResponse { Success = false, Error = "OpenAI API key is missing." };

            // Initialize the official OpenAI Client
            var openAiClient = new OpenAIClient(_apiKey);
            var chatClient = openAiClient.GetChatClient("gpt-4o-mini");

            // Avoid collision by explicitly targeting the SDK's message types
            var messages = new List<OpenAI.Chat.ChatMessage>
            {
                new SystemChatMessage(systemInstruction)
            };

            if (history != null)
            {
                foreach (var msg in history)
                {
                    if (string.IsNullOrWhiteSpace(msg.Content)) continue;

                    if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                    {
                        messages.Add(new UserChatMessage(msg.Content));
                    }
                    // Matches standard web inputs or internal fallback names
                    else if (msg.Role.Equals("ai agent", StringComparison.OrdinalIgnoreCase) ||
                             msg.Role.Equals("model", StringComparison.OrdinalIgnoreCase) ||
                             msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                    {
                        messages.Add(new AssistantChatMessage(msg.Content));
                    }
                }
            }

            // Add the final user message prompt
            messages.Add(new UserChatMessage(userMessage));

            var options = new ChatCompletionOptions
            {
                Temperature = 0.7f
            };

            // Call the API asynchronously
            ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options, ct);

            if (completion?.Content != null && completion.Content.Count > 0)
            {
                string reply = completion.Content[0].Text ?? "I couldn't generate a text response.";
                return new ChatResponse { Success = true, Message = reply };
            }

            return new ChatResponse { Success = false, Error = "OpenAI returned an empty response." };
        }
    }
}