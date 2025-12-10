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

public class OpenAILlmProviderTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly IOptions<OpenAILlmOptions> _options;

    public OpenAILlmProviderTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object);
        _options = Options.Create(new OpenAILlmOptions
        {
            ApiKey = "test-api-key",
            Model = "gpt-4o-mini"
        });
    }

    private OpenAILlmProvider CreateProvider()
    {
        return new OpenAILlmProvider(_httpClient, _options, NullLogger<OpenAILlmProvider>.Instance);
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
        provider.ModelId.Should().Be("gpt-4o-mini");
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
            choices = new[]
            {
                new { message = new { content = "Generated response" } }
            }
        });

        var provider = CreateProvider();

        // Act
        var result = await provider.GenerateAsync("system", "user");

        // Assert
        result.Should().Be("Generated response");
    }

    [Fact]
    public async Task GenerateAsync_WithEmptyChoices_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupMockResponse(new { choices = Array.Empty<object>() });

        var provider = CreateProvider();

        // Act & Assert
        var act = () => provider.GenerateAsync("system", "user");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no choices*");
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
                    choices = new[] { new { message = new { content = "Response" } } }
                })
            });

        var provider = CreateProvider();
        var options = new LlmGenerationOptions
        {
            Temperature = 0.8f,
            MaxTokens = 1000
        };

        // Act
        await provider.GenerateAsync("system", "user", options);

        // Assert
        capturedRequest.Should().NotBeNull();
        var requestBody = await capturedRequest!.Content!.ReadAsStringAsync();
        requestBody.Should().Contain("\"temperature\":0.8");
        requestBody.Should().Contain("\"max_tokens\":1000");
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

    [Fact]
    public async Task GenerateAsync_WithDefaultOptions_ShouldUseDefaultValues()
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
                    choices = new[] { new { message = new { content = "Response" } } }
                })
            });

        var provider = CreateProvider();

        // Act
        await provider.GenerateAsync("system", "user");

        // Assert
        capturedRequest.Should().NotBeNull();
        var requestBody = await capturedRequest!.Content!.ReadAsStringAsync();
        requestBody.Should().Contain("\"temperature\":0.2");
        requestBody.Should().Contain("\"max_tokens\":512");
    }

    #endregion
}
