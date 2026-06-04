using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LiteratureClub.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LiteratureClub.Controllers
{
    public class LanguageController : Controller
    {
        private readonly TranslationService _translationService;

        public LanguageController(TranslationService translationService)
        {
            _translationService = translationService;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult SetLanguage(string culture)
        {
            if (!string.IsNullOrEmpty(culture))
            {
                CookieOptions option = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(30),
                    HttpOnly = false,
                    Secure = HttpContext.Request.IsHttps,
                    SameSite = SameSiteMode.Lax
                };
                Response.Cookies.Append("SelectedLanguage", culture, option);
            }

            return Json(new { success = true });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> TranslateContent([FromBody] TranslationRequestPayload payload)
        {
            if (payload == null || payload.Texts == null || payload.Texts.Count == 0)
                return Json(new { success = false, results = new List<string>() });

            // Use the language sent by the client, not a server-side cookie
            string targetLang = string.IsNullOrWhiteSpace(payload.TargetLanguage) ? "English" : payload.TargetLanguage;

            List<string> translatedResults = await _translationService.TranslateMultipleTextsAsync(payload.Texts, targetLang);

            return Json(new { success = true, results = translatedResults });
        }
    }
    public class TranslationRequestPayload
    {
        [JsonPropertyName("texts")]
        public List<string> Texts { get; set; } = new List<string>();

        [JsonPropertyName("targetLanguage")]   // <-- new field
        public string TargetLanguage { get; set; } = "English";
    }
}