using DNFileRAG.Core.Configuration;
using FluentAssertions;

namespace DNFileRAG.Tests.Core.Configuration;

public class ConfigurationOptionsTests
{
    [Fact]
    public void QdrantOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new QdrantOptions();

        // Assert
        options.Host.Should().Be("localhost");
        options.Port.Should().Be(6333);
        options.CollectionName.Should().Be("documents");
        options.VectorSize.Should().Be(1536);
    }

    [Fact]
    public void QdrantOptions_SectionName_ShouldBeCorrect()
    {
        QdrantOptions.SectionName.Should().Be("Qdrant");
    }

    [Fact]
    public void FileWatcherOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new FileWatcherOptions();

        // Assert
        options.WatchPath.Should().Be("/app/data/documents");
        options.IncludeSubdirectories.Should().BeTrue();
        options.SupportedExtensions.Should().BeEquivalentTo([
            ".pdf", ".docx", ".txt", ".md", ".html",
            ".png", ".jpg", ".jpeg", ".webp"
        ]);
        options.DebounceMilliseconds.Should().Be(500);
    }

    [Fact]
    public void FileWatcherOptions_SectionName_ShouldBeCorrect()
    {
        FileWatcherOptions.SectionName.Should().Be("FileWatcher");
    }

    [Fact]
    public void ChunkingOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new ChunkingOptions();

        // Assert
        options.ChunkSize.Should().Be(1500);
        options.ChunkOverlap.Should().Be(200);
    }

    [Fact]
    public void ChunkingOptions_SectionName_ShouldBeCorrect()
    {
        ChunkingOptions.SectionName.Should().Be("Chunking");
    }

    [Fact]
    public void EmbeddingOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new EmbeddingOptions();

        // Assert
        options.Provider.Should().Be("OpenAI");
        options.OpenAI.Should().NotBeNull();
        options.AzureOpenAI.Should().NotBeNull();
        options.Ollama.Should().NotBeNull();
    }

    [Fact]
    public void EmbeddingOptions_SectionName_ShouldBeCorrect()
    {
        EmbeddingOptions.SectionName.Should().Be("Embedding");
    }

    [Fact]
    public void OpenAIEmbeddingOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new OpenAIEmbeddingOptions();

        // Assert
        options.ApiKey.Should().BeEmpty();
        options.Model.Should().Be("text-embedding-3-small");
    }

    [Fact]
    public void AzureOpenAIEmbeddingOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new AzureOpenAIEmbeddingOptions();

        // Assert
        options.Endpoint.Should().BeEmpty();
        options.ApiKey.Should().BeEmpty();
        options.DeploymentName.Should().BeEmpty();
    }

    [Fact]
    public void OllamaEmbeddingOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new OllamaEmbeddingOptions();

        // Assert
        options.BaseUrl.Should().Be("http://localhost:11434");
        options.Model.Should().Be("nomic-embed-text");
    }

    [Fact]
    public void LlmOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new LlmOptions();

        // Assert
        options.Provider.Should().Be("OpenAI");
        options.OpenAI.Should().NotBeNull();
        options.AzureOpenAI.Should().NotBeNull();
        options.Anthropic.Should().NotBeNull();
        options.Ollama.Should().NotBeNull();
    }

    [Fact]
    public void LlmOptions_SectionName_ShouldBeCorrect()
    {
        LlmOptions.SectionName.Should().Be("Llm");
    }

    [Fact]
    public void OpenAILlmOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new OpenAILlmOptions();

        // Assert
        options.ApiKey.Should().BeEmpty();
        options.Model.Should().Be("gpt-4o-mini");
    }

    [Fact]
    public void AnthropicLlmOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new AnthropicLlmOptions();

        // Assert
        options.ApiKey.Should().BeEmpty();
        options.Model.Should().Be("claude-3-5-sonnet-20241022");
    }

    [Fact]
    public void OllamaLlmOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new OllamaLlmOptions();

        // Assert
        options.BaseUrl.Should().Be("http://localhost:11434");
        options.Model.Should().Be("llama3.2");
    }

    [Fact]
    public void RagOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new RagOptions();

        // Assert
        options.DefaultTopK.Should().Be(5);
        options.DefaultTemperature.Should().Be(0.2f);
        options.DefaultMaxTokens.Should().Be(512);
        options.SystemPrompt.Should().Contain("helpful assistant");
    }

    [Fact]
    public void RagOptions_SectionName_ShouldBeCorrect()
    {
        RagOptions.SectionName.Should().Be("Rag");
    }

    [Fact]
    public void ApiSecurityOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new ApiSecurityOptions();

        // Assert
        options.RequireApiKey.Should().BeTrue();
        options.ApiKeys.Should().BeEmpty();
    }

    [Fact]
    public void ApiSecurityOptions_SectionName_ShouldBeCorrect()
    {
        ApiSecurityOptions.SectionName.Should().Be("ApiSecurity");
    }

    [Fact]
    public void ApiKeyConfig_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var config = new ApiKeyConfig();

        // Assert
        config.Key.Should().BeEmpty();
        config.Name.Should().BeEmpty();
        config.Role.Should().Be("reader");
    }

    [Fact]
    public void ApiSecurityOptions_ShouldSupportMultipleKeys()
    {
        // Arrange & Act
        var options = new ApiSecurityOptions
        {
            ApiKeys =
            [
                new() { Key = "key1", Name = "User 1", Role = "reader" },
                new() { Key = "key2", Name = "Admin 1", Role = "admin" }
            ]
        };

        // Assert
        options.ApiKeys.Should().HaveCount(2);
        options.ApiKeys[0].Role.Should().Be("reader");
        options.ApiKeys[1].Role.Should().Be("admin");
    }

    [Fact]
    public void VisionOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new VisionOptions();

        // Assert
        options.Enabled.Should().BeFalse();
        options.Provider.Should().Be("Ollama");
        options.Ollama.Should().NotBeNull();
    }

    [Fact]
    public void VisionOptions_SectionName_ShouldBeCorrect()
    {
        VisionOptions.SectionName.Should().Be("Vision");
    }

    [Fact]
    public void OllamaVisionOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new OllamaVisionOptions();

        // Assert
        options.BaseUrl.Should().Be("http://localhost:11434");
        options.Model.Should().Be("llava");
    }
}
