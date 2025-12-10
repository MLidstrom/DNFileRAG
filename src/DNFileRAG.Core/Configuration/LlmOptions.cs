namespace DNFileRAG.Core.Configuration;

public class LlmOptions
{
    public const string SectionName = "Llm";

    public string Provider { get; set; } = "OpenAI";
    public OpenAILlmOptions OpenAI { get; set; } = new();
    public AzureOpenAILlmOptions AzureOpenAI { get; set; } = new();
    public AnthropicLlmOptions Anthropic { get; set; } = new();
    public OllamaLlmOptions Ollama { get; set; } = new();
}

public class OpenAILlmOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
}

public class AzureOpenAILlmOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
}

public class AnthropicLlmOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-3-5-sonnet-20241022";
}

public class OllamaLlmOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2";
}
