using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FUNewsManagement_AIAPI.Services
{
    public class TagSuggestionResult
    {
        public string TagName { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Source { get; set; } = "NLP & Learning Engine";
    }

    public interface IAiTagService
    {
        Task<List<TagSuggestionResult>> SuggestTagsAsync(string content, int maxSuggestions = 5);
        void LearnTag(string tagName);
        Dictionary<string, int> GetLearningCache();
    }

    public class AiTagService : IAiTagService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AiTagService> _logger;

        // Thread-safe Learning Cache to remember frequently chosen tags
        private static readonly ConcurrentDictionary<string, int> _learningCache = new(StringComparer.OrdinalIgnoreCase);

        // Predefined Domain Keywords dictionary with baseline weights
        private static readonly Dictionary<string, string[]> _domainKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AI"] = new[] { "ai", "artificial intelligence", "trí tuệ nhân tạo", "machine learning", "deep learning", "llm", "chatgpt", "copilot", "neural" },
            ["DotNet8"] = new[] { ".net", "dotnet", ".net 8", "c#", "csharp", "asp.net", "entity framework", "ef core", "blazor" },
            ["Cloud"] = new[] { "cloud", "aws", "azure", "gcp", "serverless", "devops", "kubernetes", "docker" },
            ["Microservices"] = new[] { "microservice", "microservices", "distributed", "api", "rest", "grpc", "rabbitmq", "kafka" },
            ["CyberSecurity"] = new[] { "security", "cyber", "hacker", "vulnerability", "malware", "auth", "jwt", "encryption", "bảo mật" },
            ["Startups"] = new[] { "startup", "venture", "fundraising", "founder", "series a", "unicorn", "khởi nghiệp" },
            ["Finance"] = new[] { "fintech", "finance", "banking", "crypto", "blockchain", "stock", "tài chính", "ngân hàng" },
            ["Innovation"] = new[] { "innovation", "breakthrough", "discovery", "technology", "đột phá", "sáng tạo" },
            ["Education"] = new[] { "education", "university", "student", "scholarship", "learning", "giáo dục", "đại học" },
            ["Space"] = new[] { "space", "nasa", "spacex", "moon", "mars", "rocket", "astronomy", "vũ trụ" }
        };

        private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "is", "are", "was", "were",
            "và", "hoặc", "nhưng", "trong", "trên", "tại", "cho", "của", "với", "bởi", "là", "các", "những", "này", "đó", "được", "có"
        };

        static AiTagService()
        {
            // Seed initial learning cache
            _learningCache["AI"] = 25;
            _learningCache["DotNet8"] = 18;
            _learningCache["Cloud"] = 15;
            _learningCache["Microservices"] = 12;
            _learningCache["CyberSecurity"] = 9;
            _learningCache["Startups"] = 7;
            _learningCache["Finance"] = 6;
            _learningCache["Innovation"] = 10;
        }

        public AiTagService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<AiTagService> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<List<TagSuggestionResult>> SuggestTagsAsync(string content, int maxSuggestions = 5)
        {
            if (string.IsNullOrWhiteSpace(content))
                return new List<TagSuggestionResult>();

            // 1. Check if real OpenAI API is configured
            var openAiKey = _config["OpenAI:ApiKey"];
            if (!string.IsNullOrWhiteSpace(openAiKey) && openAiKey != "YOUR_OPENAI_API_KEY")
            {
                try
                {
                    var openAiResult = await CallOpenAiAsync(content, openAiKey);
                    if (openAiResult.Any())
                        return openAiResult;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OpenAI API call failed; falling back to local NLP extraction.");
                }
            }

            // 2. Intelligent Local NLP Keyword & Learning Extraction
            return ExtractKeywordsLocal(content, maxSuggestions);
        }

        private List<TagSuggestionResult> ExtractKeywordsLocal(string content, int maxSuggestions)
        {
            var text = content.ToLower();
            var scores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            // A. Domain keyword matching
            foreach (var kvp in _domainKeywords)
            {
                var tag = kvp.Key;
                var patterns = kvp.Value;
                int matches = 0;

                foreach (var pattern in patterns)
                {
                    // Word boundary regex
                    var regex = new Regex(@"\b" + Regex.Escape(pattern) + @"\b", RegexOptions.IgnoreCase);
                    matches += regex.Matches(text).Count;
                }

                if (matches > 0)
                {
                    scores[tag] = matches * 1.5;
                }
            }

            // B. Frequency extraction of capitalized or technical terms
            var words = Regex.Matches(content, @"\b[A-Za-z0-9#\.\+]{3,20}\b")
                .Select(m => m.Value)
                .Where(w => !_stopWords.Contains(w) && w.Length >= 3);

            foreach (var word in words)
            {
                if (char.IsUpper(word[0]))
                {
                    var cleanWord = word.Trim();
                    if (!scores.ContainsKey(cleanWord))
                    {
                        scores[cleanWord] = 1.0;
                    }
                    else
                    {
                        scores[cleanWord] += 0.5;
                    }
                }
            }

            // C. Apply Learning Cache Boost
            foreach (var tag in scores.Keys.ToList())
            {
                if (_learningCache.TryGetValue(tag, out int freq))
                {
                    // Add boost based on historical usage
                    scores[tag] += Math.Log(freq + 1) * 0.8;
                }
            }

            // Convert to confidence percentages (0.60 to 0.98)
            var maxRaw = scores.Values.DefaultIfEmpty(1.0).Max();
            if (maxRaw <= 0) maxRaw = 1.0;

            var results = scores
                .OrderByDescending(kv => kv.Value)
                .Take(maxSuggestions)
                .Select(kv =>
                {
                    var normalized = 0.65 + (kv.Value / maxRaw) * 0.30;
                    var confidence = Math.Round(Math.Min(0.98, normalized), 2);
                    return new TagSuggestionResult
                    {
                        TagName = kv.Key,
                        Confidence = confidence,
                        Source = _learningCache.ContainsKey(kv.Key) ? "NLP + Learning Cache" : "NLP Extraction"
                    };
                })
                .ToList();

            // Default fallback if no keywords matched
            if (!results.Any())
            {
                results.Add(new TagSuggestionResult { TagName = "News", Confidence = 0.70, Source = "Default" });
                results.Add(new TagSuggestionResult { TagName = "General", Confidence = 0.65, Source = "Default" });
            }

            return results;
        }

        private async Task<List<TagSuggestionResult>> CallOpenAiAsync(string content, string apiKey)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var payload = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = "Extract 3 to 5 concise tags or keywords from the text. Respond with only a comma-separated list of tags." },
                    new { role = "user", content = content.Length > 1500 ? content.Substring(0, 1500) : content }
                },
                max_tokens = 60,
                temperature = 0.3
            };

            var response = await client.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", payload);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var textResult = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (!string.IsNullOrWhiteSpace(textResult))
                {
                    return textResult.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim().Trim('#', '.', '-'))
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(t => new TagSuggestionResult
                        {
                            TagName = t,
                            Confidence = 0.95,
                            Source = "OpenAI GPT"
                        })
                        .ToList();
                }
            }

            return new List<TagSuggestionResult>();
        }

        public void LearnTag(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName)) return;
            var cleanTag = tagName.Trim();
            _learningCache.AddOrUpdate(cleanTag, 1, (_, existing) => existing + 1);
        }

        public Dictionary<string, int> GetLearningCache()
        {
            return new Dictionary<string, int>(_learningCache);
        }
    }
}
