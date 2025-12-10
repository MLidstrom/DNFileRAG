namespace DNFileRAG.Core.Configuration;

public class RagOptions
{
    public const string SectionName = "Rag";

    public int DefaultTopK { get; set; } = 5;
    public float DefaultTemperature { get; set; } = 0.2f;
    public int DefaultMaxTokens { get; set; } = 512;
    public string SystemPrompt { get; set; } = "You are a helpful assistant that answers questions based on the provided context. Always cite your sources.";
}
