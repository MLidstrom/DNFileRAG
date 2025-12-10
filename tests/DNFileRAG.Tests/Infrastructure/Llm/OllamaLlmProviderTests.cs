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

public class OllamaLlmProviderTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly IOptions<OllamaLlmOptions> _options;

    public OllamaLlmProviderTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object);
        _options = Options.Create(new OllamaLlmOptions
        {
            BaseUrl = "http://localhost:11434",
            Model = "llama3.2"
        });
    }

    private OllamaLlmProvider CreateProvider()
    {
        return new OllamaLlmProvider(_httpClient, _options, NullLogger<OllamaLlmProvider>.Instance);
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
        provider.ModelId.Should().Be("llama3.2");
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
            message = new { role = "assistant", content = "Generated response from Ollama" }
        });

        var provider = CreateProvider();

        // Act
        var result = await provider.GenerateAsync("system", "user");

        // Assert
        result.Should().Be("Generated response from Ollama");
    }

    [Fact]
    public async Task GenerateAsync_WithNullMessage_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupMockResponse(new { message = (object?)null });

        var provider = CreateProvider();

        // Act & Assert
        var act = () => provider.GenerateAsync("system", "user");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no message*");
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
                    message = new { role = "assistant", content = "Response" }
                })
            });

        var provider = CreateProvider();
        var options = new LlmGenerationOptions
        {
            Temperature = 0.7f,
            MaxTokens = 500
        };

        // Act
        await provider.GenerateAsync("system", "user", options);

        // Assert
        capturedRequest.Should().NotBeNull();
        var requestBody = await capturedRequest!.Content!.ReadAsStringAsync();
        requestBody.Should().Contain("\"temperature\":0.7");
        requestBody.Should().Contain("\"num_predict\":500");
    }

    [Fact]
    public async Task GenerateAsync_ShouldSetStreamToFalse()
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
                    message = new { role = "assistant", content = "Response" }
                })
            });

        var provider = CreateProvider();

        // Act
        await provider.GenerateAsync("system", "user");

        // Assert
        capturedRequest.Should().NotBeNull();
        var requestBody = await capturedRequest!.Content!.ReadAsStringAsync();
        requestBody.Should().Contain("\"stream\":false");
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
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var provider = CreateProvider();

        // Act & Assert
        var act = () => provider.GenerateAsync("system", "user");
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    #endregion
}
