namespace DNFileRAG.Core.Configuration;

public class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public string Provider { get; set; } = "OpenAI";
    public OpenAIEmbeddingOptions OpenAI { get; set; } = new();
    public AzureOpenAIEmbeddingOptions AzureOpenAI { get; set; } = new();
    public OllamaEmbeddingOptions Ollama { get; set; } = new();
}

public class OpenAIEmbeddingOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "text-embedding-3-small";
}

public class AzureOpenAIEmbeddingOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
}

public class OllamaEmbeddingOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "nomic-embed-text";
}
