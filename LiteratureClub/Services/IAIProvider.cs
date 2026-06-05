using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LiteratureClub.Models;

namespace LiteratureClub.Services
{
    public interface IAIProvider
    {
        string Name { get; }
        Task<ChatResponse> GetReplyAsync(
            string systemInstruction,
            string userMessage,
            List<ChatMessage>? history,
            CancellationToken ct = default);
    }
}