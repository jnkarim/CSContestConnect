using Microsoft.AspNetCore.Mvc;
using CSContestConnect.Web.Services;
using System.Threading.Tasks;

namespace CSContestConnect.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly GeminiService _gemini;

        public ChatController(GeminiService gemini)
        {
            _gemini = gemini;
        }

        // POST: /api/chat/ask
        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { error = "Message cannot be empty." });

            try
            {
                var reply = await _gemini.AskGeminiAsync(request.Message);
                return Ok(new { reply });
            }
            catch (System.Exception ex)
            {
                // Log exception if needed
                return StatusCode(500, new { error = "AI service error.", details = ex.Message });
            }
        }
    }

    // Request model
    public class ChatRequest
    {
        public string Message { get; set; }
    }
}
