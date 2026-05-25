using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.GenAI;
using Google.GenAI.Types;
using LiteratureClub.Models;
using Microsoft.Extensions.Configuration;

namespace LiteratureClub.Services
{
    // Sends the user's message to Google Gemini and returns a response from the ChatBot API.
    public class ChatService
    {
        private readonly string _apiKey;
        private readonly string _systemInstruction;

        public ChatService(IConfiguration configuration)
        {
            _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
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
        }

        public async Task<ChatResponse> GetReplyAsync(string userMessage,
            List<LiteratureClub.Models.ChatMessage>? history,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new ChatResponse { Success = false, Error = "Chatbot is offline: Missing API Key configuration." };
            }

            try
            {
                // 1. Initialize the official Google GenAI Client
                var client = new Client(apiKey: _apiKey);
                var contents = new List<Content>();

                // 2. Safely reconstruct the conversation history if present
                if (history != null)
                {
                    foreach (var msg in history)
                    {
                        if (string.IsNullOrWhiteSpace(msg.Content)) continue;

                        var historyItem = new Content();

                        if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                        {
                            historyItem.Role = "user";
                            historyItem.Parts = new List<Part> { new Part { Text = msg.Content } };
                            contents.Add(historyItem);
                        }
                        else if (msg.Role.Equals("ai agent", StringComparison.OrdinalIgnoreCase) ||
                                 msg.Role.Equals("model", StringComparison.OrdinalIgnoreCase))
                        {
                            historyItem.Role = "model"; // Gemini expects "model" for assistant turns
                            historyItem.Parts = new List<Part> { new Part { Text = msg.Content } };
                            contents.Add(historyItem);
                        }
                    }
                }

                // 3. Append the newest active user prompt to the end of the conversation
                var currentUserItem = new Content
                {
                    Role = "user",
                    Parts = new List<Part> { new Part { Text = userMessage } }
                };
                contents.Add(currentUserItem);

                // 4. Wrap the system instruction rules into a clean Content layer object
                var systemContent = new Content
                {
                    Parts = new List<Part> { new Part { Text = _systemInstruction } }
                };

                var config = new GenerateContentConfig
                {
                    SystemInstruction = systemContent,
                    Temperature = 0.7f
                };

                // 5. Invoke the stateless generation model
                var response = await client.Models.GenerateContentAsync(
                    model: "gemini-2.5-flash",
                    contents: contents,
                    config: config,
                    cancellationToken: ct
                );

                // 6. SAFE PARSING: Extract text by deep-diving into candidates safely to prevent Null Reference Exceptions
                if (response?.Candidates != null &&
                    response.Candidates.Count > 0 &&
                    response.Candidates[0].Content?.Parts != null &&
                    response.Candidates[0].Content.Parts.Count > 0)
                {
                    string reply = response.Candidates[0].Content.Parts[0].Text ?? "I couldn't generate a text response.";
                    return new ChatResponse { Success = true, Message = reply };
                }

                return new ChatResponse { Success = false, Error = "The AI engine returned an empty response layout structure." };
            }
            catch (Exception ex)
            {
                // Returns the inner error string smoothly to your UI rather than throwing an application crash
                return new ChatResponse { Success = false, Error = ex.Message };
            }
        }
    }
}