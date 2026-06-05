using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using LiteratureClub.Models;

// Explicitly alias to avoid conflicts with your local LiteratureClub Message models
using AnthropicMessage = Anthropic.SDK.Messaging.Message;

namespace LiteratureClub.Services
{
    public class ClaudeProvider : IAIProvider
    {
        private readonly string _apiKey;
        public string Name => "Claude";

        public ClaudeProvider(string apiKey) => _apiKey = apiKey;

        public async Task<ChatResponse> GetReplyAsync(
            string systemInstruction,
            string userMessage,
            List<LiteratureClub.Models.ChatMessage>? history,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_apiKey))
                return new ChatResponse { Success = false, Error = "Claude API key is missing." };

            var client = new AnthropicClient(_apiKey);
            var messages = new List<AnthropicMessage>();

            if (history != null)
            {
                foreach (var msg in history)
                {
                    // FIX: 'Role' is updated to 'RoleType' in v5.10.0
                    var role = msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase)
                        ? RoleType.User
                        : RoleType.Assistant;

                    messages.Add(new AnthropicMessage
                    {
                        Role = role,
                        Content = new List<ContentBase> { new TextContent { Text = msg.Content } }
                    });
                }
            }

            messages.Add(new AnthropicMessage
            {
                Role = RoleType.User,
                Content = new List<ContentBase> { new TextContent { Text = userMessage } }
            });

            // FIX: Enclose the system property explicitly as a List of SystemMessage elements
            var parameters = new MessageParameters
            {
                Model = "claude-3-5-sonnet-20241022",
                System = new List<SystemMessage> { new SystemMessage(systemInstruction) },
                Messages = messages,
                MaxTokens = 1024
            };

            try
            {
                // FIX: Endpoint method updated to 'GetClaudeMessageAsync' in v5
                var response = await client.Messages.GetClaudeMessageAsync(parameters, ct);

                // Extracting text value directly from the structured content object
                var replyText = response.Content.OfType<TextContent>().FirstOrDefault()?.Text?.Trim();
                return new ChatResponse { Success = true, Message = replyText ?? "" };
            }
            catch (Exception ex)
            {
                return new ChatResponse { Success = false, Error = ex.Message };
            }
        }
    }
}