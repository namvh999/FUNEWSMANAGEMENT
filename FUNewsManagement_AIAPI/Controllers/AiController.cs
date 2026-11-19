using FUNewsManagement_AIAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsManagement_AIAPI.Controllers
{
    public class SuggestTagsRequest
    {
        public string Content { get; set; } = string.Empty;
        public int MaxSuggestions { get; set; } = 5;
    }

    public class LearnTagRequest
    {
        public string TagName { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly IAiTagService _aiTagService;

        public AiController(IAiTagService aiTagService)
        {
            _aiTagService = aiTagService;
        }

        // POST: /api/ai/suggest-tags
        [HttpPost("suggest-tags")]
        public async Task<IActionResult> SuggestTags([FromBody] SuggestTagsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { message = "Nội dung (content) bài viết không được để trống." });
            }

            var tags = await _aiTagService.SuggestTagsAsync(request.Content, request.MaxSuggestions > 0 ? request.MaxSuggestions : 5);
            return Ok(tags);
        }

        // POST: /api/ai/learn-tag
        // Maintain a Learning Cache to remember tags frequently selected by users
        [HttpPost("learn-tag")]
        public IActionResult LearnTag([FromBody] LearnTagRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TagName))
            {
                return BadRequest(new { message = "Tên tag không được để trống." });
            }

            _aiTagService.LearnTag(request.TagName);
            return Ok(new { message = $"Đã cập nhật bộ nhớ học cho thẻ '{request.TagName}'.", tagName = request.TagName });
        }

        // GET: /api/ai/learning-cache
        [HttpGet("learning-cache")]
        public IActionResult GetLearningCache()
        {
            var cache = _aiTagService.GetLearningCache();
            return Ok(new
            {
                totalLearnedTags = cache.Count,
                tags = cache.OrderByDescending(kv => kv.Value).Select(kv => new { tagName = kv.Key, frequency = kv.Value })
            });
        }
    }
}
