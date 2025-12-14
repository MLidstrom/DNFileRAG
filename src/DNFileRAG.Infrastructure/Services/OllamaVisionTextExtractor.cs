using System.Net.Http.Json;
using System.Text.RegularExpressions;
using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Infrastructure.Services;

/// <summary>
/// Uses an Ollama vision-capable model (e.g. llava) to extract text and describe an image.
/// </summary>
public sealed class OllamaVisionTextExtractor : IVisionTextExtractor
{
    private readonly HttpClient _httpClient;
    private readonly VisionOptions _visionOptions;
    private readonly ILogger<OllamaVisionTextExtractor> _logger;

    public OllamaVisionTextExtractor(
        HttpClient httpClient,
        IOptions<VisionOptions> visionOptions,
        ILogger<OllamaVisionTextExtractor> logger)
    {
        _httpClient = httpClient;
        _visionOptions = visionOptions.Value;
        _logger = logger;

        var baseUrl = _visionOptions.Ollama.BaseUrl.TrimEnd('/') + "/";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<VisionTextResult> ExtractAsync(
        byte[] imageBytes,
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        if (!_visionOptions.Enabled)
        {
            return new VisionTextResult
            {
                ExtractedText = string.Empty,
                Description = string.Empty
            };
        }

        var base64 = Convert.ToBase64String(imageBytes);

        var prompt =
            "You are helping index an internal knowledge base.\n" +
            "Task:\n" +
            "1) Extract any visible text exactly as it appears.\n" +
            "2) Provide a short description of what the image shows.\n" +
            "Rules:\n" +
            "- Preserve the original language for extracted text.\n" +
            "- Write the description in English.\n" +
            "- Output MUST follow this format:\n" +
            "TEXT:\n" +
            "<text>\n" +
            "\n" +
            "DESCRIPTION:\n" +
            "<one paragraph>\n";

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            prompt += $"\nFile name: {fileName}\n";
        }

        var request = new
        {
            model = _visionOptions.Ollama.Model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt,
                    images = new[] { base64 }
                }
            },
            stream = false,
            options = new
            {
                temperature = 0.1,
                num_predict = 600
            }
        };

        _logger.LogDebug("Extracting text/description from image using Ollama model {Model}", _visionOptions.Ollama.Model);

        var response = await _httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: cancellationToken);
        var content = result?.Message?.Content ?? string.Empty;

        var parsed = Parse(content);
        _logger.LogDebug("Ollama vision extracted {TextLen} chars text and {DescLen} chars description",
            parsed.ExtractedText.Length, parsed.Description.Length);

        return parsed;
    }

    private static VisionTextResult Parse(string raw)
    {
        raw = raw.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new VisionTextResult { ExtractedText = string.Empty, Description = string.Empty };
        }

        // Try strict format first.
        var match = Regex.Match(
            raw,
            @"TEXT:\s*(?<text>[\s\S]*?)\s*DESCRIPTION:\s*(?<desc>[\s\S]*)\s*$",
            RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return new VisionTextResult
            {
                ExtractedText = match.Groups["text"].Value.Trim(),
                Description = match.Groups["desc"].Value.Trim()
            };
        }

        // Fallback: treat everything as description (better than dropping content).
        return new VisionTextResult
        {
            ExtractedText = string.Empty,
            Description = raw
        };
    }

    private sealed class OllamaChatResponse
    {
        public OllamaMessage? Message { get; set; }
    }

    private sealed class OllamaMessage
    {
        public string Content { get; set; } = string.Empty;
    }
}


