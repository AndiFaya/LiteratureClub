using Microsoft.AspNetCore.Mvc;
using LiteratureClub.Models;
using LiteratureClub.Services;
using System.Threading;
using System.Threading.Tasks;

namespace LiteratureClub.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatService _chatService;

        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        public IActionResult Index()
        {
            return View();
        }

        //Called when user clicks send button - Get AI reply and returns JSON
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return Json(new ChatResponse { Success = false, Error = "Message is required" });

            var response = await _chatService.GetReplyAsync(request.Message, request.History, ct);

            return Json(response);
        }
    }
}
