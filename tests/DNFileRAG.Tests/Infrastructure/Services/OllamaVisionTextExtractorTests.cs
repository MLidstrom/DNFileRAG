using System.Net;
using System.Text.Json;
using DNFileRAG.Core.Configuration;
using DNFileRAG.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Tests.Infrastructure.Services;

public class OllamaVisionTextExtractorTests
{
    [Fact]
    public async Task ExtractAsync_WhenDisabled_ReturnsEmptyResult()
    {
        // Arrange
        var options = Options.Create(new VisionOptions
        {
            Enabled = false
        });
        Func<HttpRequestMessage, HttpResponseMessage> throwHandler = _ => throw new InvalidOperationException("Should not be called");
        var httpClient = new HttpClient(new FakeHttpHandler(throwHandler));
        var extractor = new OllamaVisionTextExtractor(httpClient, options, NullLogger<OllamaVisionTextExtractor>.Instance);

        // Act
        var result = await extractor.ExtractAsync(new byte[] { 1, 2, 3 }, "test.png");

        // Assert
        result.ExtractedText.Should().BeEmpty();
        result.Description.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_WhenEnabled_CallsOllamaApi()
    {
        // Arrange
        var options = Options.Create(new VisionOptions
        {
            Enabled = true,
            Ollama = new OllamaVisionOptions
            {
                BaseUrl = "http://localhost:11434",
                Model = "llava"
            }
        });

        var responseContent = new
        {
            message = new
            {
                content = "TEXT:\nHello World\n\nDESCRIPTION:\nA simple greeting image."
            }
        };

        var handler = new FakeHttpHandler(request =>
        {
            request.RequestUri!.ToString().Should().Contain("api/chat");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(responseContent))
            };
        });

        var httpClient = new HttpClient(handler);
        var extractor = new OllamaVisionTextExtractor(httpClient, options, NullLogger<OllamaVisionTextExtractor>.Instance);

        // Act
        var result = await extractor.ExtractAsync(new byte[] { 1, 2, 3 }, "test.png");

        // Assert
        result.ExtractedText.Should().Be("Hello World");
        result.Description.Should().Be("A simple greeting image.");
    }

    [Fact]
    public async Task ExtractAsync_WithUnstructuredResponse_FallsBackToDescription()
    {
        // Arrange
        var options = Options.Create(new VisionOptions
        {
            Enabled = true,
            Ollama = new OllamaVisionOptions
            {
                BaseUrl = "http://localhost:11434",
                Model = "llava"
            }
        });

        var responseContent = new
        {
            message = new
            {
                content = "This is an image showing a sunset over the ocean with beautiful colors."
            }
        };

        Func<HttpRequestMessage, HttpResponseMessage> syncHandler = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent))
        };
        var handler = new FakeHttpHandler(syncHandler);

        var httpClient = new HttpClient(handler);
        var extractor = new OllamaVisionTextExtractor(httpClient, options, NullLogger<OllamaVisionTextExtractor>.Instance);

        // Act
        var result = await extractor.ExtractAsync(new byte[] { 1, 2, 3 }, "sunset.jpg");

        // Assert
        result.ExtractedText.Should().BeEmpty();
        result.Description.Should().Contain("sunset over the ocean");
    }

    [Fact]
    public async Task ExtractAsync_WithEmptyResponse_ReturnsEmptyResult()
    {
        // Arrange
        var options = Options.Create(new VisionOptions
        {
            Enabled = true,
            Ollama = new OllamaVisionOptions
            {
                BaseUrl = "http://localhost:11434",
                Model = "llava"
            }
        });

        var responseContent = new
        {
            message = new
            {
                content = ""
            }
        };

        Func<HttpRequestMessage, HttpResponseMessage> syncHandler = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent))
        };
        var handler = new FakeHttpHandler(syncHandler);

        var httpClient = new HttpClient(handler);
        var extractor = new OllamaVisionTextExtractor(httpClient, options, NullLogger<OllamaVisionTextExtractor>.Instance);

        // Act
        var result = await extractor.ExtractAsync(new byte[] { 1, 2, 3 }, "empty.png");

        // Assert
        result.ExtractedText.Should().BeEmpty();
        result.Description.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_WithMultilineText_ParsesCorrectly()
    {
        // Arrange
        var options = Options.Create(new VisionOptions
        {
            Enabled = true,
            Ollama = new OllamaVisionOptions
            {
                BaseUrl = "http://localhost:11434",
                Model = "llava"
            }
        });

        var responseContent = new
        {
            message = new
            {
                content = "TEXT:\nLine 1\nLine 2\nLine 3\n\nDESCRIPTION:\nA document with multiple lines of text visible."
            }
        };

        Func<HttpRequestMessage, HttpResponseMessage> syncHandler = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent))
        };
        var handler = new FakeHttpHandler(syncHandler);

        var httpClient = new HttpClient(handler);
        var extractor = new OllamaVisionTextExtractor(httpClient, options, NullLogger<OllamaVisionTextExtractor>.Instance);

        // Act
        var result = await extractor.ExtractAsync(new byte[] { 1, 2, 3 }, "doc.png");

        // Assert
        result.ExtractedText.Should().Contain("Line 1");
        result.ExtractedText.Should().Contain("Line 2");
        result.ExtractedText.Should().Contain("Line 3");
        result.Description.Should().Contain("multiple lines");
    }

    [Fact]
    public async Task ExtractAsync_ApiError_ThrowsHttpRequestException()
    {
        // Arrange
        var options = Options.Create(new VisionOptions
        {
            Enabled = true,
            Ollama = new OllamaVisionOptions
            {
                BaseUrl = "http://localhost:11434",
                Model = "llava"
            }
        });

        Func<HttpRequestMessage, HttpResponseMessage> syncHandler = _ => new HttpResponseMessage(HttpStatusCode.NotFound);
        var handler = new FakeHttpHandler(syncHandler);

        var httpClient = new HttpClient(handler);
        var extractor = new OllamaVisionTextExtractor(httpClient, options, NullLogger<OllamaVisionTextExtractor>.Instance);

        // Act & Assert
        await extractor.Invoking(e => e.ExtractAsync(new byte[] { 1, 2, 3 }, "test.png"))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task ExtractAsync_IncludesFileName_InRequest()
    {
        // Arrange
        var options = Options.Create(new VisionOptions
        {
            Enabled = true,
            Ollama = new OllamaVisionOptions
            {
                BaseUrl = "http://localhost:11434",
                Model = "llava"
            }
        });

        string? capturedContent = null;
        var handler = new FakeHttpHandler(async request =>
        {
            capturedContent = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { message = new { content = "TEXT:\ntest\n\nDESCRIPTION:\ntest" } }))
            };
        });

        var httpClient = new HttpClient(handler);
        var extractor = new OllamaVisionTextExtractor(httpClient, options, NullLogger<OllamaVisionTextExtractor>.Instance);

        // Act
        await extractor.ExtractAsync(new byte[] { 1, 2, 3 }, "important-doc.png");

        // Assert
        capturedContent.Should().NotBeNull();
        capturedContent.Should().Contain("important-doc.png");
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _asyncHandler;

        public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _asyncHandler = request => Task.FromResult(handler(request));
        }

        public FakeHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> asyncHandler)
        {
            _asyncHandler = asyncHandler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _asyncHandler(request);
    }
}
