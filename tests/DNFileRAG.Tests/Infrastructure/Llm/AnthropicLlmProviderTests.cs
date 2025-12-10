using System.Net;
using System.Net.Http.Json;
using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using DNFileRAG.Infrastructure.Llm;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace DNFileRAG.Tests.Infrastructure.Llm;

public class AnthropicLlmProviderTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly IOptions<AnthropicLlmOptions> _options;

    public AnthropicLlmProviderTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object);
        _options = Options.Create(new AnthropicLlmOptions
        {
            ApiKey = "test-api-key",
            Model = "claude-3-5-sonnet-20241022"
        });
    }

    private AnthropicLlmProvider CreateProvider()
    {
        return new AnthropicLlmProvider(_httpClient, _options, NullLogger<AnthropicLlmProvider>.Instance);
    }

    private void SetupMockResponse(object responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(responseBody)
        };

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    #region Property Tests

    [Fact]
    public void ModelId_ShouldReturnConfiguredModel()
    {
        // Arrange & Act
        var provider = CreateProvider();

        // Assert
        provider.ModelId.Should().Be("claude-3-5-sonnet-20241022");
    }

    #endregion

    #region GenerateAsync Tests

    [Fact]
    public async Task GenerateAsync_WithNullSystemPrompt_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        var act = () => provider.GenerateAsync(null!, "user prompt");
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GenerateAsync_WithNullUserPrompt_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        var act = () => provider.GenerateAsync("system prompt", null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GenerateAsync_WithValidPrompts_ShouldReturnResponse()
    {
        // Arrange
        SetupMockResponse(new
        {
            content = new[]
            {
                new { type = "text", text = "Generated response from Claude" }
            }
        });

        var provider = CreateProvider();

        // Act
        var result = await provider.GenerateAsync("system", "user");

        // Assert
        result.Should().Be("Generated response from Claude");
    }

    [Fact]
    public async Task GenerateAsync_WithEmptyContent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupMockResponse(new { content = Array.Empty<object>() });

        var provider = CreateProvider();

        // Act & Assert
        var act = () => provider.GenerateAsync("system", "user");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no content*");
    }

    [Fact]
    public async Task GenerateAsync_WithOptions_ShouldUseProvidedOptions()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    content = new[] { new { type = "text", text = "Response" } }
                })
            });

        var provider = CreateProvider();
        var options = new LlmGenerationOptions
        {
            Temperature = 0.9f,
            MaxTokens = 2000
        };

        // Act
        await provider.GenerateAsync("system", "user", options);

        // Assert
        capturedRequest.Should().NotBeNull();
        var requestBody = await capturedRequest!.Content!.ReadAsStringAsync();
        requestBody.Should().Contain("\"temperature\":0.9");
        requestBody.Should().Contain("\"max_tokens\":2000");
    }

    [Fact]
    public async Task GenerateAsync_ShouldSendSystemPromptAsSeparateField()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    content = new[] { new { type = "text", text = "Response" } }
                })
            });

        var provider = CreateProvider();

        // Act
        await provider.GenerateAsync("You are a helpful assistant", "Hello!");

        // Assert
        capturedRequest.Should().NotBeNull();
        var requestBody = await capturedRequest!.Content!.ReadAsStringAsync();
        // Anthropic uses a separate "system" field, not a message
        requestBody.Should().Contain("\"system\":\"You are a helpful assistant\"");
    }

    [Fact]
    public async Task GenerateAsync_WithApiError_ShouldThrowHttpRequestException()
    {
        // Arrange
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var provider = CreateProvider();

        // Act & Assert
        var act = () => provider.GenerateAsync("system", "user");
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    #endregion
}
