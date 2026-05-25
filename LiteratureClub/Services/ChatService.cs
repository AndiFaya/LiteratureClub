
using Azure.AI.OpenAI;
using LiteratureClub.Models;
using OpenAI.Chat;
using System.ClientModel;

namespace LiteratureClub.Services
{
    //Sends the user's message to Azure and returns a response from the ChatBot API.
    public class ChatService
    {

        private readonly ChatClient _chatClient;

        public async Task<ChatResponse> GetReplyAsync(string userMessage,
            List<Models.ChatMessage>? history,
            CancellationToken ct = default)
        {
            try
            {
                //create the message list and define AI behaviour using a system prompt
                var message = new List<OpenAI.Chat.ChatMessage>
                {
                    new SystemChatMessage("You are a helpful assistant that provides information about the Literature Club. " +
                    "Answer questions about the platform, its functionalities, and its features in a friendly and informative manner."),
                };

                //Add previous conversation history if it exists for context.
                if(history != null)
                {
                    foreach (var msg in history) 
                    { 
                        //Add past user messages
                        if(msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                        {
                            message.Add(new UserChatMessage(msg.Content));
                        }

                        //Add past AI responses
                        else if (msg.Role.Equals("ai agent", StringComparison.OrdinalIgnoreCase))
                        {
                            message.Add(new AssistantChatMessage(msg.Content));
                        }
                    }
                }

                //Add the current user message to the conversation
                message.Add(new UserChatMessage(userMessage));

                //Call Azure OpenAI to generate a chat completion.
                var result = await _chatClient.CompleteChatAsync(message, cancellationToken: ct);

                //ectract the AI's text response safely.
                var reply = result.Value.Content?[0].Text ?? "I couldn't generate a response";

                //Return successfully response with AI Message
                return new ChatResponse { Success = true, Message = reply };
                
            }
            catch (Exception ex)
            {
                //return error response if something fails
                return new ChatResponse { Success = false, Error = ex.Message };
            }
        }
    }
}
