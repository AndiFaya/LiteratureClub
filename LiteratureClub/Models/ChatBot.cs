namespace LiteratureClub.Models
{
    //One Message in the conversation, either from the user or the AI agent
    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty; // "user" or "Ai agent"
        public string Content { get; set; } = string.Empty;
    }

    // The request sent to the ChatBot API, containing the user's message and the conversation history.
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatMessage>? History { get; set; }
    }

    // The response recieved from the ChatBot API to the browser.
    public class  ChatResponse 
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
    }
}
