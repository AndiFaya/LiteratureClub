using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using LiteratureClub.Services;
using Microsoft.AspNetCore.Http;

namespace LiteratureClub.Controllers
{
    public class LanguageController : Controller
    {
        private readonly TranslationService _translationService;

        public LanguageController(TranslationService translationService)
        {
            _translationService = translationService;
        }

        // Sets the language preference cookie globally across the browser session
        [HttpPost]
        public IActionResult SetLanguage(string culture)
        {
            if (!string.IsNullOrEmpty(culture))
            {
                CookieOptions option = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(30),
                    HttpOnly = true,
                    Secure = true
                };
                Response.Cookies.Append("SelectedLanguage", culture, option);
            }

            return Json(new { success = true });
        }

        // Endpoint for translating dynamic page content via JavaScript
        [HttpPost]
        public async Task<IActionResult> TranslateContent([FromBody] TranslationData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Text))
            {
                return Json(new { success = false, result = "" });
            }

            string currentLang = Request.Cookies["SelectedLanguage"] ?? "English";
            string translatedResult = await _translationService.TranslateUiTextAsync(data.Text, currentLang);

            return Json(new { success = true, result = translatedResult });
        }
    }

    public class TranslationData
    {
        public string Text { get; set; } = string.Empty;
    }
}
